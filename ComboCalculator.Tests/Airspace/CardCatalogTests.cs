using System.Text.Json;
using Airspace;
using Xunit;

namespace ComboCalculator.Tests;

public class CardCatalogTests
{
    private static CardData Carta(string id, CardKind kind, CardSubKind sub = CardSubKind.Normal) =>
        new CardData(
            id, id, kind, sub, IsExtra: false, Level: null, Race: null, Attribute: null,
            Archetypes: Array.Empty<string>(), Mentions: Array.Empty<string>(), Text: "");

    private static CardCatalog Catalogo() => new CardCatalog(new[]
    {
        Carta("mystical-space-typhoon", CardKind.Spell, CardSubKind.QuickPlay),
        Carta("upstart-goblin", CardKind.Spell),
        Carta("ash-blossom", CardKind.Monster, CardSubKind.Effect),
    });

    [Fact]
    public void CountSpells_ContaCopiasDeSpellsNoCemiterio()
    {
        var cemiterio = Multiset.Empty
            .Add("mystical-space-typhoon", 2)
            .Add("upstart-goblin")
            .Add("ash-blossom", 3);

        Assert.Equal(3, Catalogo().CountSpells(cemiterio));
    }

    [Fact]
    public void CountSpells_ComMenosDeTres_NaoAtingeOLimiteDoEngage()
    {
        var cemiterio = Multiset.Empty.Add("mystical-space-typhoon", 2);

        Assert.True(Catalogo().CountSpells(cemiterio) < 3);
    }

    [Fact]
    public void Distinct_DevolveUmaPorId_SemDuplicarCopias()
    {
        var deck = Multiset.Empty.Add("mystical-space-typhoon", 3).Add("ash-blossom", 3);

        var spells = Catalogo().Distinct(deck, c => c.IsSpell).ToList();

        Assert.Single(spells);
        Assert.Equal("mystical-space-typhoon", spells[0].Id);
    }

    [Fact]
    public void Get_IdInexistente_Lanca()
    {
        Assert.Throws<KeyNotFoundException>(() => Catalogo().Get("nao-existe"));
    }

    [Fact]
    public void Construtor_IdDuplicado_Lanca()
    {
        Assert.Throws<ArgumentException>(() => new CardCatalog(new[]
        {
            Carta("a", CardKind.Spell),
            Carta("a", CardKind.Monster),
        }));
    }

    [Fact]
    public void Json_LeEnumsComoTextoECamposEmCamelCase()
    {
        const string json = """
        [
          {
            "id": "sky-striker-mecha-hornet-drones",
            "name": "Sky Striker Mecha - Hornet Drones",
            "kind": "Spell",
            "subKind": "QuickPlay",
            "isExtra": false,
            "level": null,
            "race": null,
            "attribute": null,
            "archetypes": ["Sky Striker"],
            "mentions": [],
            "text": "texto"
          },
          {
            "id": "sky-striker-ace-kagari",
            "name": "Sky Striker Ace - Kagari",
            "kind": "Monster",
            "subKind": "Link",
            "isExtra": true,
            "level": 1,
            "race": "Machine",
            "attribute": "FIRE",
            "archetypes": ["Sky Striker", "Sky Striker Ace"],
            "mentions": [],
            "text": "texto"
          }
        ]
        """;

        var cartas = JsonSerializer.Deserialize<List<CardData>>(json, CardData.JsonOptions)!;

        Assert.Equal(2, cartas.Count);
        Assert.Equal(CardKind.Spell, cartas[0].Kind);
        Assert.Equal(CardSubKind.QuickPlay, cartas[0].SubKind);
        Assert.True(cartas[0].HasArchetype("Sky Striker"));
        Assert.Null(cartas[0].Level);

        Assert.Equal(CardSubKind.Link, cartas[1].SubKind);
        Assert.True(cartas[1].IsExtra);
        Assert.Equal(1, cartas[1].Level);
        Assert.Equal("FIRE", cartas[1].Attribute);
    }
}
