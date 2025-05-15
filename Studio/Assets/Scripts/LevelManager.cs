using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;  // 添加对 List 和 Dictionary 的支持

public class LevelManager : NetworkBehaviour
{
    public enum Level
    {
    Level1,      // 第一关
    Level2,      // 第二关
    Level3A,     // 第三关选项A（原Level3）
    Level3B,     // 第三关选项B（原Level4）
    Level4A,     // 第四关选项A
    Level4B,     // 第四关选项B
    Level5A,     // 第五关选项A
    Level5B,     // 第五关选项B
    LevelFinal   // 最终关
    }
    public Level currentLevel;

    // 游戏进度追踪
    public class LevelProgress
    {
        public int completedLevels = 0;
        public List<Level> playedLevels = new List<Level>();
        public const int TOTAL_LEVELS_TO_PLAY = 6;
    }

    public LevelProgress progress = new LevelProgress();

    [System.Serializable]
    public class LevelObjective
    {
        public enum ObjectiveType
        {
            TotalCoopCount,     // 总合作次数
            TotalCheatCount,    // 总欺骗次数
            RoundCoopCount,     // 单回合合作次数
            RoundCheatCount,    // 单回合欺骗次数
            Score,              // 达到特定分数
            Custom              // 自定义目标（预留）
        }

        public ObjectiveType type;
        public int targetValue;
        public float bonus = 15f;  // 完成目标的奖励分数
        public int playerIndex;    // 0 for player1, 1 for player2
        public bool isCompleted;   // Track completion status
    }

    [Header("Level Objectives")]
    public List<LevelObjective> currentLevelObjectives = new List<LevelObjective>();
    public NetworkVariable<bool> objectiveCompleted = new NetworkVariable<bool>();

    // 玩家目标完成状态
    public NetworkVariable<bool> player1ObjectiveCompleted = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> player2ObjectiveCompleted = new NetworkVariable<bool>(false);

    // 玩家目标数值
    public NetworkVariable<int> player1CheatTarget = new NetworkVariable<int>(0);
    public NetworkVariable<int> player2CheatTarget = new NetworkVariable<int>(0);
    public NetworkVariable<int> player1CoopTarget = new NetworkVariable<int>(0);
    public NetworkVariable<int> player2CoopTarget = new NetworkVariable<int>(0);

    // 目标完成奖励
    private const float OBJECTIVE_BONUS = 10f;

    // 预设的关卡目标
    private readonly Dictionary<Level, List<LevelObjective>> levelObjectives = new Dictionary<Level, List<LevelObjective>>
    {
        {
            Level.Level3A,
            new List<LevelObjective>
            {
                new LevelObjective { type = LevelObjective.ObjectiveType.TotalCheatCount, targetValue = 15, playerIndex = 0 },
                new LevelObjective { type = LevelObjective.ObjectiveType.TotalCoopCount, targetValue = 12, playerIndex = 1 }
            }
        },
        {
            Level.Level3B,
            new List<LevelObjective>
            {
                new LevelObjective { type = LevelObjective.ObjectiveType.TotalCoopCount, targetValue = 12, playerIndex = 0 },
                new LevelObjective { type = LevelObjective.ObjectiveType.TotalCheatCount, targetValue = 15, playerIndex = 1 }
            }
        }
        // Add more level objectives here
    };

    [Header("References")]
    public RoundManager roundManager;
    public GameObject Gun1;
    public GameObject Gun2;
    
    [Header("UI Elements")]
    public GameObject settlementPanel;
    public TMP_Text settlementScoreText;
    public Button continueButton;
    public Button exitButton;

    // 网络同步的游戏状态
    public NetworkVariable<bool> isGunfightMode = new NetworkVariable<bool>();
    public NetworkVariable<float> firstPhaseScore1 = new NetworkVariable<float>();
    public NetworkVariable<float> firstPhaseScore2 = new NetworkVariable<float>();
    
    // 玩家选择状态
    public NetworkVariable<bool> player1Ready = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> player2Ready = new NetworkVariable<bool>(false);

    private void Start()
    {
        if (settlementPanel != null)
            settlementPanel.SetActive(false);

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
            
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);

        // 监听玩家准备状态变化
        player1Ready.OnValueChanged += OnPlayerReadyChanged;
        player2Ready.OnValueChanged += OnPlayerReadyChanged;

