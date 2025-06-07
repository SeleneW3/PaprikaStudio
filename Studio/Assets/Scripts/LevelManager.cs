using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

public class LevelManager : NetworkBehaviour
{
    public static LevelManager Instance { get; private set; }

    // 游戏模式枚举
    public enum Mode
    {
        Tutor,
        OnlyCard,
        OnlyGun,
        CardAndGun
    }

    // 关卡枚举
    public enum Level
    {
        Tutorial,  // 教程
        Level1,    // 只有卡牌
        Level2,    // 卡牌和枪
        Level3A,   // 只有卡牌 + 身份选择
        Level3B,   // 卡牌和枪 + 身份选择
        Level4A,   // 只有卡牌
        Level4B,   // 只有枪
        Level4C,   // 卡牌和枪
        Level5A,   // 卡牌和枪 - 被打死获得10分
        Level5B,   // 卡牌和枪 - 打死对方获得双方本轮总分
        Level6A,   // 卡牌和枪 - 打死对方获得双方全部总分
        Level6B    // 只有卡牌 - 总分高者胜利，低者归零
    }

    // 玩家身份枚举
    public enum PlayerIdentity
    {
        None,      // 未选择
        Cheater,   // 欺骗派
        Cooperator // 合作派
    }

    // 网络变量 - 当前解锁的关卡
    public NetworkVariable<Level> unlockedLevel = new NetworkVariable<Level>(
        Level.Level1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // 网络变量 - 已完成的关卡路径
    public NetworkVariable<int> completedPath = new NetworkVariable<int>(
        0, // 0: 未选择路径, 1: 路径A(3A), 2: 路径B(3B)
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // 网络变量 - 玩家总分
    public NetworkVariable<float> player1TotalPoint = new NetworkVariable<float>(
         0,
         NetworkVariableReadPermission.Everyone,     
         NetworkVariableWritePermission.Server        
     );

    public NetworkVariable<float> player2TotalPoint = new NetworkVariable<float>(
         0,
         NetworkVariableReadPermission.Everyone,
         NetworkVariableWritePermission.Server
     );

    // 网络变量 - 玩家欺骗次数
    public NetworkVariable<int> player1CheatTimes = new NetworkVariable<int>(
         0,
         NetworkVariableReadPermission.Everyone,
         NetworkVariableWritePermission.Server
     );

    public NetworkVariable<int> player2CheatTimes = new NetworkVariable<int>(
         0,
         NetworkVariableReadPermission.Everyone,
         NetworkVariableWritePermission.Server
     );

    // 网络变量 - 玩家合作次数
    public NetworkVariable<int> player1CoopTimes = new NetworkVariable<int>(
         0,
         NetworkVariableReadPermission.Everyone,
         NetworkVariableWritePermission.Server
     );

    public NetworkVariable<int> player2CoopTimes = new NetworkVariable<int>(
         0,
         NetworkVariableReadPermission.Everyone,
         NetworkVariableWritePermission.Server
     );

    // 网络变量 - 玩家身份
    public NetworkVariable<PlayerIdentity> player1Identity = new NetworkVariable<PlayerIdentity>(
        PlayerIdentity.None,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<PlayerIdentity> player2Identity = new NetworkVariable<PlayerIdentity>(
        PlayerIdentity.None,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // 添加网络变量来跟踪是否已经获得过身份奖励
    public NetworkVariable<bool> player1ReceivedIdentityBonus = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> player2ReceivedIdentityBonus = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("当前模式 (同步用)")]
    public NetworkVariable<Mode> currentMode = new NetworkVariable<Mode>(
        Mode.Tutor,
        NetworkVariableReadPermission.Everyone,     
        NetworkVariableWritePermission.Server        
    );

    [Header("当前关卡 (同步用)")]
    public NetworkVariable<Level> currentLevel = new NetworkVariable<Level>(
        Level.Tutorial,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // 添加网络变量 - 是否是最终关卡
    public NetworkVariable<bool> isFinalLevel = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // 事件 - 当关卡改变时
    public event Action<Level, Level> OnLevelChanged;

    // 新增：待播放对话的关卡
    private Level pendingDialogLevel = Level.Tutorial;
    private bool shouldPlayDialogAfterSceneLoad = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 添加场景加载完成的事件监听
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        currentMode.OnValueChanged += OnModeChanged;
        currentLevel.OnValueChanged += OnLevelValueChanged;
    }

    public override void OnNetworkDespawn()
    {
        currentMode.OnValueChanged -= OnModeChanged;
        currentLevel.OnValueChanged -= OnLevelValueChanged;
        
        // 移除场景加载事件监听
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        
        base.OnNetworkDespawn();
    }

    private void OnModeChanged(Mode previous, Mode current)
    {
        Debug.Log($"[LevelManager] 模式从 {previous} 切换到了 {current}");
        // TODO: 在这里做本地化的 UI 刷新 / 场景切换逻辑
        // 例如：根据 current 来开启/关闭不同的界面、准备场景、等等
    }

    private void OnLevelValueChanged(Level previous, Level current)
    {
        Debug.Log($"[LevelManager] 关卡从 {previous} 切换到了 {current}");
        
        // 根据关卡设置相应的模式
        SetModeBasedOnLevel(current);
        
        // 记录待播放对话的关卡，但不立即播放
        pendingDialogLevel = current;
        
        // 更新背景音乐
        UpdateBackgroundMusic(current);
        
        // 触发关卡改变事件
        OnLevelChanged?.Invoke(previous, current);
    }

    // 根据关卡设置游戏模式
    private void SetModeBasedOnLevel(Level level)
    {
        if (!IsServer) return;
        
        switch (level)
        {
            case Level.Tutorial:
                currentMode.Value = Mode.Tutor;
                break;
            case Level.Level1:
                currentMode.Value = Mode.OnlyCard;
                break;
            case Level.Level2:
                currentMode.Value = Mode.CardAndGun;
                break;
            case Level.Level3A:
                currentMode.Value = Mode.OnlyCard;
                break;
            case Level.Level3B:
                currentMode.Value = Mode.CardAndGun;
                break;
            case Level.Level4A:
                currentMode.Value = Mode.OnlyCard;
                break;
            case Level.Level4B:
                currentMode.Value = Mode.OnlyGun;
                break;
            case Level.Level4C:
                currentMode.Value = Mode.CardAndGun;
                break;
            case Level.Level5A:
                currentMode.Value = Mode.CardAndGun;
                break;
            case Level.Level5B:
                currentMode.Value = Mode.CardAndGun;
                break;
            case Level.Level6A:
                currentMode.Value = Mode.CardAndGun;
                break;
            case Level.Level6B:
                currentMode.Value = Mode.OnlyCard;
                break;
        }
    }

    // 根据关卡更新背景音乐
    private void UpdateBackgroundMusic(Level level)
    {
        // 确保SoundManager实例存在
        if (SoundManager.Instance == null) return;
        
        // 根据关卡决定播放的音乐
        if (level == Level.Tutorial)
        {
            SoundManager.Instance.PlayMusic("GameTutor");
        }
        else if (level >= Level.Level5A) // Level5和Level6
        {
            SoundManager.Instance.PlayMusic("GameGun");
        }
        else // Level1到Level4
        {
            SoundManager.Instance.PlayMusic("Game");
        }
    }

    // 播放关卡相应的对话
    private void PlayDialogForLevel(Level level)
    {
        if (DialogManager.Instance == null)
        {
            Debug.LogError("[LevelManager] DialogManager.Instance is null!");
            return;
        }
        
        switch (level)
        {
            case Level.Level1:
                Debug.Log("[LevelManager] 播放Level1对话 (13-15)");
                DialogManager.Instance.PlayRange(13, 15);
                break;
            case Level.Level2:
                Debug.Log("[LevelManager] 播放Level2对话 (16-19)");
                DialogManager.Instance.PlayRange(16, 19);
                break;
            case Level.Level3A:
                {
                    // 获取玩家当前的合作和欺骗次数
                    int p1CoopCount = player1CoopTimes.Value;
                    int p1CheatCount = player1CheatTimes.Value;
                    int p2CoopCount = player2CoopTimes.Value;
                    int p2CheatCount = player2CheatTimes.Value;
                    
                    Debug.Log($"[LevelManager] 播放Level3A自定义对话 - 玩家1(合作:{p1CoopCount},欺骗:{p1CheatCount}) 玩家2(合作:{p2CoopCount},欺骗:{p2CheatCount})");
                    
                    // 创建分段对话内容
                    string[] p1Segments = new string[] {
                        "<shake a=0.2 f=0.8>玩家1</shake>", 
                        $"<shake a=0.2 f=0.8>你现在合作{p1CoopCount}次</shake>", 
                        $"<shake a=0.2 f=0.8>欺骗{p1CheatCount}次</shake>"
                    };
                    
                    string[] p2Segments = new string[] {
                        "<shake a=0.2 f=0.8>玩家2</shake>", 
                        $"<shake a=0.2 f=0.8>你现在合作{p2CoopCount}次</shake>", 
                        $"<shake a=0.2 f=0.8>欺骗{p2CheatCount}次</shake>"
                    };
                    
                    // 根据客户端ID选择显示的分段对话
                    if (NetworkManager.Singleton.LocalClientId == 0)
                    {
                        DialogManager.Instance.PlaySegmentedDialog(p1Segments, OnDialogComplete);
                    }
                    else
                    {
                        DialogManager.Instance.PlaySegmentedDialog(p2Segments, OnDialogComplete);
                    }
                    
                    // 设置完成路径为路径A（仅服务器执行）
                    if (IsServer)
                        completedPath.Value = 1;
                }
                break;
            case Level.Level3B:
                {
                    // 获取玩家当前的合作和欺骗次数
                    int p1CoopCount = player1CoopTimes.Value;
                    int p1CheatCount = player1CheatTimes.Value;
                    int p2CoopCount = player2CoopTimes.Value;
                    int p2CheatCount = player2CheatTimes.Value;
                    
                    Debug.Log($"[LevelManager] 播放Level3B自定义对话 - 玩家1(合作:{p1CoopCount},欺骗:{p1CheatCount}) 玩家2(合作:{p2CoopCount},欺骗:{p2CheatCount})");
                    
                    // 创建分段对话内容
                    string[] p1Segments = new string[] {
                        "<shake a=0.2 f=0.8>玩家1</shake>", 
                        $"<shake a=0.2 f=0.8>你现在合作{p1CoopCount}次</shake>", 
                        $"<shake a=0.2 f=0.8>欺骗{p1CheatCount}次</shake>"
                    };
                    
                    string[] p2Segments = new string[] {
                        "<shake a=0.2 f=0.8>玩家2</shake>", 
                        $"<shake a=0.2 f=0.8>你现在合作{p2CoopCount}次</shake>", 
                        $"<shake a=0.2 f=0.8>欺骗{p2CheatCount}次</shake>"
                    };
                    
                    // 根据客户端ID选择显示的分段对话
                    if (NetworkManager.Singleton.LocalClientId == 0)
                    {
                        DialogManager.Instance.PlaySegmentedDialog(p1Segments, OnDialogComplete);
                    }
                    else
                    {
                        DialogManager.Instance.PlaySegmentedDialog(p2Segments, OnDialogComplete);
                    }
                    
                    // 设置完成路径为路径B（仅服务器执行）
                    if (IsServer)
                        completedPath.Value = 2;
                }
                break;
            case Level.Level4A:
            case Level.Level4B:
            case Level.Level4C:
                Debug.Log("[LevelManager] 播放Level4对话 (20-21)");
                DialogManager.Instance.PlayRange(20, 21);
                break;
            case Level.Level5A:
                Debug.Log("[LevelManager] 播放Level5A对话 (22-24)");
                DialogManager.Instance.PlayRange(22, 24);
                break;
            case Level.Level5B:
                Debug.Log("[LevelManager] 播放Level5B对话 (25-26)");
                DialogManager.Instance.PlayRange(25, 26);
                break;
            case Level.Level6A:
                Debug.Log("[LevelManager] 播放Level6A对话 (27-31)");
                DialogManager.Instance.PlayRange(27, 31);
                break;
            case Level.Level6B:
                Debug.Log("[LevelManager] 播放Level6B对话 (32-34)");
                DialogManager.Instance.PlayRange(32, 34);
                break;
        }
    }

    /// <summary>
    /// 这个 ServerRpc 让客户端请求"切换模式"由服务器端来实际写入 currentMode.Value。
    /// RequireOwnership=false 表示"不是这个脚本的 NetworkObject 拥有者也能调用"。
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ChangeModeServerRpc(Mode newMode)
    {
        // 只有当实际运行在服务器上时，这里才会被执行一次
        Debug.Log($"[LevelManager] 收到客户端请求切换模式到 {newMode}，服务器没有更新 currentMode");
        currentMode.Value = newMode;
        Debug.Log($"[LevelManager] 收到客户端请求切换模式到 {newMode}，服务器已更新 currentMode");

        GameManager.Instance.currentGameState = GameManager.GameState.Ready;
        GameManager.Instance.LoadScene("Game");
        // （currentMode 写了之后 Netcode 会自动分发到所有客户端）
    }

    /// <summary>
    /// 切换关卡的ServerRpc
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ChangeLevelServerRpc(Level newLevel)
    {
        Debug.Log($"[LevelManager] 收到客户端请求切换关卡到 {newLevel}");
        
        // 检查关卡是否已解锁
        if (!IsLevelSelectable(newLevel))
        {
            Debug.LogWarning($"[LevelManager] 关卡 {newLevel} 尚未解锁，无法选择");
            return;
        }
        
        Debug.Log($"[LevelManager] 关卡 {newLevel} 已解锁，准备切换");
        currentLevel.Value = newLevel;
        Debug.Log($"[LevelManager] 已设置 currentLevel.Value = {currentLevel.Value}");
        
        // 设置标志，表示场景加载后需要播放对话
        shouldPlayDialogAfterSceneLoad = true;
        pendingDialogLevel = newLevel;
        
        // 检查DialogManager是否存在
        Debug.Log($"[LevelManager] 检查DialogManager实例: {(DialogManager.Instance != null ? "存在" : "不存在")}");
        
        // 关卡切换时自动设置对应的模式并加载场景
        GameManager.Instance.currentGameState = GameManager.GameState.Ready;
        Debug.Log($"[LevelManager] 准备加载Game场景");
        GameManager.Instance.LoadScene("Game");
    }

    /// <summary>
    /// 供服务器本地直接修改 currentMode 的方法，如果你在 Host（既是Server又是Client）上点击，也可以直接调用这个。
    /// </summary>
    public void SetModeOnServer(Mode newMode)
    {
        if (!IsServer) return;
        currentMode.Value = newMode;
    }

    /// <summary>
    /// 供服务器本地直接修改 currentLevel 的方法
    /// </summary>
    public void SetLevelOnServer(Level newLevel)
    {
        if (!IsServer) return;
        
        // 检查关卡是否已解锁
        if (!IsLevelSelectable(newLevel))
        {
            Debug.LogWarning($"[LevelManager] 关卡 {newLevel} 尚未解锁，无法选择");
            return;
        }
        
        currentLevel.Value = newLevel;
    }

    /// <summary>
    /// 设置玩家身份
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerIdentityServerRpc(ulong clientId, PlayerIdentity identity)
    {
        if (!IsServer) return;
        
        if (clientId == 0ul)
            player1Identity.Value = identity;
        else if (clientId == 1ul)
            player2Identity.Value = identity;
    }

    /// <summary>
    /// 增加玩家合作次数
    /// </summary>
    public void AddPlayerCoopTime(ulong clientId)
    {
        if (!IsServer) return;
        
        // 在教程模式下不计入统计
        if (currentMode.Value == Mode.Tutor)
            return;
        
        if (clientId == 0ul)
            player1CoopTimes.Value++;
        else if (clientId == 1ul)
            player2CoopTimes.Value++;
    }

    /// <summary>
    /// 增加玩家欺骗次数
    /// </summary>
    public void AddPlayerCheatTime(ulong clientId)
    {
        if (!IsServer) return;
        
        // 在教程模式下不计入统计
        if (currentMode.Value == Mode.Tutor)
            return;
        
        if (clientId == 0ul)
            player1CheatTimes.Value++;
        else if (clientId == 1ul)
            player2CheatTimes.Value++;
    }

    /// <summary>
    /// 检查并应用身份奖励
    /// </summary>
    public void CheckAndApplyIdentityBonus()
    {
        if (!IsServer) return;
        
        bool player1Rewarded = false;
        bool player2Rewarded = false;
        
        // 欺骗派：欺骗总数达到或超过14次则获得额外加分+15（如果尚未获得过奖励）
        if (player1Identity.Value == PlayerIdentity.Cheater && player1CheatTimes.Value >= 13 && !player1ReceivedIdentityBonus.Value)
        {
            player1TotalPoint.Value += 15;
            player1Rewarded = true;
            player1ReceivedIdentityBonus.Value = true;
            Debug.Log($"[LevelManager] 玩家1(欺骗派)达成任务，奖励15分！当前欺骗次数：{player1CheatTimes.Value}");
        }
        
        if (player2Identity.Value == PlayerIdentity.Cheater && player2CheatTimes.Value >= 13 && !player2ReceivedIdentityBonus.Value)
        {
            player2TotalPoint.Value += 15;
            player2Rewarded = true;
            player2ReceivedIdentityBonus.Value = true;
            Debug.Log($"[LevelManager] 玩家2(欺骗派)达成任务，奖励15分！当前欺骗次数：{player2CheatTimes.Value}");
        }
        
        // 合作派：合作总数达到或超过9次则获得额外加分+15（如果尚未获得过奖励）
        if (player1Identity.Value == PlayerIdentity.Cooperator && player1CoopTimes.Value >= 8 && !player1ReceivedIdentityBonus.Value)
        {
            player1TotalPoint.Value += 15;
            player1Rewarded = true;
            player1ReceivedIdentityBonus.Value = true;
            Debug.Log($"[LevelManager] 玩家1(合作派)达成任务，奖励15分！当前合作次数：{player1CoopTimes.Value}");
        }
        
        if (player2Identity.Value == PlayerIdentity.Cooperator && player2CoopTimes.Value >= 8 && !player2ReceivedIdentityBonus.Value)
        {
            player2TotalPoint.Value += 15;
            player2Rewarded = true;
            player2ReceivedIdentityBonus.Value = true;
            Debug.Log($"[LevelManager] 玩家2(合作派)达成任务，奖励15分！当前合作次数：{player2CoopTimes.Value}");
        }
        
        // 显示奖励提示
        if (player1Rewarded || player2Rewarded)
        {
            ShowBonusTextClientRpc(player1Rewarded, player2Rewarded);
            
            // 更新UI显示
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdatePlayerTotalScoreTextClientRpc();
            }
        }
    }
    
    [ClientRpc]
    private void ShowBonusTextClientRpc(bool player1Rewarded, bool player2Rewarded)
    {
        Debug.Log($"[LevelManager] 显示奖励提示：玩家1={player1Rewarded}, 玩家2={player2Rewarded}");
        
        if (UIManager.Instance != null)
        {
            // 构建奖励文本
            string player1BonusText = player1Rewarded ? "<color=yellow><shake a=0.3 f=0.8>+15!</shake></color>" : "";
            string player2BonusText = player2Rewarded ? "<color=yellow><shake a=0.3 f=0.8>+15!</shake></color>" : "";
            
            // 调用UIManager显示奖励文本
            UIManager.Instance.ShowBonusText(player1BonusText, player2BonusText);
        }
    }

    /// <summary>
    /// 应用被枪打死的规则（本轮分数归零）
    /// </summary>
    public void ApplyShotPenalty(ulong shotPlayerId, float roundPoints)
    {
        if (!IsServer) return;
        
        Debug.Log($"[LevelManager] 应用枪击规则，被击中玩家ID: {shotPlayerId}, 本轮分数: {roundPoints}");
        
        string player1BonusText = "";
        string player2BonusText = "";
        
        // 在Level5A中被打死获得10分作为补偿
        if (currentLevel.Value == Level.Level5A)
        {
            if (shotPlayerId == 0ul)
            {
                player1TotalPoint.Value += 10;
                player1BonusText = "<color=yellow><shake a=0.3 f=0.8>+10!</shake></color>";
                Debug.Log($"[LevelManager] Level5A规则: 玩家1被击中，获得10分补偿，当前总分: {player1TotalPoint.Value}");
            }
            else if (shotPlayerId == 1ul)
            {
                player2TotalPoint.Value += 10;
                player2BonusText = "<color=yellow><shake a=0.3 f=0.8>+10!</shake></color>";
                Debug.Log($"[LevelManager] Level5A规则: 玩家2被击中，获得10分补偿，当前总分: {player2TotalPoint.Value}");
            }
        }
        
        // 在Level5B中打死对方获得双方本轮总分
        else if (currentLevel.Value == Level.Level5B)
        {
            ulong shooterId = shotPlayerId == 0ul ? 1ul : 0ul;
            
            if (shooterId == 0ul)
            {
                player1TotalPoint.Value += roundPoints;
                player1BonusText = $"<color=yellow><shake a=0.3 f=0.8>+{roundPoints}!</shake></color>";
                Debug.Log($"[LevelManager] Level5B规则: 玩家1击中对手，获得本轮分数 {roundPoints}，当前总分: {player1TotalPoint.Value}");
            }
            else if (shooterId == 1ul)
            {
                player2TotalPoint.Value += roundPoints;
                player2BonusText = $"<color=yellow><shake a=0.3 f=0.8>+{roundPoints}!</shake></color>";
                Debug.Log($"[LevelManager] Level5B规则: 玩家2击中对手，获得本轮分数 {roundPoints}，当前总分: {player2TotalPoint.Value}");
            }
        }
        
        // 在Level6A中打死对方获得双方全部总分
        else if (currentLevel.Value == Level.Level6A)
        {
            ulong shooterId = shotPlayerId == 0ul ? 1ul : 0ul;
            
            if (shooterId == 0ul)
            {
                // 在设置奖励文本前先保存对方当前的总分
                float stolenPoints = player2TotalPoint.Value;
                
                // 对方总分可能为0，所以显示至少+1分
                string bonusAmount = stolenPoints > 0 ? stolenPoints.ToString() : "对方所有金币";
                player1BonusText = $"<color=yellow><shake a=0.3 f=0.8>+{bonusAmount}!</shake></color>";
                player2BonusText = "<color=red><shake a=0.3 f=0.8>归零!</shake></color>";
                
                // 然后更新玩家分数
                player1TotalPoint.Value += stolenPoints;
                player2TotalPoint.Value = 0;
                
                Debug.Log($"[LevelManager] Level6A规则: 玩家1击中对手，获得对方全部分数 {stolenPoints}，当前总分: {player1TotalPoint.Value}");
            }
            else if (shooterId == 1ul)
            {
                // 在设置奖励文本前先保存对方当前的总分
                float stolenPoints = player1TotalPoint.Value;
                
                // 对方总分可能为0，所以显示至少+1分
                string bonusAmount = stolenPoints > 0 ? stolenPoints.ToString() : "对方所有金币";
                player2BonusText = $"<color=yellow><shake a=0.3 f=0.8>+{bonusAmount}!</shake></color>";
                player1BonusText = "<color=red><shake a=0.3 f=0.8>归零!</shake></color>";
                
                // 然后更新玩家分数
                player2TotalPoint.Value += stolenPoints;
                player1TotalPoint.Value = 0;
                
                Debug.Log($"[LevelManager] Level6A规则: 玩家2击中对手，获得对方全部分数 {stolenPoints}，当前总分: {player2TotalPoint.Value}");
            }
        }
        
        // 立即通知UI更新 - 先更新总分，再显示奖励文本
        ApplyShotPenaltyClientRpc(player1BonusText, player2BonusText);
    }
    
    [ClientRpc]
    private void ApplyShotPenaltyClientRpc(string player1BonusText, string player2BonusText)
    {
        Debug.Log($"[LevelManager] 客户端收到击中奖励更新: 玩家1=\"{player1BonusText}\", 玩家2=\"{player2BonusText}\"");
        
        // 先显示奖励提示
        if (UIManager.Instance != null)
        {
            // 显示奖励提示
            if (!string.IsNullOrEmpty(player1BonusText) || !string.IsNullOrEmpty(player2BonusText))
            {
                UIManager.Instance.ShowBonusText(player1BonusText, player2BonusText);
            }
            
            // 强制立即更新玩家总分UI
            // 延迟一帧后更新总分显示，确保网络变量已经同步
            StartCoroutine(DelayedTotalScoreUpdate());
        }
    }
    
    // 延迟更新总分的协程
    private IEnumerator DelayedTotalScoreUpdate()
    {
        // 等待一帧，确保网络变量已同步
        yield return null;
        
        // 更新一次
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdatePlayerTotalScoreText();
        }
        
        // 再等待短暂时间
        yield return new WaitForSeconds(0.2f);
        
        // 再次更新，确保显示最新的值
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdatePlayerTotalScoreText();
        }
    }

