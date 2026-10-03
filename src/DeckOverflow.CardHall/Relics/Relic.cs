using DeckOverflow.CardHall.Cards;
using DeckOverflow.Core.Text;

namespace DeckOverflow.CardHall.Relics;

/// <summary>
/// Een relic haakt in op vaste momenten. Gevecht en run roepen de haken aan en kennen
/// geen enkele relic bij naam; een nieuwe relic is één klasse plus een test.
/// Een gevecht krijgt verse exemplaren, dus status per gevecht (een teller) begint telkens opnieuw.
/// </summary>
public abstract class Relic
{
    public abstract string Id { get; }

    /// <summary>De tekst, met de getallen die erin horen. Sleutel <c>relic.&lt;id&gt;.text</c>.</summary>
    public virtual TextRef Text => TextRef.Of($"relic.{Id}.text");

    // ---------- Run ----------

    /// <summary>Max HP erbij op het moment dat je hem krijgt. Je heelt evenveel.</summary>
    public virtual int MaxHpOnGain => 0;

    /// <summary>Blok waarmee je elk gevecht begint.</summary>
    public virtual double BlockAtCombatStart => 0;

    /// <summary>Goud uit gevechten wordt vermenigvuldigd met deze factor, afgerond met <c>Math.Round</c>.</summary>
    public virtual double GoldFactor => 1.0;

    /// <summary>HP terug na het zoveelste gewonnen gevecht van de run.</summary>
    public virtual int HealAfterWin(int combatsWon) => 0;

    // ---------- Gevecht ----------

    /// <param name="cardsPlayed">Hoeveel kaarten je in dit gevecht al speelde.</param>
    public virtual bool MakesFree(CardDefinition card, int cardsPlayed) => false;

    /// <summary>Deze relic maakte net een kaart gratis.</summary>
    public virtual void OnMadeFree() { }

    /// <summary>Van jouw schade op een vijand ging <paramref name="lost"/> verloren door afkappen.</summary>
    public virtual void OnDamageTruncated(double lost) { }

    /// <summary>Na elke kaart: schade die de relic nu afvuurt, één keer per oproep.</summary>
    public virtual bool TryFire(out double damage)
    {
        damage = 0;
        return false;
    }

    /// <summary>Voortgang voor relics die tellen, bv. "1.5/3". Leeg voor de rest.</summary>
    public virtual string? Counter => null;
}
