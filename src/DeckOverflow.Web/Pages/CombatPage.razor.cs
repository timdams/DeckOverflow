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

    private ElementReference _host;
    private DotNetObjectReference<CombatPage>? _self;
    private Combat? _combat;
    private ulong _seed;
    private double _loadMs;
    private bool _stageReady;
    private bool _starting;
    private bool _busy;

    private bool IsOver => _combat is { Outcome: not CombatOutcome.Ongoing };

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

        _seed = ulong.TryParse(SeedQuery, out ulong seed) ? seed : ByteGolemScenario.DefaultSeed;
        _combat = Combat.Start(ByteGolemScenario.Create(), _seed);
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

    private async Task RestartAsync(ulong seed)
    {
        _seed = seed;
        _combat = Combat.Start(ByteGolemScenario.Create(), seed);
        Nav.NavigateTo(Nav.GetUriWithQueryParameter("seed", seed.ToString()), replace: true);

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
