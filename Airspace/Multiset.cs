namespace Airspace;
using System.Collections.Immutable;
using System.Text;

//Multiset: wrapper sobre ImmutableSortedDictionary<string,int> (Id → quantidade), com Add, Remove, Count e uma chave canônica em string.
//CardData (record), FieldZones (Main Monster Zones, EMZ, S/T, Field), GameState (record) com mão, deck, cemitério, banidas, campo, Extra Deck (lista), UnknownCardsInHand, UsedThisTurn e Flags.
//GameState.With(...) e GetHash(). O hash deve incluir UsedThisTurn, Flags e UnknownCardsInHand, como pede a auditoria do README.

/// <summary>
/// Coleção imutável de ids de carta com quantidade (Id -> cópias).
/// A ordem não existe: duas coleções com o mesmo conteúdo são iguais e têm a mesma
/// <see cref="CanonicalKey"/>, qualquer que seja a ordem em que foram montadas.
/// Usada para deck, mão, cemitério, banidas e Extra Deck.
/// </summary>
public sealed class Multiset : IEquatable<Multiset>
{
    public static readonly Multiset Empty =
        new Multiset(ImmutableSortedDictionary.Create<string, int>(StringComparer.Ordinal));

    private readonly ImmutableSortedDictionary<string, int> _items;
    private string? _key;

    private Multiset(ImmutableSortedDictionary<string, int> items)
    {
        _items = items;
        var total = 0;
        foreach (var quantity in items.Values) total += quantity;
        Total = total;
    }

    /// <summary>Número total de cartas (soma das cópias).</summary>
    public int Total { get; }

    public bool IsEmpty => Total == 0;

    /// <summary>Quantidade de ids distintos.</summary>
    public int DistinctCount => _items.Count;

    /// <summary>Ids distintos, em ordem ordinal (determinística).</summary>
    public IEnumerable<string> DistinctIds => _items.Keys;

    public IEnumerable<KeyValuePair<string, int>> Entries => _items;

    public int Count(string id) => _items.TryGetValue(id, out var quantity) ? quantity : 0;

    public bool Contains(string id) => _items.ContainsKey(id);

    public Multiset Add(string id, int quantity = 1)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "A quantidade deve ser positiva.");

        return new Multiset(_items.SetItem(id, Count(id) + quantity));
    }

    public Multiset Remove(string id, int quantity = 1)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "A quantidade deve ser positiva.");

        var current = Count(id);
        if (current < quantity)
            throw new InvalidOperationException(
                $"Não é possível remover {quantity} cópia(s) de '{id}': há {current}.");

        // Zerar remove a chave, para que estados equivalentes tenham a mesma chave canônica.
        return current == quantity
            ? new Multiset(_items.Remove(id))
            : new Multiset(_items.SetItem(id, current - quantity));
    }

    public static Multiset FromIds(IEnumerable<string> ids)
    {
        var result = Empty;
        foreach (var id in ids) result = result.Add(id);
        return result;
    }

    public static Multiset FromEntries(IEnumerable<KeyValuePair<string, int>> entries)
    {
        var result = Empty;
        foreach (var (id, quantity) in entries) result = result.Add(id, quantity);
        return result;
    }

    /// <summary>Texto canônico, ex.: "ash:2,raye:3" (ids em ordem ordinal).</summary>
    public string CanonicalKey => _key ??= BuildKey();

    private string BuildKey()
    {
        var sb = new StringBuilder();
        foreach (var (id, quantity) in _items)
        {
            if (sb.Length > 0) sb.Append(',');
            sb.Append(id).Append(':').Append(quantity);
        }
        return sb.ToString();
    }

    public bool Equals(Multiset? other) =>
        other is not null && string.Equals(CanonicalKey, other.CanonicalKey, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as Multiset);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(CanonicalKey);

    public override string ToString() => CanonicalKey;
}