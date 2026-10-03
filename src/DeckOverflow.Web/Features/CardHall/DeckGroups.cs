using System.Net;
using System.Text;
using DeckOverflow.CardHall.Cards;
using Microsoft.AspNetCore.Components;

namespace DeckOverflow.Web.Features.CardHall;

/// <summary>Gelijke kaarten in een deck samen, met hun posities: "4× Whack" in plaats van vier keer dezelfde kaart.</summary>
public sealed record DeckGroup(CardInfo Card, IReadOnlyList<int> Indexes)
{
    public int Count => Indexes.Count;
}

public static class DeckGroups
{
    /// <summary>Groepeert per kaart (een verbeterde kaart is een andere kaart), gesorteerd op kost en dan op naam.</summary>
    public static IReadOnlyList<DeckGroup> Of(IReadOnlyList<CardInfo> deck, Func<string, string> name, IReadOnlyList<int>? only = null) =>
        [.. Enumerable.Range(0, deck.Count)
            .Where(i => only is null || only.Contains(i))
            .GroupBy(i => deck[i].Id)
            .Select(g => new DeckGroup(deck[g.First()], [.. g]))
            .OrderBy(g => g.Card.Cost)
            .ThenBy(g => name(g.Card.Id), StringComparer.Ordinal)];

    /// <summary>
    /// De tekst na een verbetering, met in het geel wat nieuw is tegenover ervoor: "Deal <mark>9</mark> damage".
    /// Woord per woord, zodat een getal dat verandert meteen opvalt.
    /// </summary>
    public static MarkupString MarkChanges(string before, string after)
    {
        var old = before.Split(' ').GroupBy(w => w).ToDictionary(g => g.Key, g => g.Count());
        var html = new StringBuilder();
        foreach (string word in after.Split(' '))
        {
            if (html.Length > 0) html.Append(' ');
            string safe = WebUtility.HtmlEncode(word);
            if (old.TryGetValue(word, out int left) && left > 0)
            {
                old[word] = left - 1;
                html.Append(safe);
            }
            else
            {
                html.Append("<mark>").Append(safe).Append("</mark>");
            }
        }
        return new MarkupString(html.ToString());
    }
}
