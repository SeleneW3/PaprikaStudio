using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class ModeNode : NetworkBehaviour
{
    public LevelManager.Mode type;
    public LevelManager.Level level;
    public bool isLevelNode = false; // 是否为关卡节点
    private bool _isSpawned = false;
    
    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color lockedColor = Color.gray;
    
    private SpriteRenderer _spriteRenderer;
    private bool _isSelectable = false;

    private void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        // 初始化颜色
        UpdateVisuals();
    }
    
    private void Update()
    {
        // 更新节点状态
        CheckSelectability();
        
        // 更新视觉效果
        UpdateVisuals();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _isSpawned = true;
    }
    
    private void CheckSelectability()
    {
        if (LevelManager.Instance == null)
            return;
            
        if (isLevelNode)
        {
            // 检查关卡是否可选
            _isSelectable = LevelManager.Instance.IsLevelSelectable(level);
        }
        else
        {
            // 模式节点始终可选
            _isSelectable = true;
        }
    }
    
    private void UpdateVisuals()
    {
        if (_spriteRenderer == null)
            return;
            
        if (_isSelectable)
        {
            // 可选状态：正常颜色
            _spriteRenderer.color = normalColor;
        }
        else
        {
            // 锁定状态：灰色
            _spriteRenderer.color = lockedColor;
        }
    }

    private void OnMouseDown()
    {
        // 只有当网络对象已经Spawn才能发送RPC
        if (!_isSpawned) return;
        
        // 如果节点被锁定，不响应点击
        if (!_isSelectable)
        {
            Debug.Log($"[ModeNode] 节点已锁定，无法选择");
            
            // 可以在这里添加锁定音效
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX("ESC");
            }
            
            return;
        }
        
        if (isLevelNode)
        {
            Debug.Log($"[ModeNode] 点击了关卡节点: {level}");
            
            if (LevelManager.Instance.IsServer)
            {
                LevelManager.Instance.SetLevelOnServer(level);
            }
            else
            {
                LevelManager.Instance.ChangeLevelServerRpc(level);
            }
        }
        else
        {
            Debug.Log($"[ModeNode] 点击了模式节点: {type}");
            
            if (LevelManager.Instance.IsServer)
            {
                LevelManager.Instance.SetModeOnServer(type);
            }
            else
            {
                LevelManager.Instance.ChangeModeServerRpc(type);
            }
        }
        
        // 播放选择音效
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("CardClick");
        }
    }
}
