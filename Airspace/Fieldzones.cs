using System.Collections.Immutable;
using System.Text;

namespace Airspace;

public enum ZoneKind
{
    MainMonster,
    ExtraMonster,
    SpellTrap,
    Field
}

/// <summary>Carta posicionada no campo. <see cref="FaceUp"/> falso = Set / virada para baixo.</summary>
public sealed record FieldCard(string CardId, bool FaceUp = true)
{
    internal string Key => FaceUp ? CardId : CardId + "?";
}

/// <summary>
/// Tabuleiro de um jogador: 5 Main Monster Zones, 2 Extra Monster Zones, 5 Spell/Trap e 1 Field Zone.
/// Imutável: cada alteração devolve um novo <see cref="FieldZones"/>.
/// A posição do slot faz parte do estado (colunas importam em cartas como Infinite Impermanence).
/// </summary>
public sealed class FieldZones : IEquatable<FieldZones>
{
    public const int MainMonsterSlots = 5;
    public const int ExtraMonsterSlots = 1;
    public const int SpellTrapSlots = 5;
    public const int FieldSlots = 1;

    private static readonly ZoneKind[] AllZones =
    {
        ZoneKind.MainMonster, ZoneKind.ExtraMonster, ZoneKind.SpellTrap, ZoneKind.Field
    };

    public static readonly FieldZones Empty = new FieldZones(
        Blank(MainMonsterSlots), Blank(ExtraMonsterSlots), Blank(SpellTrapSlots), Blank(FieldSlots));

    private readonly ImmutableArray<FieldCard?> _mainMonster;
    private readonly ImmutableArray<FieldCard?> _extraMonster;
    private readonly ImmutableArray<FieldCard?> _spellTrap;
    private readonly ImmutableArray<FieldCard?> _field;
    private string? _key;

    private FieldZones(
        ImmutableArray<FieldCard?> mainMonster,
        ImmutableArray<FieldCard?> extraMonster,
        ImmutableArray<FieldCard?> spellTrap,
        ImmutableArray<FieldCard?> field)
    {
        _mainMonster = mainMonster;
        _extraMonster = extraMonster;
        _spellTrap = spellTrap;
        _field = field;
    }

    public static int SlotCount(ZoneKind zone) => zone switch
    {
        ZoneKind.MainMonster => MainMonsterSlots,
        ZoneKind.ExtraMonster => ExtraMonsterSlots,
        ZoneKind.SpellTrap => SpellTrapSlots,
        ZoneKind.Field => FieldSlots,
        _ => throw new ArgumentOutOfRangeException(nameof(zone))
    };

    private static ImmutableArray<FieldCard?> Blank(int size)
    {
        var builder = ImmutableArray.CreateBuilder<FieldCard?>(size);
        for (var i = 0; i < size; i++) builder.Add(null);
        return builder.ToImmutable();
    }

    private ImmutableArray<FieldCard?> Slots(ZoneKind zone) => zone switch
    {
        ZoneKind.MainMonster => _mainMonster,
        ZoneKind.ExtraMonster => _extraMonster,
        ZoneKind.SpellTrap => _spellTrap,
        ZoneKind.Field => _field,
        _ => throw new ArgumentOutOfRangeException(nameof(zone))
    };

    private FieldZones Replace(ZoneKind zone, ImmutableArray<FieldCard?> slots) => zone switch
    {
        ZoneKind.MainMonster => new FieldZones(slots, _extraMonster, _spellTrap, _field),
        ZoneKind.ExtraMonster => new FieldZones(_mainMonster, slots, _spellTrap, _field),
        ZoneKind.SpellTrap => new FieldZones(_mainMonster, _extraMonster, slots, _field),
        ZoneKind.Field => new FieldZones(_mainMonster, _extraMonster, _spellTrap, slots),
        _ => throw new ArgumentOutOfRangeException(nameof(zone))
    };

    private static void CheckSlot(ZoneKind zone, int slot)
    {
        var size = SlotCount(zone);
        if (slot < 0 || slot >= size)
            throw new ArgumentOutOfRangeException(
                nameof(slot), $"A zona {zone} tem {size} slot(s); recebido {slot}.");
    }

    public FieldCard? Get(ZoneKind zone, int slot)
    {
        CheckSlot(zone, slot);
        return Slots(zone)[slot];
    }

    public bool IsEmpty(ZoneKind zone, int slot) => Get(zone, slot) is null;

    /// <summary>Quantidade de slots ocupados na zona.</summary>
    public int Count(ZoneKind zone)
    {
        var slots = Slots(zone);
        var total = 0;
        for (var i = 0; i < slots.Length; i++)
            if (slots[i] is not null) total++;
        return total;
    }

    /// <summary>Primeiro slot livre da zona, ou null se estiver cheia.</summary>
    public int? FirstEmptySlot(ZoneKind zone)
    {
        var slots = Slots(zone);
        for (var i = 0; i < slots.Length; i++)
            if (slots[i] is null) return i;
        return null;
    }

    public IEnumerable<(ZoneKind Zone, int Slot, FieldCard Card)> AllCards()
    {
        foreach (var zone in AllZones)
        {
            var slots = Slots(zone);
            for (var i = 0; i < slots.Length; i++)
                if (slots[i] is { } card) yield return (zone, i, card);
        }
    }

    public FieldZones Place(ZoneKind zone, int slot, FieldCard card)
    {
        ArgumentNullException.ThrowIfNull(card);
        CheckSlot(zone, slot);

        var slots = Slots(zone);
        if (slots[slot] is { } occupant)
            throw new InvalidOperationException(
                $"O slot {slot} de {zone} já está ocupado por '{occupant.CardId}'.");

        return Replace(zone, slots.SetItem(slot, card));
    }

    public FieldZones Remove(ZoneKind zone, int slot)
    {
        CheckSlot(zone, slot);

        var slots = Slots(zone);
        if (slots[slot] is null)
            throw new InvalidOperationException($"O slot {slot} de {zone} está vazio.");

        return Replace(zone, slots.SetItem(slot, null));
    }

    /// <summary>Texto canônico do tabuleiro; "-" marca slot vazio e "?" marca carta virada para baixo.</summary>
    public string CanonicalKey => _key ??= BuildKey();

    private string BuildKey()
    {
        var sb = new StringBuilder();
        AppendZone(sb, "M", _mainMonster);
        AppendZone(sb, "E", _extraMonster);
        AppendZone(sb, "S", _spellTrap);
        AppendZone(sb, "F", _field);
        return sb.ToString();
    }

    private static void AppendZone(StringBuilder sb, string label, ImmutableArray<FieldCard?> slots)
    {
        sb.Append(label).Append('[');
        for (var i = 0; i < slots.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(slots[i]?.Key ?? "-");
        }
        sb.Append(']');
    }

    public bool Equals(FieldZones? other) =>
        other is not null && string.Equals(CanonicalKey, other.CanonicalKey, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as FieldZones);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(CanonicalKey);

    public override string ToString() => CanonicalKey;
}