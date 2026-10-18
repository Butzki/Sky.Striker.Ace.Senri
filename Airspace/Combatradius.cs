namespace Airspace;

/// <summary>
/// Zona de controle de um jogador: tudo que ele possui e onde está.
/// Mão, deck, cemitério, banidas e Extra Deck são <see cref="Multiset"/> (sem ordem, o que
/// mantém o hash canônico); o tabuleiro é <see cref="FieldZones"/>.
/// </summary>
public sealed class CombatRadius : IEquatable<CombatRadius>
{
    public static readonly CombatRadius Empty = new CombatRadius(
        Multiset.Empty, Multiset.Empty, Multiset.Empty, Multiset.Empty, Multiset.Empty, FieldZones.Empty);

    private string? _key;

    public CombatRadius(
        Multiset hand,
        Multiset deck,
        Multiset graveyard,
        Multiset banished,
        Multiset extraDeck,
        FieldZones field)
    {
        ArgumentNullException.ThrowIfNull(hand);
        ArgumentNullException.ThrowIfNull(deck);
        ArgumentNullException.ThrowIfNull(graveyard);
        ArgumentNullException.ThrowIfNull(banished);
        ArgumentNullException.ThrowIfNull(extraDeck);
        ArgumentNullException.ThrowIfNull(field);

        Hand = hand;
        Deck = deck;
        Graveyard = graveyard;
        Banished = banished;
        ExtraDeck = extraDeck;
        Field = field;
    }

    public Multiset Hand { get; }
    public Multiset Deck { get; }
    public Multiset Graveyard { get; }
    public Multiset Banished { get; }
    public Multiset ExtraDeck { get; }
    public FieldZones Field { get; }

    /// <summary>Devolve uma cópia; argumento omitido (null) mantém o valor atual.</summary>
    public CombatRadius With(
        Multiset? hand = null,
        Multiset? deck = null,
        Multiset? graveyard = null,
        Multiset? banished = null,
        Multiset? extraDeck = null,
        FieldZones? field = null) =>
        new CombatRadius(
            hand ?? Hand,
            deck ?? Deck,
            graveyard ?? Graveyard,
            banished ?? Banished,
            extraDeck ?? ExtraDeck,
            field ?? Field);

    public string CanonicalKey => _key ??=
        $"H:{Hand.CanonicalKey}|D:{Deck.CanonicalKey}|G:{Graveyard.CanonicalKey}|" +
        $"B:{Banished.CanonicalKey}|X:{ExtraDeck.CanonicalKey}|F:{Field.CanonicalKey}";

    public bool Equals(CombatRadius? other) =>
        other is not null && string.Equals(CanonicalKey, other.CanonicalKey, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as CombatRadius);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(CanonicalKey);

    public override string ToString() => CanonicalKey;
}