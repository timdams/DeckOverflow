using DeckOverflow.CardHall.Runs;
using DeckOverflow.Web.Backend;
using DeckOverflow.Web.Features.CardHall;
using DeckOverflow.Web.Progress;
using Microsoft.AspNetCore.Components;

namespace DeckOverflow.Web.World;

/// <summary>
/// De wereld op <c>/</c>: het titelscherm voor de onthulling, de plattegrond erna, en alles wat de afdelingen
/// delen (voortgang, Codex, ✗-register, account, meldingen). De run van de Kaartenhal staat in
/// <see cref="CardHallRun"/>; die meldt hier wat er gebeurt. Geen spelregels hier.
/// </summary>
public partial class FactoryPage
{
    // De testroutes ?seed=, ?fight= en ?unbox= werken alleen voor de superuser, die ook altijd de plattegrond ziet.

    [SupplyParameterFromQuery(Name = "seed")]
    public string? SeedQuery { get; set; }

    /// <summary>Om één vijand snel te proberen: een run van één knoop.</summary>
    [SupplyParameterFromQuery(Name = "fight")]
    public string? FightQuery { get; set; }

    /// <summary>Om het moment "uit de doos" van een afdeling te bekijken, zonder ze te verdienen.</summary>
    [SupplyParameterFromQuery(Name = "unbox")]
    public string? UnboxQuery { get; set; }

    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private IProgressStore Store { get; set; } = default!;
    [Inject] private IServiceProvider Services { get; set; } = default!;
    [Inject] private Superuser Superuser { get; set; } = default!;

    /// <summary>De maker: W wint, alles staat open, de testroutes werken. Zie <see cref="Backend.Superuser"/>.</summary>
    private bool _superuser;

    /// <summary>Accounts en klassen, alleen als Supabase ingesteld is.</summary>
    private Account? Account => Services.GetService<Account>();
    private bool _showAccount;

    /// <summary>Het vangnet: na zoveel gestarte runs barst de muur vanzelf.</summary>
    private const int CrackAfterRuns = 5;

    private CardHallRun? _cardHall;
    private int _startAct = 1;

    /// <summary>Alles wat over runs heen blijft: Codex, panelen, ontgrendelde afdelingen, de onthulling.</summary>
    private PlayerProgress _progress = new();
    private bool _showCodex;
    private string? _codexFocus;
    private bool _showXRegister;
    /// <summary>Het venster "Over dit spel" op het hoofdscherm.</summary>
    private bool _showAbout;
    private bool _showIntro;
    private bool _showReveal;
    private string _revealReason = "won";

    /// <summary>De onthulling gebeurde: de plattegrond vervangt het titelscherm.</summary>
    private bool Revealed => _superuser || _progress.Revealed;

    /// <summary>Tot welke act je een run mag starten: zo ver je kwam, of elke act voor de superuser.</summary>
    private int ReachedAct => _superuser ? Acts.All[^1].Number : _progress.ReachedAct;

    private bool CrackVisible => !Revealed && _progress.RunsStarted >= CrackAfterRuns;

    private bool RunShowing => _cardHall?.Showing ?? false;
    private bool Starting => _cardHall?.Starting ?? false;
    private string? PausedRun => _cardHall?.PausedSummary;

    /// <summary>Een verdiend paneel dat net opsprong.</summary>
    private sealed record PanelPopup(int Id, string Key);
    private readonly List<PanelPopup> _panelPopups = [];

    /// <summary>Zo lang blijft een paneel staan voor het weer wegzakt; gelijk aan de animatie in run.css.</summary>
    private const int PanelPopupMs = 4600;

    private sealed record Toast(int Id, string Text, string Class);
    private readonly List<Toast> _toasts = [];
    private int _toastId;

    protected override async Task OnInitializedAsync()
    {
        if (!S.Loaded) await S.LoadAsync(Http);
        if (!Art.Loaded) await Art.LoadAsync(Http);
        S.Changed += OnLanguageChanged;
    }