    /// <summary>
    /// 应用Level6规则（总分高者胜利，总分低者分数归0）
    /// </summary>
    public void ApplyLevel6Rule()
    {
        if (!IsServer) return;
        
        if (currentLevel.Value == Level.Level6A || currentLevel.Value == Level.Level6B)
        {
            if (player1TotalPoint.Value > player2TotalPoint.Value)
            {
                player2TotalPoint.Value = 0;
            }
            else if (player2TotalPoint.Value > player1TotalPoint.Value)
            {
                player1TotalPoint.Value = 0;
            }
            // 如果平局，不做处理
        }
    }

    public void AddPlayer1TotalPoint(float points)
    {
        if (!IsServer) return;
        
        // 在教程模式下不计入统计
        if (currentMode.Value == Mode.Tutor)
            return;
        
        player1TotalPoint.Value += points;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddPlayer2TotalPointServerRpc(float points)
    {
        if (!IsServer) return;
        
        // 在教程模式下不计入统计
        if (currentMode.Value == Mode.Tutor)
            return;
        
        player2TotalPoint.Value += points;
    }

    private void OnDialogComplete()
    {
        Debug.Log("[LevelManager] 对话完成回调被触发，准备显示身份选择面板");
        
        // 只在关卡3A和3B显示身份选择面板
        if (currentLevel.Value == Level.Level3A || currentLevel.Value == Level.Level3B)
        {
            // 通知所有客户端显示身份选择面板
            ShowIdentityPanelClientRpc();
        }
        else
        {
            // 其他关卡直接进入游戏
            Debug.Log("[LevelManager] 非身份选择关卡，直接进入游戏");
        }
    }
    
    /// <summary>
    /// 当玩家完成身份选择后调用此方法
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void IdentitySelectionCompleteServerRpc(ulong clientId, PlayerIdentity identity)
    {
        if (!IsServer) return;
        
        Debug.Log($"[LevelManager] 玩家{clientId}选择了身份: {identity}");
        
        // 设置玩家身份
        if (clientId == 0)
            player1Identity.Value = identity;
        else if (clientId == 1)
            player2Identity.Value = identity;
        
        // 检查是否所有玩家都已选择身份
        bool allPlayersSelected = (player1Identity.Value != PlayerIdentity.None && 
                                  player2Identity.Value != PlayerIdentity.None);
        
        if (allPlayersSelected)
        {
            Debug.Log("[LevelManager] 所有玩家都已选择身份，继续游戏");
            
            // 通知所有客户端隐藏身份选择面板
            HideIdentityPanelClientRpc();
            
            // 继续游戏流程
            ContinueGameAfterIdentitySelectionClientRpc();
        }
    }
    
    [ClientRpc]
    private void HideIdentityPanelClientRpc()
    {
        Debug.Log("[LevelManager] 隐藏身份选择面板");
        if (IdentityPanelManager.Instance != null)
        {
            // 从IdentityPanelManager.cs的实现可以看出，它有一个identityPanel字段
            // 在ShowPanel方法中使用identityPanel.SetActive(true)来显示面板
            // 在OnIdentitySelected方法中使用identityPanel.SetActive(false)来隐藏面板
            // 所以我们可以直接访问identityPanel并设置其活动状态为false
            
            // 但由于identityPanel是私有字段，我们无法直接访问
            // 所以我们可以通过反射来访问它，或者简单地隐藏IdentityPanelManager的GameObject
            
            // 隐藏IdentityPanelManager的GameObject
            GameObject panelObject = GameObject.Find("IdentityPanel");
            if (panelObject != null)
            {
                panelObject.SetActive(false);
            }
        }
    }
    
    [ClientRpc]
    private void ContinueGameAfterIdentitySelectionClientRpc()
    {
        Debug.Log("[LevelManager] 身份选择完成，继续游戏");
        
        // 在这里可以添加继续游戏的逻辑
        // 例如开始回合、显示UI等
        
        // 如果是服务器，更新游戏状态
        if (IsServer)
        {
            GameManager.Instance.currentGameState = GameManager.GameState.Ready;
        }
    }
    
    [ClientRpc]
    private void ShowIdentityPanelClientRpc()
    {
        Debug.Log("[LevelManager] ShowIdentityPanelClientRpc被调用，检查IdentityPanelManager实例");
        if (IdentityPanelManager.Instance != null)
        {
            Debug.Log("[LevelManager] IdentityPanelManager实例存在，调用ShowPanel方法");
            IdentityPanelManager.Instance.ShowPanel();
        }
        else
        {
            Debug.LogError("[LevelManager] IdentityPanelManager.Instance为空，无法显示身份选择面板");
        }
    }
    
    /// <summary>
    /// 判断关卡是否可选择
    /// </summary>
    public bool IsLevelSelectable(Level level)
    {
        // 教程关卡始终可选
        if (level == Level.Tutorial)
            return true;
            
        // 当前解锁的关卡层级
        int currentUnlockedTier = GetLevelTier(unlockedLevel.Value);
        int targetLevelTier = GetLevelTier(level);
        
        // 只能选择当前解锁层级的关卡
        if (targetLevelTier != currentUnlockedTier)
            return false;
            
        // 第三层：根据已完成路径选择3A或3B
        if (targetLevelTier == 3)
        {
            // 如果尚未选择路径，两个关卡都可选
            if (completedPath.Value == 0)
                return true;
                
            // 如果已选择路径A，只能选择3A
            if (completedPath.Value == 1)
                return level == Level.Level3A;
                
            // 如果已选择路径B，只能选择3B
            if (completedPath.Value == 2)
                return level == Level.Level3B;
        }
        
        // 对于其他层级（包括第四、五、六层），如果层级匹配当前解锁层级，则可选
        return true;
    }
    
    /// <summary>
    /// 获取关卡所属的层级
    /// </summary>
    private int GetLevelTier(Level level)
    {
        switch (level)
        {
            case Level.Tutorial:
                return 0;
            case Level.Level1:
                return 1;
            case Level.Level2:
                return 2;
            case Level.Level3A:
            case Level.Level3B:
                return 3;
            case Level.Level4A:
            case Level.Level4B:
            case Level.Level4C:
                return 4;
            case Level.Level5A:
            case Level.Level5B:
                return 5;
            case Level.Level6A:
            case Level.Level6B:
                return 6;
            default:
                return -1;
        }
    }
    
    /// <summary>
    /// 更新解锁的关卡
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void UpdateUnlockedLevelServerRpc()
    {
        if (!IsServer) return;
        
        // 根据当前关卡和已完成路径更新下一个解锁的关卡
        switch (currentLevel.Value)
        {
            case Level.Tutorial:
                unlockedLevel.Value = Level.Level1;
                break;
            case Level.Level1:
                unlockedLevel.Value = Level.Level2;
                break;
            case Level.Level2:
                // 解锁Level3A和Level3B
                unlockedLevel.Value = Level.Level3A;
                break;
            case Level.Level3A:
            case Level.Level3B:
                // 解锁Level4A、Level4B和Level4C
                unlockedLevel.Value = Level.Level4A;
                break;
            case Level.Level4A:
            case Level.Level4B:
            case Level.Level4C:
                // 解锁Level5A和Level5B
                unlockedLevel.Value = Level.Level5A;
                break;
            case Level.Level5A:
            case Level.Level5B:
                // 解锁Level6A和Level6B
                unlockedLevel.Value = Level.Level6A;
                break;
            case Level.Level6A:
            case Level.Level6B:
                // 游戏结束，不解锁新关卡
                break;
        }
        
        Debug.Log($"[LevelManager] 已解锁新关卡: {unlockedLevel.Value}");
    }

    [ClientRpc]
    private void PlayDialogForLevelClientRpc(Level level)
    {
        Debug.Log($"[LevelManager] 客户端收到播放对话请求，关卡={level}, IsServer={IsServer}, IsClient={IsClient}");
        
        // 如果是服务器，跳过（因为服务器已经在OnLevelValueChanged中调用了PlayDialogForLevel）
        if (IsServer) return;
        
        // 对于Level3A和Level3B，不要在这里处理，因为它们需要特殊的分段显示处理
        // 这些关卡的处理已经在PlayDialogForLevel方法中通过PlaySegmentedDialog处理
        if (level != Level.Level3A && level != Level.Level3B)
        {
            PlayDialogForLevel(level);
        }
        else
        {
            // 对于Level3A和Level3B，设置pendingDialogLevel并触发延迟播放
            pendingDialogLevel = level;
            StartCoroutine(PlayDialogAfterDelay());
        }
    }

    // 新增：场景加载完成的事件处理
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Debug.Log($"[LevelManager] 场景 {scene.name} 加载完成");
        
        // 如果是Game场景并且有待播放的对话，则播放对话
        if (scene.name == "Game" && shouldPlayDialogAfterSceneLoad)
        {
            Debug.Log($"[LevelManager] 场景加载后播放对话，关卡={pendingDialogLevel}");
            shouldPlayDialogAfterSceneLoad = false;
            
            // 延迟一帧再播放对话，确保所有对象都已初始化
            StartCoroutine(PlayDialogAfterDelay());
        }
    }

