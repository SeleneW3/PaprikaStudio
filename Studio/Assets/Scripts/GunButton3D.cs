using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunButton3D : MonoBehaviour
{
    public enum ButtonType
    {
        Throw,
        Spin,
        Fire
    }
    [Header("按钮类型")]
    public ButtonType buttonType;

    [Header("玩家编号（1或2）")]
    public int playerIndex = 1; // 1=玩家1，2=玩家2

    [Header("高亮颜色")]
    public Color hoverColor = new Color(1f, 1f, 0.5f, 1f);

    private Color originalColor;
    private Renderer rend;

    // Start is called before the first frame update
    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
            originalColor = rend.material.color;
    }

    void OnMouseEnter()
    {
        if (rend != null)
            rend.material.color = hoverColor;
        Debug.Log($"[GunButton3D] MouseEnter: Player{playerIndex} {buttonType} 按钮");
    }

    void OnMouseExit()
    {
        if (rend != null)
            rend.material.color = originalColor;
        Debug.Log($"[GunButton3D] MouseExit: Player{playerIndex} {buttonType} 按钮");
    }

    void OnMouseDown()
    {
        // 播放音效（可选）
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX("BlockClick");

        // 找到对应的枪
        string gunName = playerIndex == 1 ? "Gun1" : "Gun2";
        GunController gun = GameObject.Find(gunName)?.GetComponent<GunController>();
        if (gun == null) return;

        // 只允许本地玩家点击自己一侧的按钮
        if (Unity.Netcode.NetworkManager.Singleton != null)
        {
            ulong localId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            if ((playerIndex == 1 && localId != 0) || (playerIndex == 2 && localId != 1))
                return;
        }

        // 获取RoundManager
        RoundManager roundManager = FindObjectOfType<RoundManager>();
        if (roundManager == null) return;

        // 执行对应操作
        switch (buttonType)
        {
            case ButtonType.Throw:
                gun.PlayThrowAnimation();
                break;
            case ButtonType.Spin:
                gun.PlaySpinAnimation();
                break;
            case ButtonType.Fire:
                // 只有有开枪权限时才允许开枪
                if ((playerIndex == 1 && roundManager.player1CanFire.Value) ||
                    (playerIndex == 2 && roundManager.player2CanFire.Value))
                {
                    gun.FireGun();
                }
                else
                {
                    Debug.Log($"[GunButton3D] Player{playerIndex} 没有开枪权限，无法开枪");
                }
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
