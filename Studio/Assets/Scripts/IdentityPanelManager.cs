using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class IdentityPanelManager : MonoBehaviour
{
    public static IdentityPanelManager Instance { get; private set; }
    
    [SerializeField] private GameObject identityPanel;
    [SerializeField] private Button cooperatorButton;
    [SerializeField] private Button cheaterButton;
    
    private void Awake()
    {
        Instance = this;
        identityPanel.SetActive(false);
        
        cooperatorButton.onClick.AddListener(() => OnIdentitySelected(LevelManager.PlayerIdentity.Cooperator));
        cheaterButton.onClick.AddListener(() => OnIdentitySelected(LevelManager.PlayerIdentity.Cheater));
    }
    
    public void ShowPanel()
    {
        // 检查当前玩家是否已经选择了身份
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        bool hasIdentity = false;
        
        if (clientId == 0 && LevelManager.Instance.player1Identity.Value != LevelManager.PlayerIdentity.None)
            hasIdentity = true;
        else if (clientId == 1 && LevelManager.Instance.player2Identity.Value != LevelManager.PlayerIdentity.None)
            hasIdentity = true;
        
        // 如果已经选择了身份，不显示面板
        if (hasIdentity)
        {
            Debug.Log($"[IdentityPanelManager] 玩家{clientId}已经选择了身份，不再显示选择面板");
            return;
        }
        
        // 显示面板
        identityPanel.SetActive(true);
    }
    
    private void OnIdentitySelected(LevelManager.PlayerIdentity identity)
    {
        // 获取本地客户端ID
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        
        // 调用LevelManager的方法设置身份
        LevelManager.Instance.SetPlayerIdentityServerRpc(clientId, identity);
        
        // 隐藏面板
        identityPanel.SetActive(false);
        
        Debug.Log($"[IdentityPanelManager] 玩家{clientId}选择了身份: {identity}");
        
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("ButtonClick");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
