using System.Text.Json;
using System.Text.Json.Serialization;

namespace Airspace;

public enum CardKind
{
    Monster,
    Spell,
    Trap
}

public enum CardSubKind
{
    // Spell / Trap
    Normal,
    QuickPlay,
    Continuous,
    Field,
    Counter,

    // Monster
    Effect,
    Fusion,
    Link
}

/// <summary>
/// Dados estáticos de uma carta, lidos de <c>cards.json</c>.
/// Nos Links, <see cref="Level"/> guarda o Link Rating.
/// </summary>
public sealed record CardData(
    string Id,
    string Name,
    CardKind Kind,
    CardSubKind SubKind,
    bool IsExtra,
    int? Level,
    string? Race,
    string? Attribute,
    IReadOnlyList<string> Archetypes,
    IReadOnlyList<string> Mentions,
    string Text)
{
    public bool IsMonster => Kind == CardKind.Monster;
    public bool IsSpell => Kind == CardKind.Spell;
    public bool IsTrap => Kind == CardKind.Trap;

    public bool HasArchetype(string archetype) =>
        Archetypes.Contains(archetype, StringComparer.Ordinal);

    /// <summary>Verdadeiro se o texto da carta menciona o nome informado (campo "mentions").</summary>
    public bool MentionsName(string cardName) =>
        Mentions.Contains(cardName, StringComparer.Ordinal);

    /// <summary>Opções para ler cards.json (camelCase, enums como texto).</summary>
    public static JsonSerializerOptions JsonOptions { get; } = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}