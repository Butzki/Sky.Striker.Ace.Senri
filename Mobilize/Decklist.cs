using Airspace;

namespace Mobilize;

/// <summary>Uma linha da decklist: id da carta e número de cópias.</summary>
public sealed record DeckEntry(string Id, int Copies);

/// <summary>
/// Decklist como foi lida do JSON, ainda sem validação.
/// Formato: { "name": "...", "main": [ { "id": "...", "copies": 3 } ], "extra": [ ... ] }
/// </summary>
public sealed record Decklist(
    string Name,
    IReadOnlyList<DeckEntry> Main,
    IReadOnlyList<DeckEntry> Extra);

/// <summary>Decklist já validada, com Main e Extra como <see cref="Multiset"/> (id -> cópias).</summary>
public sealed record Deck(string Name, Multiset Main, Multiset Extra);
