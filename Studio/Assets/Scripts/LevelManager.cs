using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;
using System.Collections;

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

        SetPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        
        // 禁用按钮并显示等待消息
        if (continueButton != null)
            continueButton.interactable = false;
        if (exitButton != null)
            exitButton.interactable = false;
        if (settlementScoreText != null)
            settlementScoreText.text += "\n\n等待另一位玩家选择...";
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
    private void StartGunfightPhaseServerRpc()
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
        
        // 播放对话
        if (DialogManager.Instance != null)
        {
            DialogManager.Instance.PlayRange(13, 16);
        }
    }

    // 供RoundManager调用，判断是否使用枪战模式的计算方法
    public bool IsGunfightMode()
    {
        return isGunfightMode.Value;
    }
} 