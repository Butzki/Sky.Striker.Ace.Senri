namespace Airspace;

/// <summary>
/// Consulta de <see cref="CardData"/> por id. O <see cref="GameState"/> guarda só ids;
/// quem precisa saber "o que é essa carta" (ex.: contar Spells no cemitério) pergunta aqui.
/// </summary>
public sealed class CardCatalog
{
    private readonly Dictionary<string, CardData> _byId = new(StringComparer.Ordinal);

    public CardCatalog(IEnumerable<CardData> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);
        foreach (var card in cards)
        {
            if (!_byId.TryAdd(card.Id, card))
                throw new ArgumentException($"Id de carta duplicado em cards.json: '{card.Id}'.", nameof(cards));
        }
    }

    public IEnumerable<CardData> All => _byId.Values;

    public bool Contains(string id) => _byId.ContainsKey(id);

    public bool TryGet(string id, out CardData card) => _byId.TryGetValue(id, out card!);

    public CardData Get(string id) =>
        _byId.TryGetValue(id, out var card)
            ? card
            : throw new KeyNotFoundException($"Carta '{id}' não existe em cards.json.");

    /// <summary>Total de cartas do conjunto que satisfazem o filtro (conta cópias).</summary>
    public int Count(Multiset set, Func<CardData, bool> predicate)
    {
        var total = 0;
        foreach (var (id, quantity) in set.Entries)
            if (predicate(Get(id))) total += quantity;
        return total;
    }

    public int CountSpells(Multiset set) => Count(set, c => c.IsSpell);

    /// <summary>Ids distintos do conjunto que satisfazem o filtro, em ordem ordinal.</summary>
    public IEnumerable<CardData> Distinct(Multiset set, Func<CardData, bool> predicate)
    {
        foreach (var id in set.DistinctIds)
        {
            var card = Get(id);
            if (predicate(card)) yield return card;
        }
    }
}