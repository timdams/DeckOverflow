using DeckOverflow.Core.Random;

namespace DeckOverflow.CardHall.Cards;

/// <summary>Trekstapel, hand en aflegstapel. Kent geen regels, alleen kaartbewegingen.</summary>
public sealed class Deck
{
    private readonly List<CardDefinition> _drawPile;
    private readonly List<CardDefinition> _hand = [];
    private readonly List<CardDefinition> _discard = [];
    private readonly SeededRng _rng;

    public Deck(IEnumerable<CardDefinition> cards, SeededRng rng)
    {
        _rng = rng;
        _drawPile = [.. cards];
        _rng.Shuffle(_drawPile);
    }

    public IReadOnlyList<CardDefinition> Hand => _hand;
    public int DrawCount => _drawPile.Count;
    public int DiscardCount => _discard.Count;

    public void Draw(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (_drawPile.Count == 0)
            {
                if (_discard.Count == 0) return;
                _drawPile.AddRange(_discard);
                _discard.Clear();
                _rng.Shuffle(_drawPile);
            }

            // Bovenaan de stapel = einde van de lijst
            int top = _drawPile.Count - 1;
            _hand.Add(_drawPile[top]);
            _drawPile.RemoveAt(top);
        }
    }

    public CardDefinition DiscardFromHand(int handIndex)
    {
        CardDefinition card = _hand[handIndex];
        _hand.RemoveAt(handIndex);
        _discard.Add(card);
        return card;
    }

    public void DiscardHand()
    {
        _discard.AddRange(_hand);
        _hand.Clear();
    }
}
