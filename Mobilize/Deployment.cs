using Airspace;

namespace Mobilize;

/// <summary>Decklist inválida. <see cref="Errors"/> traz todos os problemas encontrados, não só o primeiro.</summary>
public sealed class DeckValidationException : Exception
{
    public DeckValidationException(IReadOnlyList<string> errors)
        : base("Decklist inválida:" + Environment.NewLine +
               string.Join(Environment.NewLine, errors.Select(e => "- " + e)))
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }
}

/// <summary>
/// Valida a decklist (40 cartas no Main, 15 no Extra, no máximo 3 cópias por carta) e a converte em <see cref="Deck"/>.
/// A montagem da mão inicial entra numa etapa seguinte.
/// </summary>
public static class Deployment
{
    public const int MainDeckSize = 40;
    public const int ExtraDeckSize = 15;
    public const int MaxCopies = 3;

    /// <summary>Lista de problemas da decklist; vazia quando ela é válida.</summary>
    public static IReadOnlyList<string> Validate(Decklist decklist, CardCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(decklist);
        ArgumentNullException.ThrowIfNull(catalog);

        var errors = new List<string>();
        var main = Tally(decklist.Main, "Main Deck", catalog, wantExtra: false, errors);
        var extra = Tally(decklist.Extra, "Extra Deck", catalog, wantExtra: true, errors);

        if (main.Total != MainDeckSize)
            errors.Add($"Main Deck tem {main.Total} cartas; esperado {MainDeckSize}.");
        if (extra.Total != ExtraDeckSize)
            errors.Add($"Extra Deck tem {extra.Total} cartas; esperado {ExtraDeckSize}.");

        // O limite de 3 vale por nome, somando Main e Extra.
        var ids = main.DistinctIds.Concat(extra.DistinctIds)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal);

        foreach (var id in ids)
        {
            var copies = main.Count(id) + extra.Count(id);
            if (copies > MaxCopies)
                errors.Add($"'{NameOf(catalog, id)}' tem {copies} cópias; o limite é {MaxCopies}.");
        }

        return errors;
    }

    /// <summary>Valida e devolve o <see cref="Deck"/>; lança <see cref="DeckValidationException"/> se houver erros.</summary>
    public static Deck CreateDeck(Decklist decklist, CardCatalog catalog)
    {
        var errors = Validate(decklist, catalog);
        if (errors.Count > 0)
            throw new DeckValidationException(errors);

        return new Deck(decklist.Name, ToMultiset(decklist.Main), ToMultiset(decklist.Extra));
    }

    // Soma as cópias de uma lista e registra os erros de cada entrada.
    // Entradas sem id, com cópias inválidas ou inexistentes não entram na contagem.
    private static Multiset Tally(
        IReadOnlyList<DeckEntry> entries,
        string listName,
        CardCatalog catalog,
        bool wantExtra,
        List<string> errors)
    {
        var result = Multiset.Empty;

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Id))
            {
                errors.Add($"{listName}: entrada sem 'id'.");
                continue;
            }

            if (entry.Copies <= 0)
            {
                errors.Add($"{listName}: '{entry.Id}' tem copies = {entry.Copies}; deve ser 1 ou mais.");
                continue;
            }

            if (!catalog.TryGet(entry.Id, out var card))
            {
                errors.Add($"{listName}: a carta '{entry.Id}' não existe no cards.json.");
                continue;
            }

            // A carta na lista errada é apontada, mas continua contando, para o total não gerar um segundo erro.
            if (card.IsExtra != wantExtra)
            {
                errors.Add(wantExtra
                    ? $"{card.Name} é carta de Main Deck, mas está no Extra Deck."
                    : $"{card.Name} é carta de Extra Deck, mas está no Main Deck.");
            }

            result = result.Add(entry.Id, entry.Copies);
        }

        return result;
    }

    private static Multiset ToMultiset(IReadOnlyList<DeckEntry> entries)
    {
        var result = Multiset.Empty;
        foreach (var entry in entries)
            result = result.Add(entry.Id, entry.Copies);
        return result;
    }

    private static string NameOf(CardCatalog catalog, string id) =>
        catalog.TryGet(id, out var card) ? card.Name : id;
}
