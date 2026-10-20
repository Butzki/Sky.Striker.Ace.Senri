using System.Text.Json;
using Airspace;

namespace Mobilize;

/// <summary>Falha ao ler ou interpretar um arquivo de dados (cards.json ou decklist).</summary>
public sealed class LogisticsException : Exception
{
    public LogisticsException(string message) : base(message) { }

    public LogisticsException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Carrega cards.json e decklists. Só lê e confere o formato; as regras da decklist
/// (40/15/3 cópias) ficam em <see cref="Deployment"/>.
/// </summary>
public static class Logistics
{
    private static readonly JsonSerializerOptions DecklistOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public static CardCatalog LoadCards(string path) => ParseCards(ReadFile(path), path);

    public static Decklist LoadDecklist(string path) => ParseDecklist(ReadFile(path), path);

    /// <param name="source">Nome mostrado nas mensagens de erro (normalmente o caminho do arquivo).</param>
    public static CardCatalog ParseCards(string json, string source = "cards.json")
    {
        ArgumentNullException.ThrowIfNull(json);

        List<CardData>? cards;
        try
        {
            cards = JsonSerializer.Deserialize<List<CardData>>(json, CardData.JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new LogisticsException($"{source}: JSON inválido. {ex.Message}", ex);
        }

        if (cards is null || cards.Count == 0)
            throw new LogisticsException($"{source}: nenhuma carta encontrada (o JSON está vazio ou é 'null').");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var normalized = new List<CardData>(cards.Count);

        for (var i = 0; i < cards.Count; i++)
        {
            var card = cards[i];

            if (card is null)
                throw new LogisticsException($"{source}: a carta na posição {i + 1} é null.");
            if (string.IsNullOrWhiteSpace(card.Id))
                throw new LogisticsException($"{source}: a carta na posição {i + 1} não tem 'id'.");
            if (string.IsNullOrWhiteSpace(card.Name))
                throw new LogisticsException($"{source}: a carta '{card.Id}' não tem 'name'.");
            if (!seen.Add(card.Id))
                throw new LogisticsException($"{source}: o id '{card.Id}' aparece mais de uma vez.");

            // Campos de lista ou texto ausentes no JSON viram vazios, para não gerar NullReference depois.
            normalized.Add(card with
            {
                Archetypes = card.Archetypes ?? Array.Empty<string>(),
                Mentions = card.Mentions ?? Array.Empty<string>(),
                Text = card.Text ?? ""
            });
        }

        return new CardCatalog(normalized);
    }

    /// <param name="source">Nome mostrado nas mensagens de erro (normalmente o caminho do arquivo).</param>
    public static Decklist ParseDecklist(string json, string source = "decklist.json")
    {
        ArgumentNullException.ThrowIfNull(json);

        Decklist? deck;
        try
        {
            deck = JsonSerializer.Deserialize<Decklist>(json, DecklistOptions);
        }
        catch (JsonException ex)
        {
            throw new LogisticsException($"{source}: JSON inválido. {ex.Message}", ex);
        }

        if (deck is null)
            throw new LogisticsException($"{source}: o JSON está vazio ou é 'null'.");

        return new Decklist(
            string.IsNullOrWhiteSpace(deck.Name) ? "(sem nome)" : deck.Name,
            RequireEntries(deck.Main, "main", source),
            RequireEntries(deck.Extra, "extra", source));
    }

    // Lista ausente vira vazia; a contagem errada aparece depois, na validação (Deployment).
    private static IReadOnlyList<DeckEntry> RequireEntries(IReadOnlyList<DeckEntry>? entries, string listName, string source)
    {
        if (entries is null)
            return Array.Empty<DeckEntry>();

        for (var i = 0; i < entries.Count; i++)
        {
            if (entries[i] is null)
                throw new LogisticsException($"{source}: a entrada {i + 1} de '{listName}' é null.");
        }

        return entries;
    }

    private static string ReadFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new LogisticsException($"Arquivo não encontrado: {path}", ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new LogisticsException($"Não foi possível ler '{path}': {ex.Message}", ex);
        }
    }
}
