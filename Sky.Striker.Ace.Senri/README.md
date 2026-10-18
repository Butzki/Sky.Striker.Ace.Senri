// Sky Striker Ace Senri
//Uma engine DFS (motor de profundidade) e solver com score definido, para localizar o melhor score de card advantage (maior valor de vantagem numérica) possível em uma determinada ''mão'' de partida para Yu-Gi-Oh!.
//O projeto inteiro é tematizado em suas noemações, para simular uma unidade militar capaz de utilizar protocolos de combate e equipar-se com dispositivos ''força celestes''.

// Motor de regras mínimo: zonas, custos, alvos, condições de invocação, chains (pilha LIFO) e Link Summon.
// Solver por busca em espaço de estados, com poda e memoização.
// Replanejamento a partir de checkpoints. Interrupções do oponente entram só como eventos informados pelo usuário.

// Métrica: Saldo de cartas; quanto de recurso útil o jogador termina em relação ao ponto de partida.
// (Mão prioritária, campo para board específica + recursos de backup) 
// Cartas "objetivo" (que não geram recurso e são usadas no combate, como interrupções) 
// valem mais na pontuação em vez de ter lógica própria, pois não geram recurso por si.
// 
// A partir da decklist selecionada, informe sua mão inicial, o solver tentará encontrar a melhor sequência de jogadas para atingir o objetivo,
//  e a melhor vantágem numérica possível; cartas -1 consomem, cartas 0 geram consistência, e cartas 1+ geram recurso.
// 
// Guarda-se quantas cópias de cada carta restam. A posição no baralho é irrelevante.
//
// Cada ação gera um estado novo. O hash cobre todos os campos que afetam jogadas futuras e é a base da memoização.
//
// O motor decide o que é legal; o avaliador decide o que é vantajoso. O motor não tem noção de "vantagem" ou "estratégia".
//
// Cada efeito expõe CanActivate (inclui custos e restrições) e Resolve (reavalia o que importa naquele momento). 
// É isso que faz Fallen & Virtuous antes de Engage render a compra.
//
// O solver quando insere uma carta desconhecida em uma zona pública (como a mão, que vale +1 na pontuação) e para de expandir naquele ramo. 
// O usuário pode informar a carta real na sessão e o programa recalcula dali.
//
// Negações e destruições são eventos manuais do usuário.
//
// Empates na pontuação resolvem por critério fixo (menos ações, prioridade na pontuação, depois ordem aflabética no identificador).
//
//  Sky Striker Ace Senri/
//  ├─ Airspace/      (Domain)  zona de controle = o estado do jogo
//  │    GameState, CardData, GameAction, ChainLink
//  │    CombatRadius            -> FieldZones (monstros, EMZ, Spell/Trap, Field Zone)
//  ├─ Cards/         (efeitos) uma classe por carta, com o nome REAL da carta
//  │    Monsters/, Spells/, Traps/, Extra/
//  ├─ Maneuver/      (Engine)  táticas de posicionamento e resposta
//  │    Divergence!              -> ActionGenerator (ramifica as ações possíveis)
//  │    Sequence!                -> ChainEngine (monta a pilha de links)
//  │    Convergence!             -> Resolver (resolução LIFO)
//  │    Interception!            -> janela de resposta (Quick Effects e Traps)
//  │    Suppression!             -> tratamento de link negado ou destruído
//  ├─ Mecha/         (Search)  dispositivos de cálculo
//  │    Permutator              -> Solver (DFS sobre estados)
//  │    Valuation               -> Evaluator (pontuação, lê scoring.json)
//  │    TacticalNode                    -> SearchNode (estado + ação + pai)
//  │    TacticalPoint           -> StateHasher (chave canônica do estado)
//  │    ProbabilityEngine       -> carta desconhecida e eventos aleatórios
//  ├─ Mobilize/      (procedimentos em torno da busca)
//  │    Logistics!               -> carrega cards.json e decklists
//  │    Deployment!              -> valida a decklist e monta a mão inicial
//  │    Recon!                   -> varredura inicial e estado raiz
//  │    Optimize!                -> poda e política de memoização
//  ├─ Modules/       (Session + infraestrutura)
//  │    ECMSuite                -> eventos informados (negado, destruído, comprado)
//  │    Telemetry               -> contadores (nós, tempo) e log
//  │    Checkpoint, SessionManager
//  ├─ Ace/           (ponto de entrada)
//  │    Senri                   -> fachada: "dada a mão, devolva o plano"
//  │    SenriConsole            -> menus e exibição (ConsoleUI)
//  ├─ Data/          cards.json, decklists/*.json, scoring.json
//  └─ ComboCalculator.Tests/   xUnit, espelha as pastas acima

//Score = cartas na mão + objetivos úteis em campo
//      + UnknownCardsInHand
//      + peso extra por carta objetivo na mão (ex.: +0,5)
//      - cartas gastas sem retorno



Fase 1: núcleo do domínio e primeiras cartas
Objetivo. Entender na prática backtracking e memoização. Ter GameState, zonas e o contrato IEffect funcionando, com um punhado de cartas ativadas isoladamente (sem chain ainda).
Entrada. Contexto fixo, decisões, contratos, textos das cartas abaixo, regras do capítulo 3 do Extended Game Mechanics (custos, alvos, enviar ao cemitério, destruir, comprar, adicionar).
Cartas desta fase.
• Sky Striker Mobilize - Engage! (busca + compra condicional a 3 ou mais Spells no cemitério)
• The Fallen & The Virtuous (só o 1º efeito: enviar do Extra Deck e destruir; Extra Deck como lista, sem invocar)
• Mystical Space Typhoon
• Reinforcement of the Army
• Upstart Goblin (compra vira carta desconhecida)
• Sky Striker Ace - Raye (apenas como carta buscável, sem efeito)
• Ash Blossom (preenche espaço com card inutilizável)
Escopo.
• Carregar cards.json e uma decklist JSON; validar 40 main + 15 Extra Deck e limite de 3 cópias.
• GameState imutável com With(...), Multiset e hash canônico.
• Ações com escolha de alvo e de busca: Engage gera uma ação por carta buscável distinta.
• Cemitério, banimento, mão e campo; contagem de Spells no cemitério.
• Compra como carta desconhecida, conforme a decisão 9.
Fora de escopo. Chains, resposta, Link Summon, solver, console interativo.
Entregáveis. Projeto compilando, cards.json das cartas da fase, uma decklist de exemplo, uma classe por carta e testes xUnit.
Critérios de aceite.
• Engage com menos de 3 Spells no cemitério busca e não compra; com 3 ou mais, compra (carta desconhecida +1).
• Engage gera uma ação por carta Sky Striker distinta no deck, sem duplicar cópias idênticas.
• Estados equivalentes produzem o mesmo hash.
• Decklist inválida é rejeitada com mensagem clara.
• Cada carta tem ao menos um teste de ativação e um de restrição (por exemplo, Engage não ativa com monstro na Main Monster Zone).
Prompt de abertura.
Contexto fixo, decisões e contratos anexados, mais o código atual. Estamos na Fase 1. Implemente o núcleo do domínio e as 6 cartas listadas com testes xUnit. Siga os contratos; se precisar alterar uma assinatura, liste a alteração no fim. Não implemente chain nem solver.
Auditoria.
• Comparar cada Resolve com o texto da carta, frase a frase.
• Verificar que condições "se você tiver 3 ou mais Spells" são lidas na resolução.
• Verificar que nenhum GameState é mutado e que o hash inclui UsedThisTurn, Flags e cartas desconhecidas.