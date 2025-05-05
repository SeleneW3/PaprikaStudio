using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;

public class LevelManager : NetworkBehaviour
{
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

    private void Start()
    {
        if (settlementPanel != null)
            settlementPanel.SetActive(false);

        if (continueButton != null)
            continueButton.onClick.AddListener(StartGunfightPhase);
            
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);
    }

    // 第一阶段（无枪）结束时调用
    public void ShowFirstPhaseSettlement()
    {
        if (settlementPanel != null)
        {
            settlementPanel.SetActive(true);
            
            // 保存第一阶段分数
            if (NetworkManager.Singleton.IsServer)
            {
                firstPhaseScore1.Value = roundManager.player1.point.Value;
                firstPhaseScore2.Value = roundManager.player2.point.Value;
            }

            // 更新结算面板显示
            if (settlementScoreText != null)
            {
                string winner = firstPhaseScore1.Value > firstPhaseScore2.Value ? "Player 1" : 
                              firstPhaseScore2.Value > firstPhaseScore1.Value ? "Player 2" : "No one";
                
                settlementScoreText.text = $"Phase 1 Complete!\n\n" +
                                         $"Player 1: {firstPhaseScore1.Value}\n" +
                                         $"Player 2: {firstPhaseScore2.Value}\n\n" +
                                         $"{winner} wins Phase 1!\n\n" +
                                         "Continue to Gunfight Phase?";
            }
        }
    }

    // 开始枪战阶段
    public void StartGunfightPhase()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            isGunfightMode.Value = true;
            // 重置回合和分数
            roundManager.ResetRound();
            
            // 设置游戏状态为Ready，这样会触发发牌
            GameManager.Instance.currentGameState = GameManager.GameState.Ready;
        }

        // 关闭结算面板
        if (settlementPanel != null)
            settlementPanel.SetActive(false);
    }

    private void ExitGame()
    {
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            string finalScore = $"Game Complete!\n\n" +
                              $"Phase 1 Results:\n" +
                              $"Player 1: {firstPhaseScore1.Value}\n" +
                              $"Player 2: {firstPhaseScore2.Value}";
            uiManager.ShowGameOver(finalScore);
        }
    }

    // 供RoundManager调用，判断是否使用枪战模式的计算方法
    public bool IsGunfightMode()
    {
        return isGunfightMode.Value;
    }
} 