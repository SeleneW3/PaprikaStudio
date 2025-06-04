using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;
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
    

    [Header("当前模式")]
    public NetworkVariable<Mode> currentMode = new NetworkVariable<Mode>(Mode.Tutor
        , NetworkVariableReadPermission.Everyone,       // 所有客户端都能读取
        NetworkVariableWritePermission.Server);

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

}
