using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BlockLogic : MonoBehaviour
{
    public enum Belonging
    {
        Player1,
        Player2
    }

    public enum Type
    {
        Cooperate,
        Cheat
    }

    public Belonging belonging;
    public Type type;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    public Color hoverColor = new Color(1f, 1f, 1f, 0.8f); // 默认悬停颜色，可在Inspector中修改
    private bool isSelected = false;

    public Transform targetPoint;

    void Start()
    {
        // 获取SpriteRenderer组件并保存原始颜色
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    void OnMouseEnter()
    {
        //Debug.Log($"Mouse entered: belonging={belonging}, isSelected={isSelected}");
        if (spriteRenderer != null && !isSelected)
        {
            spriteRenderer.color = hoverColor;
        }
    }

    void OnMouseExit()
    {
        if (spriteRenderer != null && !isSelected)
        {
            spriteRenderer.color = originalColor;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnMouseDown()
    {
        //Debug.Log($"OnMouseDown - LocalClientId: {NetworkManager.Singleton.LocalClientId}, belonging: {belonging}, type: {type}");
        
        // 播放Block点击音效
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("BlockClick");
        }
        
        if(NetworkManager.Singleton.LocalClientId == 0)
        {
            //Debug.Log("Detected as Player1 client");
            if (belonging == Belonging.Player1 && type == Type.Cooperate)
            {
                //Debug.Log("Player1 selecting Cooperate");
                GameManager.Instance.SetPlayer1ChoiceServerRpc(PlayerLogic.playerChoice.Cooperate);
                GameManager.Instance.ChangeChess1ClickPointServerRpc(targetPoint.position, targetPoint.rotation);
                SetSelected(true);
            }
            else if (belonging == Belonging.Player1 && type == Type.Cheat)
            {
                GameManager.Instance.SetPlayer1ChoiceServerRpc(PlayerLogic.playerChoice.Cheat);
                GameManager.Instance.ChangeChess1ClickPointServerRpc(targetPoint.position, targetPoint.rotation);
                SetSelected(true);
            }
        }

        if (NetworkManager.Singleton.LocalClientId == 1)
        {
            if (belonging == Belonging.Player2 && type == Type.Cooperate)
            {
                GameManager.Instance.SetPlayer2ChoiceServerRpc(PlayerLogic.playerChoice.Cooperate);
                GameManager.Instance.ChangeChess2ClickPointServerRpc(targetPoint.position,targetPoint.rotation);
                SetSelected(true);
            }
            else if (belonging == Belonging.Player2 && type == Type.Cheat)
            {
                GameManager.Instance.SetPlayer2ChoiceServerRpc(PlayerLogic.playerChoice.Cheat);
                GameManager.Instance.ChangeChess2ClickPointServerRpc(targetPoint.position, targetPoint.rotation);
                SetSelected(true);
            }
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = selected ? hoverColor : originalColor;
        }
    }

    public void ResetState()
    {
        isSelected = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
}
