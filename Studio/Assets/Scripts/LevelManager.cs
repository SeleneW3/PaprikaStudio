using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System;

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

    // 事件 - 当关卡改变时
    public event Action<Level, Level> OnLevelChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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
        
        // 播放关卡相应的对话
        PlayDialogForLevel(current);
        
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

    // 播放关卡相应的对话
    private void PlayDialogForLevel(Level level)
    {
        if (!IsServer) return;
        
        if (DialogManager.Instance == null)
        {
            Debug.LogError("[LevelManager] DialogManager.Instance is null!");
            return;
        }
        
        switch (level)
        {
            case Level.Level1:
                DialogManager.Instance.PlayRange(13, 15);
                break;
            case Level.Level2:
                DialogManager.Instance.PlayRange(16, 19);
                break;
            case Level.Level3A:
                {
                    // 获取玩家当前的合作和欺骗次数
                    int p1CoopCount = player1CoopTimes.Value;
                    int p1CheatCount = player1CheatTimes.Value;
                    int p2CoopCount = player2CoopTimes.Value;
                    int p2CheatCount = player2CheatTimes.Value;
                    
                    // 创建自定义对话内容
                    string p1Dialog = $"玩家1，\n你现在合作{p1CoopCount}次，\n欺骗{p1CheatCount}次";
                    string p2Dialog = $"玩家2，\n你现在合作{p2CoopCount}次，\n欺骗{p2CheatCount}次";
                    
                    // 使用DialogManager播放对话
                    DialogManager.Instance.PlayCustomDialog(p1Dialog, p2Dialog, OnDialogComplete);
                    
                    // 设置完成路径为路径A
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
                    
                    // 创建自定义对话内容
                    string p1Dialog = $"玩家1，\n你现在合作{p1CoopCount}次，\n欺骗{p1CheatCount}次";
                    string p2Dialog = $"玩家2，\n你现在合作{p2CoopCount}次，\n欺骗{p2CheatCount}次";
                    
                    // 使用DialogManager播放对话
                    DialogManager.Instance.PlayCustomDialog(p1Dialog, p2Dialog, OnDialogComplete);
                    
                    // 设置完成路径为路径B
                    completedPath.Value = 2;
                }
                break;
            case Level.Level4A:
            case Level.Level4B:
            case Level.Level4C:
                DialogManager.Instance.PlayRange(20, 21);
                break;
            case Level.Level5A:
                DialogManager.Instance.PlayRange(22, 24);
                break;
            case Level.Level5B:
                DialogManager.Instance.PlayRange(25, 26);
                break;
            case Level.Level6A:
                DialogManager.Instance.PlayRange(27, 31);
                break;
            case Level.Level6B:
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
        
        currentLevel.Value = newLevel;
        
        // 关卡切换时自动设置对应的模式并加载场景
        GameManager.Instance.currentGameState = GameManager.GameState.Ready;
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
        
        // 欺骗派：六局欺骗总数达到12次则获得额外加分+15
        if (player1Identity.Value == PlayerIdentity.Cheater && player1CheatTimes.Value >= 12)
        {
            player1TotalPoint.Value += 15;
        }
        
        if (player2Identity.Value == PlayerIdentity.Cheater && player2CheatTimes.Value >= 12)
        {
            player2TotalPoint.Value += 15;
        }
        
        // 合作派：六局合作总数达到8次则获得额外加分+15
        if (player1Identity.Value == PlayerIdentity.Cooperator && player1CoopTimes.Value >= 8)
        {
            player1TotalPoint.Value += 15;
        }
        
        if (player2Identity.Value == PlayerIdentity.Cooperator && player2CoopTimes.Value >= 8)
        {
            player2TotalPoint.Value += 15;
        }
    }

    /// <summary>
    /// 应用被枪打死的规则（本轮分数归零）
    /// </summary>
    public void ApplyShotPenalty(ulong shotPlayerId, float roundPoints)
    {
        if (!IsServer) return;
        
        // 在Level5A中被打死获得10分作为补偿
        if (currentLevel.Value == Level.Level5A)
        {
            if (shotPlayerId == 0ul)
                player1TotalPoint.Value += 10;
            else if (shotPlayerId == 1ul)
                player2TotalPoint.Value += 10;
        }
        
        // 在Level5B中打死对方获得双方本轮总分
        if (currentLevel.Value == Level.Level5B)
        {
            ulong shooterId = shotPlayerId == 0ul ? 1ul : 0ul;
            
            if (shooterId == 0ul)
                player1TotalPoint.Value += roundPoints;
            else if (shooterId == 1ul)
                player2TotalPoint.Value += roundPoints;
        }
        
        // 在Level6A中打死对方获得双方全部总分
        if (currentLevel.Value == Level.Level6A)
        {
            ulong shooterId = shotPlayerId == 0ul ? 1ul : 0ul;
            
            if (shooterId == 0ul)
            {
                player1TotalPoint.Value += player2TotalPoint.Value;
                player2TotalPoint.Value = 0;
            }
            else if (shooterId == 1ul)
            {
                player2TotalPoint.Value += player1TotalPoint.Value;
                player1TotalPoint.Value = 0;
            }
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

    [ClientRpc]
    private void ShowIdentityPanelClientRpc()
    {
        if (IdentityPanelManager.Instance != null)
        {
            IdentityPanelManager.Instance.ShowPanel();
        }
    }

    private void OnDialogComplete()
    {
        // 通知所有客户端显示身份选择面板
        ShowIdentityPanelClientRpc();
    }
    
    /// <summary>
    /// 判断关卡是否可选择
    /// </summary>
    public bool IsLevelSelectable(Level level)
    {
        // 教程关卡始终可选
        if (level == Level.Tutorial)
            return true;
            
        // 第一层：只有在未解锁其他关卡时可选
        if (level == Level.Level1)
            return unlockedLevel.Value == Level.Level1;
            
        // 第二层
        if (level == Level.Level2)
            return unlockedLevel.Value == Level.Level2;
            
        // 第三层：只能选择3A或3B中的一个
        if (level == Level.Level3A || level == Level.Level3B)
            return unlockedLevel.Value == Level.Level3A;
            
        // 第四层：可以选择4A、4B或4C中的任意一个
        if (level == Level.Level4A || level == Level.Level4B || level == Level.Level4C)
            return unlockedLevel.Value == Level.Level4A;
            
        // 第五层：只能选择5A或5B中的一个
        if (level == Level.Level5A || level == Level.Level5B)
            return unlockedLevel.Value == Level.Level5A;
            
        // 第六层：只能选择6A或6B中的一个
        if (level == Level.Level6A || level == Level.Level6B)
            return unlockedLevel.Value == Level.Level6A;
            
        return false;
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
}
