using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class ModeNode : NetworkBehaviour
{
    public LevelManager.Mode type;
    private bool _isSpawned = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _isSpawned = true;
    }

    private void OnMouseDown()
    {
        // 只有当本地确认这个 NetworkBehaviour 已经被 Spawn，才能发 RPC
        if (!_isSpawned) return;
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
}