        // 设置关卡目标
        ApplyLevelObjectives();
    }

    private void OnDestroy()
    {
        player1Ready.OnValueChanged -= OnPlayerReadyChanged;
        player2Ready.OnValueChanged -= OnPlayerReadyChanged;
    }

    private void OnContinueClicked()
    {
        if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
        {
            Debug.LogError("Not connected to network!");
            return;
        }

        // 禁用按钮
        if (continueButton != null)
            continueButton.interactable = false;
        if (exitButton != null)
            exitButton.interactable = false;

        GameManager.Instance.LoadScene("MapScene");
    }

    private void OnExitClicked()
    {
        if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
        {
            Debug.LogError("Not connected to network!");
            return;
        }

        ExitGameServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerReadyServerRpc(ulong clientId)
    {
        if (clientId == 0)
            player1Ready.Value = true;
        else
            player2Ready.Value = true;
    }

    private void OnPlayerReadyChanged(bool previousValue, bool newValue)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        
        // 如果双方都准备好了
        if (player1Ready.Value && player2Ready.Value)
        {
            StartGunfightPhaseServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGunfightPhaseServerRpc()
    {
        // 重置准备状态
        player1Ready.Value = false;
        player2Ready.Value = false;
        
        StartGunfightPhaseClientRpc();
    }

    [ClientRpc]
    private void StartGunfightPhaseClientRpc()
    {
        // 关闭结算面板
        if (settlementPanel != null)
            settlementPanel.SetActive(false);

        // 隐藏游戏结束文本
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.HideGameOver();
        }

        // 先重置游戏状态和分数
        if (NetworkManager.Singleton.IsServer)
        {
            isGunfightMode.Value = true;
            // 重置回合和分数
            roundManager.ResetRound();
            
            // 重置玩家分数
            roundManager.player1.point.Value = 0;
            roundManager.player2.point.Value = 0;
            
            // 立即发牌
            GameManager.Instance.currentGameState = GameManager.GameState.Ready;
        }

        // 等待一小段时间让发牌动画开始，然后播放对话
        StartCoroutine(PlayDialogAfterDealing());
    }

    [ServerRpc(RequireOwnership = false)]
    private void ExitGameServerRpc()
    {
        ExitGameClientRpc();
    }

    [ClientRpc]
    private void ExitGameClientRpc()
    {
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            string finalScore = $"Game Complete!\n\n" +
                              $"Phase 1 Results:\n" +
                              $"Player 1: {roundManager.player1.point.Value}\n" +
                              $"Player 2: {roundManager.player2.point.Value}";
            uiManager.NetworkShowGameOver(finalScore);
        }
    }

    // 第一阶段（无枪）结束时调用
    public void ShowFirstPhaseSettlement()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            // 在服务器端保存分数
            firstPhaseScore1.Value = roundManager.player1.point.Value;
            firstPhaseScore2.Value = roundManager.player2.point.Value;
            
            // 重置准备状态
            player1Ready.Value = false;
            player2Ready.Value = false;

            // 通知所有客户端更新UI
            ShowSettlementPanelClientRpc();
            Debug.Log("ShowFirstPhaseSettlement");
        }
    }

    [ClientRpc]
    private void ShowSettlementPanelClientRpc()
    {
        Debug.Log($"Showing settlement panel on client. Scores: {roundManager.player1.point.Value} vs {firstPhaseScore2.Value}");
        
        if (settlementPanel != null)
        {
            settlementPanel.SetActive(true);

            // 启用按钮
            if (continueButton != null)
                continueButton.interactable = true;
            if (exitButton != null)
                exitButton.interactable = true;

            // 更新结算面板显示
            if (settlementScoreText != null)
            {
                string winner = roundManager.player1.point.Value > roundManager.player2.point.Value ? "Player 1" :
                              roundManager.player2.point.Value > roundManager.player1.point.Value ? "Player 2" : "No one";
                
                settlementScoreText.text = $"Phase 1 Complete!\n\n" +
                                         $"Player 1: {roundManager.player1.point.Value}\n" +
                                         $"Player 2: {roundManager.player2.point.Value}\n\n" +
                                         $"{winner} wins Phase 1!\n\n" +
                                         "Continue to Gunfight Phase?";
            }
            else
            {
                Debug.LogError("Settlement score text is missing!");
            }
        }
        else
        {
            Debug.LogError("Settlement panel is missing!");
        }
    }

    private IEnumerator PlayDialogAfterDealing()
    {
        // 等待一小段时间让发牌动画开始
        yield return new WaitForSeconds(0.5f);
        
        // 根据不同关卡播放不同的对话
        if (DialogManager.Instance != null)
        {
            PlayLevelDialog();
        }
    }

    // 供RoundManager调用，判断是否使用枪战模式的计算方法
    public bool IsGunfightMode()
    {
        return isGunfightMode.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    private void LoadMapSceneServerRpc(string sceneName)
    {
        // 在服务器端加载地图场景
        GameManager.Instance.LoadScene(sceneName);
    }

    private void PlayLevelDialog()
    {
        if (DialogManager.Instance != null)
        {
            bool isPlayer1 = NetworkManager.Singleton.LocalClientId == 0;
            DialogManager.Instance.PlayLevelDialog(currentLevel, isPlayer1);
        }
    }

    public bool IsLevelAvailable(Level level)
    {
        // 如果已经玩过这个关卡，不能重复玩
        if (progress.playedLevels.Contains(level))
            return false;

        // 根据已完成的关卡数和关卡类型来判断是否可用
        switch (level)
        {
            case Level.Level1:
                return progress.completedLevels == 0;
            
            case Level.Level2:
                return progress.completedLevels == 1;
            
            case Level.Level3A:
            case Level.Level3B:
                return progress.completedLevels == 2;
            
            case Level.Level4A:
            case Level.Level4B:
                return progress.completedLevels == 3;
            
            case Level.Level5A:
            case Level.Level5B:
                return progress.completedLevels == 4;
            
            case Level.LevelFinal:
                return progress.completedLevels == 4;
            
            default:
                return false;
        }
    }

    public void SelectLevel(Level level)
    {
        if (!IsLevelAvailable(level))
            return;

        currentLevel = level;
        progress.playedLevels.Add(level);
        GameManager.Instance.LoadScene("Game");
    }

    // 当关卡完成时调用
    public void OnLevelComplete()
    {
        progress.completedLevels++;
        
        // 如果已经完成了指定数量的关卡，游戏结束
        if (progress.completedLevels >= LevelProgress.TOTAL_LEVELS_TO_PLAY)
        {
            // 显示游戏完成界面
            ShowGameComplete();
        }
        else
        {
            // 返回关卡选择地图
            GameManager.Instance.LoadScene("MapScene");
        }
    }

    private void ShowGameComplete()
    {
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            string message = "Congratulations!\nYou have completed all levels!\n\n" +
                           $"Total Score:\nPlayer 1: {roundManager.player1.point.Value}\n" +
                           $"Player 2: {roundManager.player2.point.Value}";
            uiManager.NetworkShowGameOver(message);
        }
    }

    private void ApplyLevelObjectives()
    {
        if (!IsServer) return;

        // 重置所有目标状态
        player1ObjectiveCompleted.Value = false;
        player2ObjectiveCompleted.Value = false;
        player1CheatTarget.Value = 0;
        player2CheatTarget.Value = 0;
        player1CoopTarget.Value = 0;
        player2CoopTarget.Value = 0;
        objectiveCompleted.Value = false;

        switch (currentLevel)
        {
            case Level.Level1:
                break;
            case Level.Level2:
                break;
            case Level.Level3A:  
                // 第三关A选项：在全局达到欺骗/合作的数量
                player1CheatTarget.Value = 15;
                player2CoopTarget.Value = 12;
                break;
            case Level.Level3B:  
                // 第三关B选项：与A选项相反的目标
                player1CoopTarget.Value = 12;
                player2CheatTarget.Value = 15;
                break;
            case Level.Level4A:
                break;
            case Level.Level4B:
                break;
            case Level.Level5A:
                break;
            case Level.Level5B:
                break;
            case Level.LevelFinal:
                // 最终关的目标
                break;
        }
    }

    // 检查关卡目标是否完成
    public void CheckLevelObjectives()
    {
        if (!IsServer) return;

        bool completed = false;
        // 计算当前总分
        float totalScore = roundManager.player1.point.Value + roundManager.player2.point.Value;

        switch (currentLevel)
        {
            case Level.Level1:
                completed = false;
                break;
            case Level.Level2:
                completed = false;
                break;
            case Level.Level3A:
                // 检查是否达到欺骗/合作目标
                if (!player1ObjectiveCompleted.Value && roundManager.player1TotalCheatCount.Value >= player1CheatTarget.Value)
                {
                    player1ObjectiveCompleted.Value = true;
                    // 给Player 1加分
                    roundManager.player1.point.Value += OBJECTIVE_BONUS;
                }
                
                if (!player2ObjectiveCompleted.Value && roundManager.player2TotalCoopCount.Value >= player2CoopTarget.Value)
                {
                    player2ObjectiveCompleted.Value = true;
                    // 给Player 2加分
                    roundManager.player2.point.Value += OBJECTIVE_BONUS;
                }
                
                completed = player1ObjectiveCompleted.Value && player2ObjectiveCompleted.Value;
                break;
            case Level.Level3B:
                // 检查是否达到合作/欺骗目标（与Level3A相反）
                if (!player1ObjectiveCompleted.Value && roundManager.player1TotalCoopCount.Value >= player1CoopTarget.Value)
                {
                    player1ObjectiveCompleted.Value = true;
                    // 给Player 1加分
                    roundManager.player1.point.Value += OBJECTIVE_BONUS;
                }
                
                if (!player2ObjectiveCompleted.Value && roundManager.player2TotalCheatCount.Value >= player2CheatTarget.Value)
                {
                    player2ObjectiveCompleted.Value = true;
                    // 给Player 2加分
                    roundManager.player2.point.Value += OBJECTIVE_BONUS;
                }
                
                completed = player1ObjectiveCompleted.Value && player2ObjectiveCompleted.Value;
                break;
            case Level.Level4A:
                // 玩家1：总分<=15分获得奖励
                if (!player1ObjectiveCompleted.Value && totalScore <= 15)
                {
                    player1ObjectiveCompleted.Value = true;
                    roundManager.player1.point.Value += OBJECTIVE_BONUS;
                }
                // 玩家2：总分>=20分获得奖励
                if (!player2ObjectiveCompleted.Value && totalScore >= 20)
                {
                    player2ObjectiveCompleted.Value = true;
                    roundManager.player2.point.Value += OBJECTIVE_BONUS;
                }
                completed = player1ObjectiveCompleted.Value || player2ObjectiveCompleted.Value;
                break;
            case Level.Level4B:
                // 玩家1：总分>=20分获得奖励
                if (!player1ObjectiveCompleted.Value && totalScore >= 20)
                {
                    player1ObjectiveCompleted.Value = true;
                    roundManager.player1.point.Value += OBJECTIVE_BONUS;
                }
                // 玩家2：总分<=15分获得奖励
                if (!player2ObjectiveCompleted.Value && totalScore <= 15)
                {
                    player2ObjectiveCompleted.Value = true;
                    roundManager.player2.point.Value += OBJECTIVE_BONUS;
                }
                completed = player1ObjectiveCompleted.Value || player2ObjectiveCompleted.Value;
                break;

            case Level.Level5A:
                // 玩家1：被玩家2的枪打死获得奖励
                if (!player1ObjectiveCompleted.Value && Gun2.GetComponent<GunController>().gameEnded.Value)
                {
                    player1ObjectiveCompleted.Value = true;
                    roundManager.player1.point.Value += OBJECTIVE_BONUS;
                }
                // 玩家2：分数低于对手获得奖励
                if (!player2ObjectiveCompleted.Value && roundManager.player2.point.Value < roundManager.player1.point.Value)
                {
                    player2ObjectiveCompleted.Value = true;
                    roundManager.player2.point.Value += OBJECTIVE_BONUS;
                }
                completed = player1ObjectiveCompleted.Value || player2ObjectiveCompleted.Value;
                break;
            case Level.Level5B:
                // 玩家1：分数低于对手获得奖励
                if (!player1ObjectiveCompleted.Value && roundManager.player1.point.Value < roundManager.player2.point.Value)
                {
                    player1ObjectiveCompleted.Value = true;
                    roundManager.player1.point.Value += OBJECTIVE_BONUS;
                }
                // 玩家2：被玩家1的枪打死获得奖励
                if (!player2ObjectiveCompleted.Value && Gun1.GetComponent<GunController>().gameEnded.Value)
                {
                    player2ObjectiveCompleted.Value = true;
                    roundManager.player2.point.Value += OBJECTIVE_BONUS;
                }
                completed = player1ObjectiveCompleted.Value || player2ObjectiveCompleted.Value;
                break;
    
            case Level.LevelFinal:
                // 检查是否有玩家被击杀
                if (!objectiveCompleted.Value)
                {
                    float totalPoints = roundManager.player1.point.Value + roundManager.player2.point.Value;
                    
                    bool player2Dead = Gun1.GetComponent<GunController>().gameEnded.Value;
                    bool player1Dead = Gun2.GetComponent<GunController>().gameEnded.Value;

                    // 双方同时击杀
                    if (player1Dead && player2Dead)
                    {
                        // 双方都归零
                        roundManager.player1.point.Value = 0;
                        roundManager.player2.point.Value = 0;
                        objectiveCompleted.Value = true;
                    }
                    // 只有玩家2被击杀
                    else if (player2Dead)
                    {
                        // 玩家1获得双方总分
                        roundManager.player1.point.Value = totalPoints;
                        roundManager.player2.point.Value = 0;
                        objectiveCompleted.Value = true;
                    }
                    // 只有玩家1被击杀
                    else if (player1Dead)
                    {
                        // 玩家2获得双方总分
                        roundManager.player2.point.Value = totalPoints;
                        roundManager.player1.point.Value = 0;
                        objectiveCompleted.Value = true;
                    }
                }
                completed = objectiveCompleted.Value;
                break;
        }

        objectiveCompleted.Value = completed;
    }
} 