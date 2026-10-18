using Airspace;
using Xunit;

namespace ComboCalculator.Tests;

public class GameStateTests
{
    private static GameState NovoEstado(params string[] mao) =>
        GameState.Initial(
            hand: Multiset.FromIds(mao),
            deck: Multiset.Empty.Add("mystical-space-typhoon", 3),
            extraDeck: Multiset.Empty.Add("sky-striker-ace-zero"));

    [Fact]
    public void EstadosEquivalentes_ProduzemOMesmoHash()
    {
        var a = NovoEstado("raye", "roze", "raye");
        var b = NovoEstado("roze", "raye", "raye");

        Assert.Equal(a.CanonicalKey, b.CanonicalKey);
        Assert.Equal(a.GetHash(), b.GetHash());
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Hash_EstavelParaOMesmoEstado()
    {
        var estado = NovoEstado("raye");

        Assert.Equal(estado.GetHash(), estado.GetHash());
    }

    [Fact]
    public void Hash_IncluiUsedThisTurn()
    {
        var a = NovoEstado("raye");
        var b = a.MarkUsed("sky-striker-mobilize-engage");

        Assert.NotEqual(a.CanonicalKey, b.CanonicalKey);
        Assert.NotEqual(a.GetHash(), b.GetHash());
    }

    [Fact]
    public void Hash_IncluiFlags()
    {
        var a = NovoEstado("raye");
        var b = a.SetFlag("sem-special-summon-do-extra", 1);

        Assert.NotEqual(a.CanonicalKey, b.CanonicalKey);
        Assert.NotEqual(a.GetHash(), b.GetHash());
    }

    [Fact]
    public void Hash_IncluiUnknownCardsInHand()
    {
        var a = NovoEstado("raye");
        var b = a.With(unknownCardsInHand: 1);

        Assert.NotEqual(a.CanonicalKey, b.CanonicalKey);
        Assert.NotEqual(a.GetHash(), b.GetHash());
    }

    [Fact]
    public void Hash_IncluiTabuleiro()
    {
        var a = NovoEstado("raye");
        var b = a.With(field: a.Field.Place(ZoneKind.MainMonster, 0, new FieldCard("roze")));

        Assert.NotEqual(a.GetHash(), b.GetHash());
    }

    [Fact]
    public void SetFlag_ZeroRemoveAChave_EVoltaAoHashOriginal()
    {
        var a = NovoEstado("raye");
        var b = a.SetFlag("x", 1).SetFlag("x", 0);

        Assert.Equal(a.GetHash(), b.GetHash());
        Assert.Equal(0, b.GetFlag("x"));
    }

    [Fact]
    public void MarkUsed_OrdemNaoImporta()
    {
        var a = NovoEstado("raye").MarkUsed("a").MarkUsed("b");
        var b = NovoEstado("raye").MarkUsed("b").MarkUsed("a");

        Assert.Equal(a.GetHash(), b.GetHash());
    }

    [Fact]
    public void With_NaoMutaOEstadoOriginal()
    {
        var original = NovoEstado("raye");
        var chaveAntes = original.CanonicalKey;

        _ = original.With(
            hand: Multiset.Empty,
            graveyard: Multiset.Empty.Add("raye"),
            unknownCardsInHand: 2);
        _ = original.MarkUsed("x").SetFlag("y", 1);

        Assert.Equal(chaveAntes, original.CanonicalKey);
        Assert.Equal(1, original.Hand.Count("raye"));
        Assert.Equal(0, original.UnknownCardsInHand);
        Assert.False(original.IsUsed("x"));
        Assert.Equal(0, original.GetFlag("y"));
    }

    [Fact]
    public void With_ArgumentoOmitido_MantemOValorAtual()
    {
        var original = NovoEstado("raye");
        var novo = original.With(unknownCardsInHand: 1);

        Assert.Equal(original.Hand, novo.Hand);
        Assert.Equal(original.Deck, novo.Deck);
        Assert.Equal(original.ExtraDeck, novo.ExtraDeck);
    }

    [Fact]
    public void Initial_CampoCemiterioEBanidasComecamVazios()
    {
        var estado = NovoEstado("raye");

        Assert.True(estado.Graveyard.IsEmpty);
        Assert.True(estado.Banished.IsEmpty);
        Assert.Empty(estado.Field.AllCards());
        Assert.Empty(estado.UsedThisTurn);
        Assert.Empty(estado.Flags);
    }

    [Fact]
    public void UnknownCardsInHand_Negativo_Lanca()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NovoEstado("raye").With(unknownCardsInHand: -1));
    }
}
