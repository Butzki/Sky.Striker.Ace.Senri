namespace Airspace;
using System.Collections.Immutable;
using System.Text;

/// <summary>
/// Estado completo do jogo, imutável. Cada ação gera um novo estado via <see cref="With"/>.
/// <see cref="CanonicalKey"/> cobre tudo que afeta jogadas futuras (zonas, cartas desconhecidas na mão,
/// <see cref="UsedThisTurn"/> e <see cref="Flags"/>); estados equivalentes têm a mesma chave.
/// </summary>
public sealed class GameState : IEquatable<GameState>
{
    private string? _key;

    private GameState(
        CombatRadius radius,
        int unknownCardsInHand,
        ImmutableSortedSet<string> usedThisTurn,
        ImmutableSortedDictionary<string, int> flags)
    {
        if (unknownCardsInHand < 0)
            throw new ArgumentOutOfRangeException(
                nameof(unknownCardsInHand), "UnknownCardsInHand não pode ser negativo.");

        Radius = radius;
        UnknownCardsInHand = unknownCardsInHand;
        UsedThisTurn = usedThisTurn;
        Flags = flags;
    }

    public CombatRadius Radius { get; }

    /// <summary>Cartas na mão cuja identidade ainda não foi informada (ex.: compras). Valem +1 cada na pontuação.</summary>
    public int UnknownCardsInHand { get; }

    /// <summary>Chaves de efeitos "uma vez por turno" já usados (ex.: "sky-striker-mobilize-engage").</summary>
    public ImmutableSortedSet<string> UsedThisTurn { get; }

    /// <summary>Marcadores nomeados com valor inteiro (ex.: restrições de invocação do turno). Valor 0 não é guardado.</summary>
    public ImmutableSortedDictionary<string, int> Flags { get; }

    // Atalhos para a zona de controle.
    public Multiset Hand => Radius.Hand;
    public Multiset Deck => Radius.Deck;
    public Multiset Graveyard => Radius.Graveyard;
    public Multiset Banished => Radius.Banished;
    public Multiset ExtraDeck => Radius.ExtraDeck;
    public FieldZones Field => Radius.Field;

    public static GameState Empty { get; } = new GameState(
        CombatRadius.Empty,
        0,
        ImmutableSortedSet.Create<string>(StringComparer.Ordinal),
        ImmutableSortedDictionary.Create<string, int>(StringComparer.Ordinal));

    /// <summary>
    /// Estado inicial: <paramref name="deck"/> já deve estar sem as cartas da <paramref name="hand"/>.
    /// Campo, cemitério e banidas começam vazios.
    /// </summary>
    public static GameState Initial(Multiset hand, Multiset deck, Multiset extraDeck) =>
        Empty.With(hand: hand, deck: deck, extraDeck: extraDeck);

    /// <summary>Devolve um novo estado; argumento omitido (null) mantém o valor atual. Nunca altera este objeto.</summary>
    public GameState With(
        Multiset? hand = null,
        Multiset? deck = null,
        Multiset? graveyard = null,
        Multiset? banished = null,
        Multiset? extraDeck = null,
        FieldZones? field = null,
        int? unknownCardsInHand = null,
        ImmutableSortedSet<string>? usedThisTurn = null,
        ImmutableSortedDictionary<string, int>? flags = null) =>
        new GameState(
            Radius.With(hand, deck, graveyard, banished, extraDeck, field),
            unknownCardsInHand ?? UnknownCardsInHand,
            usedThisTurn ?? UsedThisTurn,
            flags ?? Flags);

    public bool IsUsed(string effectKey) => UsedThisTurn.Contains(effectKey);

    public GameState MarkUsed(string effectKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(effectKey);
        return With(usedThisTurn: UsedThisTurn.Add(effectKey));
    }

    public int GetFlag(string name) => Flags.TryGetValue(name, out var value) ? value : 0;

    /// <summary>Define um marcador. Valor 0 remove a chave, para que estados equivalentes tenham o mesmo hash.</summary>
    public GameState SetFlag(string name, int value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        return With(flags: value == 0 ? Flags.Remove(name) : Flags.SetItem(name, value));
    }

    /// <summary>
    /// Texto canônico do estado. Use como chave de memoização (sem risco de colisão).
    /// </summary>
    public string CanonicalKey => _key ??= BuildKey();

    private string BuildKey()
    {
        var sb = new StringBuilder();
        sb.Append(Radius.CanonicalKey);
        sb.Append("|U:").Append(UnknownCardsInHand);

        sb.Append("|T:");
        var first = true;
        foreach (var used in UsedThisTurn)
        {
            if (!first) sb.Append(',');
            sb.Append(used);
            first = false;
        }

        sb.Append("|L:");
        first = true;
        foreach (var (name, value) in Flags)
        {
            if (!first) sb.Append(',');
            sb.Append(name).Append('=').Append(value);
            first = false;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Fingerprint de 64 bits (FNV-1a sobre <see cref="CanonicalKey"/>), estável entre execuções.
    /// Compacto para logs e tabelas grandes; para memoização exata prefira <see cref="CanonicalKey"/>.
    /// </summary>
    public ulong GetHash()
    {
        unchecked
        {
            var hash = 14695981039346656037UL;
            foreach (var b in Encoding.UTF8.GetBytes(CanonicalKey))
            {
                hash ^= b;
                hash *= 1099511628211UL;
            }
            return hash;
        }
    }

    public bool Equals(GameState? other) =>
        other is not null && string.Equals(CanonicalKey, other.CanonicalKey, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as GameState);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(CanonicalKey);

    public override string ToString() => CanonicalKey;
}