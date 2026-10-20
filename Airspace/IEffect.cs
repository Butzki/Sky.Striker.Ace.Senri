namespace Airspace;

/// <summary>
/// Contrato de um efeito de carta. Uma classe por efeito, com o nome real da carta.
/// Todos os métodos são funções puras: nunca alteram o <see cref="GameState"/> recebido, devolvem um novo.
///
/// A ativação é dividida em duas etapas, como no jogo:
/// <list type="bullet">
///   <item><see cref="Activate"/> paga custos e escolhe alvos. A carta ativada ainda NÃO foi para o cemitério.</item>
///   <item><see cref="Resolve"/> relê o estado e aplica o efeito. É aqui que condições como
///   "3 ou mais Spells no cemitério" são avaliadas.</item>
/// </list>
/// Sem chain, as duas rodam em sequência (<see cref="EffectExtensions.ActivateAndResolve"/>). Com chain,
/// outras ativações podem acontecer entre elas, e o estado em <see cref="Resolve"/> pode ser diferente do de
/// <see cref="Activate"/>. É isso que faz The Fallen &amp; The Virtuous, em resposta a Engage!, render a compra.
/// </summary>
public interface IEffect
{
    /// <summary>Id da carta dona do efeito.</summary>
    string CardId { get; }

    /// <summary>Número do efeito na carta, começando em 1 (como numerado no texto).</summary>
    int EffectNumber { get; }

    /// <summary>
    /// Verdadeiro se o efeito pode ser ativado agora: condição de ativação, custos pagáveis, limite de uma vez
    /// por turno e ao menos uma escolha legal. Deve concordar com <see cref="GetActions"/>:
    /// <c>CanActivate(s) == (GetActions(s).Count &gt; 0)</c>.
    /// </summary>
    bool CanActivate(GameState state);

    /// <summary>
    /// Uma ação por escolha distinta e legal (Engage! gera uma por carta Sky Striker diferente no Deck, não uma por
    /// cópia). Sem repetições, em ordem natural de <see cref="GameAction"/>. Vazia quando não pode ativar.
    /// </summary>
    IReadOnlyList<GameAction> GetActions(GameState state);

    /// <summary>
    /// Etapa de ativação: paga os custos, move a carta ativada para onde ela fica enquanto resolve e registra
    /// o uso em <see cref="GameState.UsedThisTurn"/> quando o efeito tem limite por turno. Não aplica o efeito
    /// e não lê condições de resolução.
    /// </summary>
    /// <exception cref="InvalidOperationException">A ação não está em <see cref="GetActions"/> para este estado.</exception>
    GameState Activate(GameState state, GameAction action);

    /// <summary>
    /// Etapa de resolução: relê o estado ATUAL, aplica o efeito e leva a carta ativada ao destino final
    /// (ex.: cemitério). Se o alvo ou a condição não valem mais, o efeito não faz nada, mas a carta ainda sai
    /// de onde estava resolvendo. Não valida a ação: ela já foi validada em <see cref="Activate"/>.
    /// </summary>
    GameState Resolve(GameState state, GameAction action);
}

public static class EffectExtensions
{
    /// <summary>Chave do efeito (carta + número), a mesma de <see cref="GameAction.EffectKey"/>.</summary>
    public static string GetEffectKey(this IEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        return GameAction.EffectKeyFor(effect.CardId, effect.EffectNumber);
    }

    /// <summary>Ativação isolada, sem chain: <see cref="IEffect.Activate"/> seguido de <see cref="IEffect.Resolve"/>.</summary>
    public static GameState ActivateAndResolve(this IEffect effect, GameState state, GameAction action)
    {
        ArgumentNullException.ThrowIfNull(effect);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        return effect.Resolve(effect.Activate(state, action), action);
    }
}