    private void OnLanguageChanged() => _ = InvokeAsync(StateHasChanged);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        _superuser = await Superuser.IsActiveAsync();
        _progress = await Store.LoadAsync();
        Store.Changed += OnProgressChanged;
        _progress.ReachedAct = Math.Clamp(_progress.ReachedAct, 1, Acts.All[^1].Number);
        StateHasChanged();
    }

    /// <summary>Een run in de Kaartenhal starten. De allereerste keer komt eerst de intro.</summary>
    private async Task StartAsync(int startAct)
    {
        if (_cardHall is null) return;
        _startAct = startAct;
        if (!_progress.IntroSeen && !_cardHall.IsTestFight)
        {
            _showIntro = true;
            return;
        }
        await _cardHall.StartAsync(startAct);
    }

    private async Task FinishIntroAsync()
    {
        _progress.IntroSeen = true;
        _showIntro = false;
        await Store.SaveAsync(_progress);
        await StartAsync(_startAct);
    }

    private Task ContinueRunAsync() => _cardHall?.ContinueAsync() ?? Task.CompletedTask;

    /// <summary>
    /// De laatste baas van de Card Hall viel: je verdient de sleutel van de Controlekamer, en wie de fabriek
    /// nog niet zag, krijgt de onthulling. Staat de Controlekamer nog op binnenkort, dan is de sleutel een tease.
    /// </summary>
    private async Task RunWonAsync()
    {
        if (_progress.TryUnlock(Departments.ControlRoom, UnlockHow.Boss, DateTimeOffset.UtcNow))
        {
            await Store.SaveAsync(_progress);
            bool open = Departments.IsOpen(Departments.Get(Departments.ControlRoom), _progress.Unlocks);
            AddToast(S.T(open ? "ui.toast.unlocked" : "ui.toast.key", ("name", S.T("ui.world.control-room.name"))), "relic");
        }
        if (!_progress.Revealed) await RevealAsync("won");
    }

    private async Task RevealAsync(string reason)
    {
        _revealReason = reason;
        _progress.RevealedBy = reason;
        _showReveal = true;
        await Store.SaveAsync(_progress);
        StateHasChanged();
    }

    /// <summary>Na de onthulling meteen je fabriek bewaren.</summary>
    private async Task KeepFactoryAfterRevealAsync()
    {
        await BackToFloorAsync();
        _showAccount = true;
    }

    /// <summary>Afgemeld of account verwijderd: helemaal opnieuw beginnen, als een nieuwe gast.</summary>
    private void SignedOut() => Nav.NavigateTo(Nav.BaseUri, forceLoad: true);

    /// <summary>Terug naar de plattegrond: de onthulling is gezien, de run is voorbij.</summary>
    private async Task BackToFloorAsync()
    {
        _showReveal = false;
        if (_cardHall is not null) await _cardHall.LeaveAsync();
    }

    /// <summary>Een Codex-blad omgeslagen. Wie een pagina tot het einde leest, overtreedt de laatste regel.</summary>
    private async Task TurnCodexPageAsync((string Key, int Layer) turn)
    {
        if (turn.Layer <= _progress.CodexRead.GetValueOrDefault(turn.Key, 1)) return;
        _progress.CodexRead[turn.Key] = turn.Layer;
        await Store.SaveAsync(_progress);
        if (turn.Layer >= 4) await EarnXPanelAsync(XPanels.ReadTheManual);
    }

    /// <summary>Een verdiend ✗-paneel bewaren en laten opspringen.</summary>
    private async Task EarnXPanelAsync(string key)
    {
        if (!_progress.XPanels.Add(key)) return;
        var popup = new PanelPopup(++_toastId, key);
        _panelPopups.Add(popup);
        _ = RemovePopupLaterAsync(popup);
        await Store.SaveAsync(_progress);
    }

    private async Task RemovePopupLaterAsync(PanelPopup popup)
    {
        await Task.Delay(PanelPopupMs);
        _panelPopups.Remove(popup);
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Ook na inloggen: misschien ben je nu de superuser.</summary>
    private void OnProgressChanged() => _ = InvokeAsync(async () =>
    {
        _superuser = await Superuser.IsActiveAsync();
        StateHasChanged();
    });

    private void OpenCodex(string? focus)
    {
        _codexFocus = focus;
        _showCodex = true;
    }

    /// <summary>Een afdeling klapte op de plattegrond uit haar doos: dat moment is gebeurd en komt niet terug.</summary>
    private async Task UnboxedAsync(string department)
    {
        if (_progress.Unboxed.Add(department)) await Store.SaveAsync(_progress);
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

    public void Dispose()
    {
        Store.Changed -= OnProgressChanged;
        S.Changed -= OnLanguageChanged;
    }
}