    // 新增：延迟播放对话的协程
    private IEnumerator PlayDialogAfterDelay()
    {
        // 等待一帧
        yield return null;
        
        // 再等待0.5秒确保UI已完全加载
        yield return new WaitForSeconds(0.5f);
        
        // 播放对话 - 移除IsServer检查，让客户端也能执行
        if (IsServer)
        {
            // 服务器需要同时播放对话并通知客户端
            PlayDialogForLevel(pendingDialogLevel);
            PlayDialogForLevelClientRpc(pendingDialogLevel);
        }
        else
        {
            // 客户端只需要播放自己的对话
            PlayDialogForLevel(pendingDialogLevel);
        }
    }

    /// <summary>
    /// 完成当前关卡，自动解锁下一关卡
    /// </summary>
    public void CompleteCurrentLevel()
    {
        if (!IsServer) return;
        
        Debug.Log($"[LevelManager] 完成当前关卡: {currentLevel.Value}");
        
        // 更新解锁的关卡
        UpdateUnlockedLevelServerRpc();
        
        // 通知UI更新关卡选择界面
        UpdateLevelSelectionUIClientRpc();
    }
    
    [ClientRpc]
    private void UpdateLevelSelectionUIClientRpc()
    {
        Debug.Log("[LevelManager] 通知客户端更新关卡选择界面");
        
        // 移除对不存在的LevelSelectionUIManager的引用
        // 如果有UI管理器，可以在这里添加对其更新方法的调用
        // 例如：UIManager.Instance?.UpdateLevelSelectionUI();
    }
    
