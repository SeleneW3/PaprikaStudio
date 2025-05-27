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
    public Mode currentMode = Mode.Tutor;

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
