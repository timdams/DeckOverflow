using DeckOverflow.Engine.Gambits;
using DeckOverflow.Web.Components;
using DeckOverflow.Web.Text;

namespace DeckOverflow.Web.Pages;

/// <summary>
/// De shell van de Controlekamer. Bevat geen spelregels: ze stelt regels op, laat de motor zet per zet spelen
/// en zet de events om in een log, flitsen en zwevende getallen.
/// </summary>
public partial class ControlRoom
{
    private sealed record LogLine(int Id, string Text, string Kind);

    /// <summary>Hoelang één zet in beeld blijft bij snelheid 1×.</summary>
    private const int StepMs = 700;
    private const int LogLength = 9;

    private int _levelIndex;
    private bool _openAll;
    private readonly HashSet<string> _beaten = [];
    private readonly Dictionary<string, (int Turns, int Rules)> _best = [];
    private readonly Dictionary<string, List<RuleDraft>> _boards = [];

    private List<RuleDraft> _drafts = [];
    private List<RuleDraft> _enemyDrafts = [];
    private Duel? _duel;
    private bool _running;
    private bool _showCode;
    private int _speed = 1;
    private CancellationTokenSource? _cts;

    private readonly List<LogLine> _log = [];
    private readonly List<BotCard.Popup> _popups = [];
    private readonly int[] _flash = [-1, -1];
    private readonly int[] _flashSeq = [0, 0];
    private readonly string[] _anim = ["", ""];
    private readonly int[] _animSeq = [0, 0];
    private int _seq;

    private Level Level => Levels.All[_levelIndex];
    private bool Busy => _duel is { IsOver: false };

    private BotState PlayerState => _duel?.Bot(Side.Player).State() ?? new Bot(Levels.Player).State();
    private BotState EnemyState => _duel?.Bot(Side.Enemy).State() ?? new Bot(Level.Enemy).State();

