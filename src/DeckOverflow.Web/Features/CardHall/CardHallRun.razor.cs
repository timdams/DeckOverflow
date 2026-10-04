using System.Globalization;
using DeckOverflow.CardHall.Cards;
using DeckOverflow.CardHall.Combat;
using DeckOverflow.CardHall.Commands;
using DeckOverflow.CardHall.Events;
using DeckOverflow.CardHall.Maps;
using DeckOverflow.CardHall.Runs;
using DeckOverflow.Core.Text;
using DeckOverflow.Web.Art;
using DeckOverflow.Web.Progress;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DeckOverflow.Web.Features.CardHall;

/// <summary>
/// Dunne shell rond een hele run van de Kaartenhal: vertaalt klikken naar commands, geeft gevechtsevents
/// door aan de stage en toont de andere schermen uit de snapshot. Geen spelregels hier. Wat de wereld
/// aanbelangt (een ✗-paneel, een melding, de laatste baas, terug naar de plattegrond) meldt ze aan <see cref="World.FactoryPage"/>.
/// </summary>
public partial class CardHallRun
{
    [Inject] private IProgressStore Store { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    /// <summary>De voortgang van de wereld; de run vult Codex, zakje, startpunten en het aantal runs aan.</summary>
    [Parameter, EditorRequired] public PlayerProgress Progress { get; set; } = default!;

    /// <summary>De maker: W wint, en de testroutes <c>?seed=</c> en <c>?fight=</c> werken.</summary>
    [Parameter] public bool Superuser { get; set; }

    /// <summary>De fabriek is onthuld: geen teases meer, en een knop terug naar de plattegrond.</summary>
    [Parameter] public bool Revealed { get; set; }

    [Parameter] public string? SeedQuery { get; set; }
    [Parameter] public string? FightQuery { get; set; }

    /// <summary>De run verscheen of verdween (gestart, gepauzeerd, voorbij): de wereld tekent opnieuw.</summary>
    [Parameter] public EventCallback OnChanged { get; set; }
    [Parameter] public EventCallback<string> OnXPanel { get; set; }
    [Parameter] public EventCallback<(string Text, string Class)> OnToast { get; set; }
    [Parameter] public EventCallback<string?> OnOpenCodex { get; set; }
    [Parameter] public EventCallback OnOpenXRegister { get; set; }

    /// <summary>De laatste baas van de Kaartenhal viel.</summary>
    [Parameter] public EventCallback OnRunWon { get; set; }

    private ElementReference _host;
    private DotNetObjectReference<CardHallRun>? _self;
    private Run? _run;
    private RunSnapshot? _snap;
    private bool _busy;
    private bool _showDeck;
    private bool _showRelics;
    private bool _showPouch;
    private int _startAct = 1;

    /// <summary>Terug in het hoofdmenu terwijl de run wacht: ze blijft in het geheugen, tot je herlaadt.</summary>
    private bool _paused;
    private bool _showOptions;
    private bool _soundOn = true;

    /// <summary>Pagina's die na het laatste gevecht opengingen, voor de melding op het volgende scherm.</summary>
    private readonly List<string> _newPages = [];

    /// <summary>Een act die net begon: de actkaart staat open tot je verdergaat.</summary>
    private int? _actCard;

    /// <summary>De vijand van het laatste gevecht, voor het verhaal in de Codex op het beloningsscherm.</summary>
    private string? _lastEnemy;
    private Picker? _picker;

    /// <summary>Een kaart uit je deck kiezen, en welk command daar dan uit volgt.</summary>
    private sealed record Picker(string Title, string? Hint, IReadOnlyList<int>? Eligible, bool ShowUpgrade, Func<int, ICommand> Command);

    private string? Fight => Superuser ? FightQuery : null;

    /// <summary>Er loopt een run en ze staat in beeld; anders toont FactoryPage het titelscherm of de plattegrond.</summary>
    public bool Showing => _run is not null && _snap is not null && !_paused;

    /// <summary>Er wordt een run gestart (de stage laadt nog).</summary>
    public bool Starting { get; private set; }

    /// <summary>De wachtende run, als korte regel voor de knop om verder te spelen.</summary>
    public string? PausedSummary => _paused && _snap is { } s
        ? S.T("ui.continue-run.where", ("act", s.Act), ("floor", s.Floor), ("rows", s.Map.Rows))
        : null;

    protected override void OnInitialized() => S.Changed += OnLanguageChanged;

    /// <summary>Een andere taal: de stage laadt haar teksten opnieuw, de schermen tekenen opnieuw.</summary>
    private void OnLanguageChanged() => _ = InvokeAsync(async () =>
    {
        await Stage.SetLanguageAsync(S.Language);
        StateHasChanged();
    });

    /// <summary>Start na een klik, zodat de browser audio toelaat.</summary>
    public async Task StartAsync(int startAct)
    {
        _startAct = startAct;
        Starting = true;
        await OnChanged.InvokeAsync();
        _self ??= DotNetObjectReference.Create(this);
        await Stage.InitAsync(_host, _self, debug: Superuser);

        ulong seed = Superuser && ulong.TryParse(SeedQuery, out ulong s) ? s : NewSeed();
        await NewRunAsync(seed);
        Starting = false;
        await OnChanged.InvokeAsync();
    }

    /// <summary>Is dit de testroute <c>?fight=</c>? Dan geen intro.</summary>
    public bool IsTestFight => Fight is not null;

    private async Task NewRunAsync(ulong seed)
    {
        RunSetup setup = Bestiary.Exists(Fight)
            ? new RunSetup(Map: SingleFight(Fight!), Opening: false, Deck: TestDeck)
            : new RunSetup(StartAct: _startAct);
        _run = Run.Start(seed, setup);
        _snap = _run.Snapshot();
        _paused = false;
        _showOptions = false;
        if (Fight is null)
        {
            Progress.RunsStarted++;
            await Store.SaveAsync(Progress);
        }
        // Elke run begint met de kaart van zijn act, behalve op de testroute ?fight=
        _actCard = Fight is null ? _run.Act.Number : null;
        _picker = null;
        _showDeck = false;
        _lastEnemy = null;

        var query = new Dictionary<string, object?> { ["seed"] = seed.ToString(CultureInfo.InvariantCulture) };
        if (Fight is not null) query["fight"] = Fight;
        Nav.NavigateTo(Nav.GetUriWithQueryParameters(query), replace: true);

        await Stage.ResetAsync();
        await OnChanged.InvokeAsync();
    }

    /// <summary>Voor <c>?fight=</c>: het starterdeck plus de kaarten die de puzzelvijanden nodig hebben.</summary>
    private static IReadOnlyList<CardDefinition> TestDeck =>
        [.. CardCatalog.StarterDeck(), CardCatalog.RemoldByte, CardCatalog.MeasureTwice, CardCatalog.ReadTheLabel, CardCatalog.CountLetters, CardCatalog.LetterA, CardCatalog.Ink, CardCatalog.Read,
         CardCatalog.HitThenTighten, CardCatalog.TightenThenHit, CardCatalog.Brackets];

    private static ActMap SingleFight(string enemy)
    {
        NodeKind kind = Bestiary.Bosses.Contains(enemy) ? NodeKind.Boss
            : Bestiary.Elites.Contains(enemy) ? NodeKind.Elite
            : NodeKind.Fight;
        return ActMap.Path((kind, enemy), (NodeKind.Rest, null));
    }

    // ---------- Opties ----------

    private async Task OpenOptionsAsync()
    {
        _soundOn = await Stage.SoundOnAsync();
        _showOptions = true;
    }

    /// <summary>Naar het hoofdmenu (titelscherm of plattegrond); de run wacht.</summary>
    private Task ToMainMenuAsync()
    {
        _showOptions = false;
        _showDeck = _showRelics = _showPouch = false;
        _picker = null;
        _paused = true;
        return OnChanged.InvokeAsync();
    }

    /// <summary>De wachtende run weer in beeld.</summary>
    public Task ContinueAsync()
    {
        _paused = false;
        return OnChanged.InvokeAsync();
    }

    /// <summary>Dezelfde run van voren af aan: zelfde seed, zelfde startact.</summary>
    private Task RestartAsync() => _snap is { } s ? NewRunAsync(s.Seed) : Task.CompletedTask;

    /// <summary>De run opgeven: ze telt nergens mee en je staat weer in het hoofdmenu.</summary>
    private Task AbandonAsync()
    {
        _showOptions = false;
        _paused = false;
        return LeaveAsync();
    }

    /// <summary>De run is voorbij of opgegeven: terug naar het titelscherm of de plattegrond.</summary>
    public Task LeaveAsync()
    {
        _run = null;
        _snap = null;
        _actCard = null;
        return OnChanged.InvokeAsync();
    }

    private async Task ToggleSoundAsync()
    {
        _soundOn = !_soundOn;
        await Stage.SetSoundAsync(_soundOn);
    }

    // ---------- Buiten het gevecht ----------

    private Task ChooseNodeAsync(int nodeId) => DoAsync(new ChooseNode(nodeId));

    private async Task DoAsync(ICommand command)
    {
        if (_run is null || _busy) return;

        var before = _snap!;
        var events = _run.Handle(command);
        _snap = _run.Snapshot();
        await ShowToastsAsync(events);
        await RememberActAsync(events);
        await RememberCodexAsync(events);
        await RememberTrinketsAsync(events);
        await RememberXPanelsAsync(events);
        await RememberRunEndAsync(events);

        // Een nieuw gevecht: de stage begint met een schone lei
        if (_snap.Phase == RunPhase.Combat && before.Phase != RunPhase.Combat)
        {
            await Stage.ResetAsync();
            await Stage.SetBackdropAsync(ArtStyle.Wide($"act-{_snap.ActKey}"));
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
    private Task ScrapAsync() => RunCombatAsync(new ScrapModifiers());

    /// <summary>Sneltoets W wint het lopende gevecht meteen, alleen voor de superuser.</summary>
    [JSInvokable]
    public Task OnDebugWin() => Superuser ? RunCombatAsync(new DebugWin()) : Task.CompletedTask;

    private async Task RunCombatAsync(ICommand command)
    {
        if (_run is null || _busy || _run.Phase != RunPhase.Combat) return;

        _busy = true;
        await InvokeAsync(StateHasChanged);
        try
        {
            _lastEnemy = _run.CombatSnapshot()?.Combatants.FirstOrDefault(c => c.IsEnemy)?.Key;
            if (_run.CombatSnapshot()?.Turn == 1) _newPages.Clear();
            var events = _run.Handle(command);

            await Stage.PlayAsync(events);
            if (_run.CombatSnapshot() is { } combat) await Stage.SyncAsync(combat);

            // Even blijven staan op VICTORY of CRASHED voor het volgende scherm komt
            if (_run.Phase != RunPhase.Combat) await Task.Delay(700);

            _snap = _run.Snapshot();
            // Pas na het gevecht: goud, relic. Tijdens het gevecht spreekt de stage.
            if (_snap.Phase != RunPhase.Combat) await ShowToastsAsync(events);
            await RememberCodexAsync(events);
            await RememberXPanelsAsync(events);
            await RememberRunEndAsync(events);
        }
        finally
        {
            _busy = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>De run is gewonnen: de wereld beslist wat dat ontgrendelt en onthult.</summary>
    private async Task RememberRunEndAsync(IEnumerable<GameEvent> events)
    {
        if (events.OfType<RunEnded>().Any(e => e.Won)) await OnRunWon.InvokeAsync();
    }

    /// <summary>Een nieuwe act bereikt: die wordt een startpunt voor volgende runs.</summary>
    private async Task RememberActAsync(IEnumerable<GameEvent> events)
    {
        foreach (var started in events.OfType<ActStarted>())
        {
            _actCard = started.Act;
            if (started.Act <= Progress.ReachedAct) continue;
            Progress.ReachedAct = started.Act;
            await Store.SaveAsync(Progress);
        }
    }

    /// <summary>Nieuwe Codex-pagina's bewaren. Een pagina houdt de getallen van het eerste moment waarop ze openging.</summary>
    private async Task RememberCodexAsync(IEnumerable<GameEvent> events)
    {
        bool changed = false;
        foreach (var unlocked in events.OfType<CodexUnlocked>())
        {
            if (!Progress.Codex.TryAdd(unlocked.Key, unlocked.Values)) continue;
            _newPages.Add(unlocked.Key);
            changed = true;
        }
        if (changed) await Store.SaveAsync(Progress);
    }

    /// <summary>Een onderdeel uit een kist gaat in het zakje, over runs heen.</summary>
    private async Task RememberTrinketsAsync(IEnumerable<GameEvent> events)
    {
        bool changed = false;
        foreach (var found in events.OfType<TrinketFound>()) changed |= Progress.Trinkets.Add(found.Key);
        if (changed) await Store.SaveAsync(Progress);
    }

    private static int StepOf(string trinket) => Trinkets.All.FirstOrDefault(t => t.Key == trinket)?.Step ?? 0;

    /// <summary>Panelen uit de motor: de wereld bewaart en meldt ze.</summary>
    private async Task RememberXPanelsAsync(IEnumerable<GameEvent> events)
    {
        foreach (var earned in events.OfType<XPanelEarned>()) await OnXPanel.InvokeAsync(earned.Key);
    }

    /// <summary>Een melding per nieuwe pagina; een klik opent de Codex op die pagina.</summary>
    private RenderFragment NewPages() => builder =>
    {
        int seq = 0;
        foreach (string key in _newPages)
        {
            builder.OpenElement(seq++, "button");
            builder.AddAttribute(seq++, "class", "codex-new");
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create(this, () => OnOpenCodex.InvokeAsync(key)));
            builder.AddContent(seq++, S.T("ui.codex.new", ("name", S.T($"codex.{key}.name"))));
            builder.CloseElement();
        }
    };

    // ---------- Meldingen ----------

    private async Task ShowToastsAsync(IEnumerable<GameEvent> events)
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
                EnemyPatched p => (S.T(TextRef.Of("ui.toast.patched", ("enemy", p.EnemyKey))), "relic"),
                ActStarted a => (S.T("ui.act", ("act", a.Act), ("name", S.T($"act.{Acts.Get(a.Act).Key}"))), "act"),
                RunRejected r => (S.T(r.Reason), "rejected"),
                _ => null
            };
            if (toast is { } t2) await OnToast.InvokeAsync(t2);
        }
    }

    // ---------- Hulp ----------

    private static string Signed(int amount) => amount > 0 ? $"+{amount}" : amount.ToString(CultureInfo.InvariantCulture);

    private static string Num(double value) => (Math.Round(value * 100) / 100).ToString(CultureInfo.InvariantCulture);

    private static string Percent(double value, double max) =>
        (max <= 0 ? 0 : Math.Clamp(value / max * 100, 0, 100)).ToString("0.#", CultureInfo.InvariantCulture);

    private static ulong NewSeed() => (ulong)Random.Shared.NextInt64(1, 1_000_000);

    public async ValueTask DisposeAsync()
    {
        S.Changed -= OnLanguageChanged;
        await Stage.DisposeAsync();
        _self?.Dispose();
    }
}
