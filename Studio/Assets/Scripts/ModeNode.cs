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
        // ֻ�е�����ȷ����� NetworkBehaviour �Ѿ��� Spawn�����ܷ� RPC
        if (!_isSpawned) return;
        Debug.Log($"[ModeNode] �����ģʽ�ڵ�: {type}");

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
