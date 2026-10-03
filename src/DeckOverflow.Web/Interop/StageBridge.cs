using DeckOverflow.CardHall.Combat;
using DeckOverflow.CardHall.Events;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DeckOverflow.Web.Interop;

/// <summary>
/// De enige plek die met de JS-stage praat. Wisselen naar [JSImport]/[JSExport]
/// blijft daardoor een lokale wijziging.
/// </summary>
public sealed class StageBridge(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _stage;

    public async Task InitAsync<TPage>(ElementReference host, DotNetObjectReference<TPage> page, double loadMs)
        where TPage : class
    {
        _stage = await js.InvokeAsync<IJSObjectReference>("import", "./card-hall/stage/stage.js");
        await _stage.InvokeVoidAsync("init", host, page, loadMs);
    }

    /// <summary>Klaar als de laatste animatie gedaan is.</summary>
    public ValueTask PlayAsync(IReadOnlyList<GameEvent> events, double engineMs) =>
        Stage.InvokeVoidAsync("play", events, new { engineMs });

    public ValueTask SyncAsync(CombatSnapshot snapshot) =>
        Stage.InvokeVoidAsync("sync", snapshot);

    public ValueTask ResetAsync() =>
        Stage.InvokeVoidAsync("reset");

    public async ValueTask DisposeAsync()
    {
        if (_stage is null) return;
        try
        {
            await _stage.InvokeVoidAsync("dispose");
            await _stage.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // Pagina is al weg, niets op te ruimen
        }
        _stage = null;
    }

    private IJSObjectReference Stage =>
        _stage ?? throw new InvalidOperationException("De stage is nog niet geïnitialiseerd.");
}
