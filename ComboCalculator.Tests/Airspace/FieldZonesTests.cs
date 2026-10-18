using Airspace;
using Xunit;

namespace ComboCalculator.Tests;

public class FieldZonesTests
{
    [Fact]
    public void Empty_NaoTemCartas()
    {
        Assert.Empty(FieldZones.Empty.AllCards());
        Assert.Equal(0, FieldZones.Empty.Count(ZoneKind.MainMonster));
    }

    [Fact]
    public void Place_ColocaACartaNoSlot()
    {
        var zones = FieldZones.Empty.Place(ZoneKind.MainMonster, 2, new FieldCard("raye"));

        Assert.Equal("raye", zones.Get(ZoneKind.MainMonster, 2)!.CardId);
        Assert.Equal(1, zones.Count(ZoneKind.MainMonster));
    }

    [Fact]
    public void Place_NaoAlteraOOriginal()
    {
        var original = FieldZones.Empty;
        _ = original.Place(ZoneKind.SpellTrap, 0, new FieldCard("mystical-space-typhoon", FaceUp: false));

        Assert.Equal(0, original.Count(ZoneKind.SpellTrap));
    }

    [Fact]
    public void Place_SlotOcupado_Lanca()
    {
        var zones = FieldZones.Empty.Place(ZoneKind.MainMonster, 0, new FieldCard("raye"));

        Assert.Throws<InvalidOperationException>(
            () => zones.Place(ZoneKind.MainMonster, 0, new FieldCard("roze")));
    }

    [Fact]
    public void Place_SlotForaDoLimite_Lanca()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FieldZones.Empty.Place(ZoneKind.ExtraMonster, 2, new FieldCard("x")));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FieldZones.Empty.Place(ZoneKind.Field, 1, new FieldCard("x")));
    }

    [Fact]
    public void Remove_EsvaziaOSlot()
    {
        var zones = FieldZones.Empty
            .Place(ZoneKind.MainMonster, 1, new FieldCard("raye"))
            .Remove(ZoneKind.MainMonster, 1);

        Assert.True(zones.IsEmpty(ZoneKind.MainMonster, 1));
        Assert.Equal(FieldZones.Empty.CanonicalKey, zones.CanonicalKey);
    }

    [Fact]
    public void Remove_SlotVazio_Lanca()
    {
        Assert.Throws<InvalidOperationException>(() => FieldZones.Empty.Remove(ZoneKind.SpellTrap, 0));
    }

    [Fact]
    public void FirstEmptySlot_PulaOcupados_ENullQuandoCheia()
    {
        var zones = FieldZones.Empty;
        Assert.Equal(0, zones.FirstEmptySlot(ZoneKind.Field));

        zones = zones.Place(ZoneKind.Field, 0, new FieldCard("area-zero"));
        Assert.Null(zones.FirstEmptySlot(ZoneKind.Field));

        zones = FieldZones.Empty.Place(ZoneKind.SpellTrap, 0, new FieldCard("a"));
        Assert.Equal(1, zones.FirstEmptySlot(ZoneKind.SpellTrap));
    }

    [Fact]
    public void CanonicalKey_DiferenciaFaceUpDeFaceDown()
    {
        var up = FieldZones.Empty.Place(ZoneKind.SpellTrap, 0, new FieldCard("x", FaceUp: true));
        var down = FieldZones.Empty.Place(ZoneKind.SpellTrap, 0, new FieldCard("x", FaceUp: false));

        Assert.NotEqual(up.CanonicalKey, down.CanonicalKey);
    }

    [Fact]
    public void Equals_MesmoConteudo_SaoIguais()
    {
        var a = FieldZones.Empty
            .Place(ZoneKind.MainMonster, 0, new FieldCard("raye"))
            .Place(ZoneKind.SpellTrap, 1, new FieldCard("x", FaceUp: false));
        var b = FieldZones.Empty
            .Place(ZoneKind.SpellTrap, 1, new FieldCard("x", FaceUp: false))
            .Place(ZoneKind.MainMonster, 0, new FieldCard("raye"));

        Assert.Equal(a, b);
    }
}
