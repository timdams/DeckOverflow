using System.Diagnostics;
using System.Globalization;
using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Combat;
using DeckOverflow.Engine.Commands;
using DeckOverflow.Engine.Events;
using DeckOverflow.Engine.Maps;
using DeckOverflow.Engine.Runs;
using DeckOverflow.Engine.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DeckOverflow.Web.Pages;

/// <summary>
/// Dunne shell rond een hele run: vertaalt klikken naar commands, geeft gevechtsevents
/// door aan de stage en toont de andere schermen uit de snapshot. Geen spelregels hier.
/// </summary>
public partial class RunPage
{
    [SupplyParameterFromQuery(Name = "seed")]
    public string? SeedQuery { get; set; }

    /// <summary>Om één vijand snel te proberen: een run van één knoop.</summary>
    [SupplyParameterFromQuery(Name = "fight")]
    public string? FightQuery { get; set; }

    [Inject] private HttpClient Http { get; set; } = default!;

    private ElementReference _host;
    private DotNetObjectReference<RunPage>? _self;
    private Run? _run;
    private RunSnapshot? _snap;
    private double _loadMs;
    private bool _starting;
    private bool _busy;
    private bool _showDeck;

    /// <summary>De vijand van het laatste gevecht, voor het verhaal in de Codex op het beloningsscherm.</summary>
    private string? _lastEnemy;
    private Picker? _picker;
    private readonly List<Toast> _toasts = [];
    private int _toastId;

    /// <summary>Een kaart uit je deck kiezen, en welk command daar dan uit volgt.</summary>
    private sealed record Picker(string Title, string? Hint, IReadOnlyList<int>? Eligible, bool ShowUpgrade, Func<int, ICommand> Command);

    private sealed record Toast(int Id, string Text, string Class);

    protected override async Task OnInitializedAsync()
    {
        if (!S.Loaded) await S.LoadAsync(Http);
        if (!Art.Loaded) await Art.LoadAsync(Http);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Meetpunt "laadtijd tot speelbaar": de Speel-knop staat op het scherm
        if (firstRender) _loadMs = await JS.InvokeAsync<double>("deckOverflow.now");
    }

    /// <summary>Start na een klik, zodat de browser audio toelaat.</summary>
    private async Task StartAsync()
    {
        _starting = true;
        _self = DotNetObjectReference.Create(this);
        await Stage.InitAsync(_host, _self, _loadMs);

        ulong seed = ulong.TryParse(SeedQuery, out ulong s) ? s : NewSeed();
        await NewRunAsync(seed);
        _starting = false;
    }

    private async Task NewRunAsync(ulong seed)
    {
        RunSetup setup = Bestiary.Exists(FightQuery) ? new RunSetup(Map: SingleFight(FightQuery!), Opening: false) : new RunSetup();
        _run = Run.Start(seed, setup);
        _snap = _run.Snapshot();
        _picker = null;
        _showDeck = false;
        _lastEnemy = null;
        _toasts.Clear();

        var query = new Dictionary<string, object?> { ["seed"] = seed.ToString(CultureInfo.InvariantCulture) };
        if (FightQuery is not null) query["fight"] = FightQuery;
        Nav.NavigateTo(Nav.GetUriWithQueryParameters(query), replace: true);

        await Stage.ResetAsync();
    }

    private static ActMap SingleFight(string enemy)
    {
        NodeKind kind = enemy == Bestiary.Reckoner ? NodeKind.Boss
            : Bestiary.ElitePool.Contains(enemy) ? NodeKind.Elite
            : NodeKind.Fight;
        return ActMap.Path((kind, enemy), (NodeKind.Rest, null));
    }

    // ---------- Buiten het gevecht ----------

    private Task ChooseNodeAsync(int nodeId) => DoAsync(new ChooseNode(nodeId));

    private async Task DoAsync(ICommand command)
    {
        if (_run is null || _busy) return;

        var before = _snap!;
        var events = _run.Handle(command);
        _snap = _run.Snapshot();
        ShowToasts(events);

        // Een nieuw gevecht: de stage begint met een schone lei
        if (_snap.Phase == RunPhase.Combat && before.Phase != RunPhase.Combat)
        {
            await Stage.ResetAsync();
            await Stage.SyncAsync(_run.CombatSnapshot()!);
        }
    }

    private Task ChooseEventOptionAsync(int index, EventOptionView option)
    {
        if (option.EligibleCards.Count == 0) return DoAsync(new ChooseEventOption(index));

        _picker = new Picker(S.T(option.Label), S.T(option.Detail), option.EligibleCards, ShowUpgrade: false,
            i => new ChooseEventOption(index, i));
        return Task.CompletedTask;
    }