    /// <summary>Checks en zetten die in dit gevecht voor het eerst opduiken.</summary>
    private List<string> NewThings
    {
        get
        {
            if (_levelIndex == 0) return [];
            var before = Levels.All[_levelIndex - 1];
            return Level.Checks.Except(before.Checks).Select(c => S.T($"pick.{c}"))
                .Concat(Level.Moves.Except(before.Moves).Select(m => S.T($"move.{m}")))
                .Concat(Level.AllowAnd && !before.AllowAnd ? [S.T("ui.addAnd").TrimStart('+', ' ')] : [])
                .ToList();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await S.LoadAsync(Http);
        var query = new Uri(Nav.Uri).Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2)).ToDictionary(p => p[0], p => p.Length > 1 ? Uri.UnescapeDataString(p[1]) : "");
        _openAll = query.ContainsKey("all");
        if (query.TryGetValue("level", out var key))
        {
            int found = Levels.All.ToList().FindIndex(l => l.Key == key);
            if (found >= 0) { _levelIndex = found; _openAll = true; }
        }
        LoadBoard();
    }

    private bool Unlocked(int index) =>
        _openAll || index == 0 || _beaten.Contains(Levels.All[index - 1].Key);

    private void SelectLevel(int index)
    {
        Stop();
        _duel = null;
        _levelIndex = index;
        LoadBoard();
    }

    private void LoadBoard()
    {
        if (!_boards.TryGetValue(Level.Key, out var board))
            _boards[Level.Key] = board = Level.StartRules.Select(RuleDraft.From).ToList();
        _drafts = board;
        _enemyDrafts = Level.EnemyRules.Select(RuleDraft.From).ToList();
        ClearFeedback();
    }

    private void ClearFeedback()
    {
        _log.Clear();
        _popups.Clear();
        _flash[0] = _flash[1] = -1;
        _flashSeq[0] = _flashSeq[1] = 0;
        _anim[0] = _anim[1] = "";
    }

    private async Task Run()
    {
        ClearFeedback();
        _duel = Levels.Start(Level, _drafts.Select(d => d.ToRule()).ToList());
        await Resume();
    }

    private async Task Resume()
    {
        if (_duel is null || _duel.IsOver) return;
        _running = true;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        try
        {
            while (!_duel.IsOver && !token.IsCancellationRequested)
            {
                Show(_duel.Step());
                StateHasChanged();
                await Task.Delay(StepMs / _speed, token);
            }
        }
        catch (TaskCanceledException) { }
        finally
        {
            _running = false;
            StateHasChanged();
        }
    }

    private void Pause() => Stop();

    private void StepOnce()
    {
        if (_duel is null || _duel.IsOver) return;
        Show(_duel.Step());
    }

    private void Reset()
    {
        Stop();
        _duel = null;
        ClearFeedback();
    }

    private void Stop()
    {
        _cts?.Cancel();
        _running = false;
    }

    private void Show(IReadOnlyList<DuelEvent> events)
    {
        _popups.Clear();
        foreach (var e in events)
        {
            switch (e)
            {
                case TurnStarted t:
                    Log(S.T("log.turn", ("turn", t.Turn)), "turn");
                    break;
                case RuleFired f:
                    _flash[(int)f.Side] = f.RuleIndex;
                    _flashSeq[(int)f.Side] = ++_seq;
                    Animate(f.Side, "act");
                    break;
                case NoRuleMatched n:
                    _flash[(int)n.Side] = -1;
                    Log(S.T($"log.{n.Side}.none"), n.Side.ToString());
                    break;
                case Whacked w:
                {
                    string text = S.T(w.WasCharged ? $"log.{w.Side}.whackCharged" : $"log.{w.Side}.whack", ("damage", w.Damage));
                    if (w.Absorbed > 0) text += $" ({S.T("log.absorbed", ("absorbed", w.Absorbed))})";
                    Log(text, w.Side.ToString());
                    Side target = Other(w.Side);
                    int hurt = w.Damage - w.Absorbed;
                    if (hurt > 0) Pop(target, $"-{hurt}", "damage");
                    if (w.Absorbed > 0) Pop(target, $"⛨{w.Absorbed}", "blocked");
                    if (hurt > 0) Animate(target, "hit");
                    break;
                }
                case Braced b:
                    Log(S.T($"log.{b.Side}.brace", ("total", b.Total)), b.Side.ToString());
                    Pop(b.Side, $"+{b.Amount}⛨", "block");
                    break;
                case Repaired r:
                    Log(S.T($"log.{r.Side}.repair", ("amount", r.Amount)), r.Side.ToString());
                    Pop(r.Side, $"+{r.Amount}", "heal");
                    break;
                case RepairEmpty r:
                    Log(S.T($"log.{r.Side}.repairEmpty"), r.Side + " wasted");
                    Pop(r.Side, "∅", "wasted");
                    break;
                case WoundUp u:
                    Log(S.T(u.WasCharged ? $"log.{u.Side}.windUpAgain" : $"log.{u.Side}.windUp"), u.Side + (u.WasCharged ? " wasted" : ""));
                    break;
                case DuelEnded d:
                    Finish(d);
                    break;
            }
        }
    }

    private void Finish(DuelEnded end)
    {
        _running = false;
        if (end.Outcome != Outcome.PlayerWon) return;
        _beaten.Add(Level.Key);
        var score = (end.Turn, _drafts.Count);
        if (!_best.TryGetValue(Level.Key, out var best) || score.CompareTo(best) < 0)
            _best[Level.Key] = score;
    }

    private void Log(string text, string kind)
    {
        _log.Insert(0, new LogLine(++_seq, text, kind));
        if (_log.Count > LogLength) _log.RemoveAt(_log.Count - 1);
    }

    private void Pop(Side side, string text, string kind) => _popups.Add(new BotCard.Popup(++_seq, side, text, kind));

    private void Animate(Side side, string anim)
    {
        _anim[(int)side] = anim;
        _animSeq[(int)side] = ++_seq;
    }

    private static Side Other(Side side) => side == Side.Player ? Side.Enemy : Side.Player;

    public void Dispose() => _cts?.Cancel();
}
