using System.Collections.Immutable;
using System.Text;

namespace Airspace;

/// <summary>
/// Uma jogada concreta: qual efeito de qual carta, mais as escolhas feitas (carta buscada, alvo, custo).
/// Duas ações com o mesmo efeito e as mesmas escolhas são iguais, e a <see cref="CanonicalKey"/> é estável.
/// Por isso a ordem natural das ações é determinística (base do desempate do solver).
/// Exemplo de chave: "sky-striker-mobilize-engage#1|search=sky-striker-ace-raye".
/// </summary>
public sealed class GameAction : IEquatable<GameAction>, IComparable<GameAction>
{
    // Caracteres usados para montar a chave canônica; não podem aparecer em ids, nomes ou valores.
    private static readonly char[] ReservedChars = { '|', ',', '=', '#' };

    private string? _key;

    /// <param name="cardId">Id da carta dona do efeito.</param>
    /// <param name="effectNumber">Número do efeito na carta, começando em 1 (como numerado no texto).</param>
    /// <param name="choices">Escolhas nomeadas (ex.: search = id da carta buscada). Nomes não podem se repetir.</param>
    public GameAction(string cardId, int effectNumber, IEnumerable<KeyValuePair<string, string>>? choices = null)
    {
        RequireToken(cardId, nameof(cardId));
        if (effectNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(effectNumber), "O número do efeito começa em 1.");

        var builder = ImmutableSortedDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        if (choices is not null)
        {
            foreach (var (name, value) in choices)
            {
                RequireToken(name, nameof(choices));
                RequireToken(value, nameof(choices));
                if (builder.ContainsKey(name))
                    throw new ArgumentException($"A escolha '{name}' aparece mais de uma vez.", nameof(choices));
                builder[name] = value;
            }
        }

        CardId = cardId;
        EffectNumber = effectNumber;
        Choices = builder.ToImmutable();
    }

    public string CardId { get; }

    public int EffectNumber { get; }

    /// <summary>Identifica o efeito (carta + número). É a chave usada em <see cref="GameState.UsedThisTurn"/>.</summary>
    public string EffectKey => EffectKeyFor(CardId, EffectNumber);

    /// <summary>Escolhas feitas, ordenadas pelo nome.</summary>
    public ImmutableSortedDictionary<string, string> Choices { get; }

    public static string EffectKeyFor(string cardId, int effectNumber) => $"{cardId}#{effectNumber}";

    /// <summary>Valor da escolha ou null se ela não foi feita.</summary>
    public string? GetChoice(string name) => Choices.TryGetValue(name, out var value) ? value : null;

    /// <summary>Devolve uma nova ação com a escolha acrescentada (ou com o valor trocado, se o nome já existe).</summary>
    public GameAction WithChoice(string name, string value)
    {
        RequireToken(name, nameof(name));
        RequireToken(value, nameof(value));
        return new GameAction(CardId, EffectNumber, Choices.SetItem(name, value));
    }

    /// <summary>Texto canônico: efeito, depois cada escolha por ordem de nome ("x#1|search=a|target=b").</summary>
    public string CanonicalKey => _key ??= BuildKey();

    private string BuildKey()
    {
        var sb = new StringBuilder(EffectKey);
        foreach (var (name, value) in Choices)
            sb.Append('|').Append(name).Append('=').Append(value);
        return sb.ToString();
    }

    private static void RequireToken(string? value, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
        if (value.IndexOfAny(ReservedChars) >= 0)
            throw new ArgumentException($"'{value}' contém um caractere reservado (| , = #).", paramName);
    }

    public bool Equals(GameAction? other) =>
        other is not null && string.Equals(CanonicalKey, other.CanonicalKey, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as GameAction);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(CanonicalKey);

    /// <summary>Ordem natural: alfabética pela chave canônica (desempate determinístico).</summary>
    public int CompareTo(GameAction? other) =>
        other is null ? 1 : string.CompareOrdinal(CanonicalKey, other.CanonicalKey);

    public override string ToString() => CanonicalKey;
}
