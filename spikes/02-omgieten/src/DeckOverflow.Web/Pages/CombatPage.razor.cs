using System.Diagnostics;
using DeckOverflow.Engine.Combat;
using DeckOverflow.Engine.Commands;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DeckOverflow.Web.Pages;

/// <summary>
/// Dunne shell: vertaalt input naar commands, geeft events door aan de stage
/// en stuurt daarna de snapshot als waarheid. Geen spelregels hier.
/// </summary>
public partial class CombatPage
{
    [SupplyParameterFromQuery(Name = "seed")]
    public string? SeedQuery { get; set; }

    [SupplyParameterFromQuery(Name = "gevecht")]
    public string? ScenarioQuery { get; set; }

    private ElementReference _host;
    private DotNetObjectReference<CombatPage>? _self;
    private Combat? _combat;
    private string _scenario = Scenarios.Order[0];
    private ulong _seed;
    private double _loadMs;
    private bool _stageReady;
    private bool _starting;
    private bool _busy;

    private bool IsOver => _combat is { Outcome: not CombatOutcome.Ongoing };
    private bool Won => _combat is { Outcome: CombatOutcome.Won };

    private string? NextScenario
    {
        get
        {
            int i = Scenarios.Order.ToList().IndexOf(_scenario);
            return i >= 0 && i + 1 < Scenarios.Order.Count ? Scenarios.Order[i + 1] : null;
        }
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

        _scenario = Scenarios.Exists(ScenarioQuery) ? ScenarioQuery! : Scenarios.Order[0];
        _seed = ulong.TryParse(SeedQuery, out ulong seed) ? seed : Scenarios.DefaultSeed;
        _combat = Combat.Start(Scenarios.Create(_scenario), _seed);
        await Stage.SyncAsync(_combat.Snapshot());

        _stageReady = true;
        _starting = false;
    }

    /// <summary>Aangeroepen door de stage wanneer een kaart op een doelwit valt.</summary>
    [JSInvokable]
    public Task OnCardPlayed(int handIndex, int targetId) =>
        RunAsync(new PlayCard(handIndex, targetId));

    private Task EndTurnAsync() => RunAsync(new EndTurn());

    private async Task RunAsync(ICommand command)
    {
        if (_combat is null || _busy) return;

        _busy = true;
        await InvokeAsync(StateHasChanged);
        try
        {
            long start = Stopwatch.GetTimestamp();
            var events = _combat.Handle(command);
            double engineMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

            await Stage.PlayAsync(events, engineMs);
            await Stage.SyncAsync(_combat.Snapshot());
        }
        finally
        {
            _busy = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadAsync(string scenario, ulong seed)
    {
        _scenario = scenario;
        _seed = seed;
        _combat = Combat.Start(Scenarios.Create(scenario), seed);
        Nav.NavigateTo(Nav.GetUriWithQueryParameters(new Dictionary<string, object?>
        {
            ["gevecht"] = scenario,
            ["seed"] = seed.ToString(),
        }), replace: true);

        await Stage.ResetAsync();
        await Stage.SyncAsync(_combat.Snapshot());
    }

    private static ulong NewSeed() => (ulong)Random.Shared.NextInt64(1, 1_000_000);

    public async ValueTask DisposeAsync()
    {
        await Stage.DisposeAsync();
        _self?.Dispose();
    }
}
