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
        // 鼠标悬停时改变颜色
        if (spriteRenderer != null)
        {
            spriteRenderer.color = hoverColor;
        }
    }

    void OnMouseExit()
    {
        // 鼠标离开时恢复原色
        if (spriteRenderer != null)
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
        if(NetworkManager.Singleton.LocalClientId == 0)
        {
            if (belonging == Belonging.Player1 && type == Type.Cooperate)
            {
                GameManager.Instance.SetPlayer1ChoiceServerRpc(PlayerLogic.playerChoice.Cooperate);
                GameManager.Instance.ChangeChess1ClickPointServerRpc(transform.position, transform.rotation);
            }
            else if (belonging == Belonging.Player1 && type == Type.Cheat)
            {
                GameManager.Instance.SetPlayer1ChoiceServerRpc(PlayerLogic.playerChoice.Cheat);
                GameManager.Instance.ChangeChess1ClickPointServerRpc(transform.position, transform.rotation);
            }
        }

        if (NetworkManager.Singleton.LocalClientId == 1)
        {
            if (belonging == Belonging.Player2 && type == Type.Cooperate)
            {
                GameManager.Instance.SetPlayer2ChoiceServerRpc(PlayerLogic.playerChoice.Cooperate);
                GameManager.Instance.ChangeChess2ClickPointServerRpc(transform.position,transform.rotation);
            }
            else if (belonging == Belonging.Player2 && type == Type.Cheat)
            {
                GameManager.Instance.SetPlayer2ChoiceServerRpc(PlayerLogic.playerChoice.Cheat);
                GameManager.Instance.ChangeChess2ClickPointServerRpc(transform.position, transform.rotation);
            }
        }
    }
}