    private void PickUpgrade(RestView rest) =>
        _picker = new Picker(S.T("ui.rest.pick"), null, rest.UpgradableCards, ShowUpgrade: true, i => new RestUpgrade(i));

    private void PickRemoval(ShopView shop) =>
        _picker = new Picker(S.T("ui.shop.remove.pick"), S.T("ui.shop.remove.hint", ("price", shop.RemovalPrice)), null, ShowUpgrade: false, i => new BuyRemoval(i));

    private async Task PickedAsync(int deckIndex)
    {
        if (_picker is not { } picker) return;
        _picker = null;
        await DoAsync(picker.Command(deckIndex));
        StateHasChanged();
    }

    // ---------- Gevecht ----------

    /// <summary>Aangeroepen door de stage wanneer een kaart op een doelwit valt.</summary>
    [JSInvokable]
    public Task OnCardPlayed(int handIndex, int targetId) =>
        RunCombatAsync(new PlayCard(handIndex, targetId));

    private Task EndTurnAsync() => RunCombatAsync(new EndTurn());

    private async Task RunCombatAsync(ICommand command)
    {
        if (_run is null || _busy || _run.Phase != RunPhase.Combat) return;

        _busy = true;
        await InvokeAsync(StateHasChanged);
        try
        {
            _lastEnemy = _run.CombatSnapshot()?.Combatants.FirstOrDefault(c => c.IsEnemy)?.Key;
            long start = Stopwatch.GetTimestamp();
            var events = _run.Handle(command);
            double engineMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

            await Stage.PlayAsync(events, engineMs);
            if (_run.CombatSnapshot() is { } combat) await Stage.SyncAsync(combat);

            // Even blijven staan op VICTORY of CRASHED voor het volgende scherm komt
            if (_run.Phase != RunPhase.Combat) await Task.Delay(700);

            _snap = _run.Snapshot();
            // Pas na het gevecht: goud, relic. Tijdens het gevecht spreekt de stage.
            if (_snap.Phase != RunPhase.Combat) ShowToasts(events);
        }
        finally
        {
            _busy = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    // ---------- Meldingen ----------

    private void ShowToasts(IEnumerable<GameEvent> events)
    {
        foreach (var e in events)
        {
            (string Text, string Class)? toast = e switch
            {
                GoldChanged g => (S.T("ui.toast.gold", ("amount", Signed(g.Amount))), "gold"),
                RunHpChanged h when h.Amount != 0 => (S.T("ui.toast.hp", ("amount", Signed(h.Amount))), h.Amount > 0 ? "heal" : "hurt"),
                CardAdded c => (S.T(TextRef.Of("ui.toast.card-added", ("card", c.CardId))), "card"),
                CardRemoved c => (S.T(TextRef.Of("ui.toast.card-removed", ("card", c.CardId))), "card"),
                CardTransformed t => (S.T(TextRef.Of("ui.toast.card-changed", ("from", t.FromId), ("to", t.ToId))), "card"),
                RelicGained r => (S.T(TextRef.Of("ui.toast.relic", ("relic", r.RelicId))), "relic"),
                RunRejected r => (S.T(r.Reason), "rejected"),
                _ => null
            };
            if (toast is { } t2) AddToast(t2.Text, t2.Class);
        }
    }

    private void AddToast(string text, string cssClass)
    {
        var toast = new Toast(++_toastId, text, cssClass);
        _toasts.Add(toast);
        _ = RemoveLaterAsync(toast);
    }

    private async Task RemoveLaterAsync(Toast toast)
    {
        await Task.Delay(2600);
        _toasts.Remove(toast);
        await InvokeAsync(StateHasChanged);
    }

    // ---------- Hulp ----------

    private static string Signed(int amount) => amount > 0 ? $"+{amount}" : amount.ToString(CultureInfo.InvariantCulture);

    private static string Num(double value) => (Math.Round(value * 100) / 100).ToString(CultureInfo.InvariantCulture);

    private static string Percent(double value, double max) =>
        (max <= 0 ? 0 : Math.Clamp(value / max * 100, 0, 100)).ToString("0.#", CultureInfo.InvariantCulture);

    private static ulong NewSeed() => (ulong)Random.Shared.NextInt64(1, 1_000_000);

    public async ValueTask DisposeAsync()
    {
        await Stage.DisposeAsync();
        _self?.Dispose();
    }
}
