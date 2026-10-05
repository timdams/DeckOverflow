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
    private IJSObjectReference? _module;
    private IJSObjectReference? _stage;

    private async ValueTask<IJSObjectReference> ModuleAsync() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./card-hall/stage/stage.js");

    /// <summary>Tekeningen en geluid alvast laden, op het titelscherm; wacht niet tot het klaar is.</summary>
    public async ValueTask PreloadAsync() =>
        await (await ModuleAsync()).InvokeVoidAsync("preload");

    /// <summary>Hoe ver het laden is, van 0 tot 100.</summary>
    public async ValueTask<int> LoadProgressAsync() =>
        await (await ModuleAsync()).InvokeAsync<int>("loadProgress");

    /// <param name="debug">Sneltoetsen om te testen (W wint), alleen voor de superuser.</param>
    public async Task InitAsync<TPage>(ElementReference host, DotNetObjectReference<TPage> page, bool debug = false)
        where TPage : class
    {
        // Eén stage per pagina: een tweede run hergebruikt ze (reset doet de rest)
        if (_stage is not null) return;
        var module = await ModuleAsync();
        await module.InvokeVoidAsync("init", host, page, debug);
        _stage = module;
    }

    /// <summary>Klaar als de laatste animatie gedaan is.</summary>
    public ValueTask PlayAsync(IReadOnlyList<GameEvent> events) =>
        Stage.InvokeVoidAsync("play", events);

    public ValueTask SyncAsync(CombatSnapshot snapshot) =>
        Stage.InvokeVoidAsync("sync", snapshot);

    public ValueTask SetSoundAsync(bool on) =>
        Stage.InvokeVoidAsync("setSound", on);

    public ValueTask<bool> SoundOnAsync() =>
        Stage.InvokeAsync<bool>("soundOn");

    /// <summary>Een stage die nog niet bestaat, leest de taal zelf bij het opstarten.</summary>
    public ValueTask SetLanguageAsync(string language) =>
        _stage is null ? ValueTask.CompletedTask : _stage.InvokeVoidAsync("setLanguage", language);

    public ValueTask ResetAsync() =>
        Stage.InvokeVoidAsync("reset");

    /// <summary>De tekening van de act als decor achter het gevecht, zoals de beeldtaal vraagt: elke plek is een plek.</summary>
    public ValueTask SetBackdropAsync(string url) =>
        Stage.InvokeVoidAsync("setBackdrop", url);

    public async ValueTask DisposeAsync()
    {
        if (_module is null) return;
        try
        {
            if (_stage is not null) await _stage.InvokeVoidAsync("dispose");
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // Pagina is al weg, niets op te ruimen
        }
        _stage = _module = null;
    }

    private IJSObjectReference Stage =>
        _stage ?? throw new InvalidOperationException("De stage is nog niet geïnitialiseerd.");
}