    /// <summary>
    /// 在关卡结束时调用，处理关卡完成逻辑
    /// </summary>
    public void OnLevelComplete()
    {
        if (!IsServer) return;
        
        // 完成当前关卡，解锁下一关卡
        CompleteCurrentLevel();
        
        // 如果是最终关卡，触发游戏结束逻辑
        if (currentLevel.Value == Level.Level6A || currentLevel.Value == Level.Level6B)
        {
            // 设置最终关卡标志
            isFinalLevel.Value = true;
            
            // 应用Level6规则
            ApplyLevel6Rule();
            
            // 触发游戏结束
            GameEndedClientRpc();
        }
    }
    
    /// <summary>
    /// 检查当前是否是最终关卡
    /// </summary>
    public bool IsFinalLevel()
    {
        return currentLevel.Value == Level.Level6A || currentLevel.Value == Level.Level6B;
    }
    
    [ClientRpc]
    private void GameEndedClientRpc()
    {
        Debug.Log("[LevelManager] 游戏结束，显示结算界面");
        
        // 移除对不存在的GameEndUIManager的引用
        // 如果有UI管理器，可以在这里添加对其显示结算界面方法的调用
        // 例如：UIManager.Instance?.ShowGameEndUI();
    }

    /// <summary>
    /// 当玩家被枪击中时调用此方法
    /// 应该在检测到玩家被击中后调用，例如在UIManager或GunController中
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void PlayerShotServerRpc(ulong shotPlayerId, float roundPoints)
    {
        if (!IsServer) return;
        
        Debug.Log($"[LevelManager] 收到玩家被击中通知，被击中玩家ID: {shotPlayerId}, 本轮分数: {roundPoints}");
        
        // 应用枪击规则 - 已包含更新UI的逻辑
        ApplyShotPenalty(shotPlayerId, roundPoints);
        
        // 如果是最终关卡，可能需要提前结束游戏
        if (currentLevel.Value == Level.Level6A)
        {
            // 最终关卡被击中直接结束游戏
            OnLevelComplete();
        }
    }
}
