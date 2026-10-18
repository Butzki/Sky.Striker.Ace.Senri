using Airspace;
using Xunit;

namespace ComboCalculator.Tests;

public class MultisetTests
{
    [Fact]
    public void Add_MesmaCartaDuasVezes_SomaQuantidade()
    {
        var m = Multiset.Empty.Add("raye").Add("raye");

        Assert.Equal(2, m.Count("raye"));
        Assert.Equal(2, m.Total);
        Assert.Equal(1, m.DistinctCount);
    }

    [Fact]
    public void Add_NaoAlteraOOriginal()
    {
        var original = Multiset.Empty.Add("raye");
        var alterado = original.Add("roze");

        Assert.Equal(1, original.Total);
        Assert.False(original.Contains("roze"));
        Assert.Equal(2, alterado.Total);
    }

    [Fact]
    public void Add_QuantidadeNaoPositiva_Lanca()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Multiset.Empty.Add("raye", 0));
    }

    [Fact]
    public void Remove_ReduzQuantidade()
    {
        var m = Multiset.Empty.Add("raye", 3).Remove("raye");

        Assert.Equal(2, m.Count("raye"));
    }

    [Fact]
    public void Remove_ZerarRemoveAChave()
    {
        var m = Multiset.Empty.Add("raye").Remove("raye");

        Assert.False(m.Contains("raye"));
        Assert.True(m.IsEmpty);
        Assert.Equal(Multiset.Empty.CanonicalKey, m.CanonicalKey);
    }

    [Fact]
    public void Remove_MaisDoQueExiste_Lanca()
    {
        var m = Multiset.Empty.Add("raye");

        Assert.Throws<InvalidOperationException>(() => m.Remove("raye", 2));
        Assert.Throws<InvalidOperationException>(() => m.Remove("roze"));
    }

    [Fact]
    public void Count_CartaAusente_EhZero()
    {
        Assert.Equal(0, Multiset.Empty.Count("qualquer"));
    }

    [Fact]
    public void CanonicalKey_NaoDependeDaOrdemDeInsercao()
    {
        var a = Multiset.Empty.Add("roze").Add("raye").Add("raye");
        var b = Multiset.Empty.Add("raye").Add("raye").Add("roze");

        Assert.Equal("raye:2,roze:1", a.CanonicalKey);
        Assert.Equal(a.CanonicalKey, b.CanonicalKey);
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_ConteudosDiferentes_SaoDiferentes()
    {
        var a = Multiset.Empty.Add("raye", 2);
        var b = Multiset.Empty.Add("raye", 3);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void FromEntries_MontaComAsQuantidades()
    {
        var m = Multiset.FromEntries(new Dictionary<string, int> { ["a"] = 3, ["b"] = 1 });

        Assert.Equal(4, m.Total);
        Assert.Equal(3, m.Count("a"));
    }
}
