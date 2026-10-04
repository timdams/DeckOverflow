using DeckOverflow.ControlRoom.Gambits;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DeckOverflow.Web.Interop;

/// <summary>
/// De enige plek die met de stage van de Controlekamer praat, zoals <see cref="StageBridge"/> voor de Card Hall.
/// De stage speelt events af en tekent een momentopname; ze kent geen regels en leest nooit zelf het duel.
/// </summary>
public sealed class ControlRoomStage(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _stage;

    public bool Ready => _stage is not null;

    public async Task InitAsync(ElementReference host)
    {
        if (_stage is not null) return;
        try
        {
            var stage = await js.InvokeAsync<IJSObjectReference>("import", "./control-room/stage/stage.js");
            await stage.InvokeVoidAsync("init", host);
            _stage = stage;
        }
        catch (JSException e)
        {
            Console.WriteLine($"De stage van de Controlekamer laadde niet, het duel speelt zonder beeld: {e.Message}");
        }
    }

    /// <summary>Een nieuw gevecht: de twee automaten op hun plaats, zonder animatie.</summary>
    public ValueTask ShowAsync(DuelSnapshot snapshot) => Call("show", snapshot);

    /// <summary>Klaar als de laatste animatie gedaan is.</summary>
    public ValueTask PlayAsync(IReadOnlyList<DuelEvent> events) => Call("play", events);

    /// <summary>De waarheid na elke zet, en bij terugspoelen: tekent zonder animatie.</summary>
    public ValueTask SyncAsync(DuelSnapshot snapshot) => Call("sync", snapshot);

    /// <summary>1, 2 of 4 keer zo snel.</summary>
    public ValueTask SetSpeedAsync(int speed) => Call("setSpeed", speed);

    public ValueTask SetSoundAsync(bool on) => Call("setSound", on);

    public ValueTask<bool> SoundOnAsync() => _stage is null ? ValueTask.FromResult(true) : _stage.InvokeAsync<bool>("soundOn");

    public ValueTask SetLanguageAsync(string language) =>
        _stage is null ? ValueTask.CompletedTask : _stage.InvokeVoidAsync("setLanguage", language);

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

    /// <summary>
    /// Zonder stage (ze laadde niet, of nog niet) speelt het duel gewoon verder: de shell en het log
    /// tonen alles wat ertoe doet, de stage is alleen het beeld erbij.
    /// </summary>
    private ValueTask Call(string method, params object?[] args) =>
        _stage is null ? ValueTask.CompletedTask : _stage.InvokeVoidAsync(method, args);
}
