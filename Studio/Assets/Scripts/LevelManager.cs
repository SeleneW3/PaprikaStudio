using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class LevelManager : NetworkBehaviour
{
    public static LevelManager Instance { get; private set; }

    public enum Mode
    {
        Tutor,
        OnlyCard,
        OnlyGun,
        CardAndGun
    }

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

    [Header("当前模式 (同步用)")]
    public NetworkVariable<Mode> currentMode = new NetworkVariable<Mode>(
        Mode.Tutor,
        NetworkVariableReadPermission.Everyone,     
        NetworkVariableWritePermission.Server        
    );



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
    }

    public override void OnNetworkDespawn()
    {
        currentMode.OnValueChanged -= OnModeChanged;
        base.OnNetworkDespawn();
    }

    private void OnModeChanged(Mode previous, Mode current)
    {
        Debug.Log($"[LevelManager] 模式从 {previous} 切换到了 {current}");
        // TODO: 在这里做本地化的 UI 刷新 / 场景切换逻辑
        // 例如：根据 current 来开启/关闭不同的界面、准备场景、等等
    }

    /// <summary>
    /// 这个 ServerRpc 让客户端请求“切换模式”由服务器端来实际写入 currentMode.Value。
    /// RequireOwnership=false 表示“不是这个脚本的 NetworkObject 拥有者也能调用”。
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
    /// 供服务器本地直接修改 currentMode 的方法，如果你在 Host（既是Server又是Client）上点击，也可以直接调用这个。
    /// </summary>
    public void SetModeOnServer(Mode newMode)
    {
        if (!IsServer) return;
        currentMode.Value = newMode;
    }

    public void AddPlayer1TotalPoint(float points)
    {
        player1TotalPoint.Value += points;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddPlayer2TotalPointServerRpc(float points)
    {
        if (!IsServer) return;
        player2TotalPoint.Value += points;
    }
}
