using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinIpLogic : MonoBehaviour
{
    public TMP_InputField joinIP;
    public GameObject joinPanel;
    public Button confirmButton;
    public Button exitButton;
    private ButtonLogic buttonLogic;

    void Start()
    {
        // 初始化时隐藏面板
        if (joinPanel != null)
        {
            joinPanel.SetActive(false);
        }

        // 设置按钮监听
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(() => {
                if (buttonLogic != null)
                {
                    buttonLogic.ExecuteJoin();
                }
                joinPanel.SetActive(false);
            });
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(() => {
                joinPanel.SetActive(false);
            });
        }
    }

    void Update()
    {
        // 只保留ESC关闭功能
        if (joinPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            joinPanel.SetActive(false);
        }
    }

    public void Initialize(ButtonLogic logic)
    {
        buttonLogic = logic;
    }

    public void ShowJoinPanel()
    {
        if (joinPanel != null)
        {
            joinPanel.SetActive(true);
            
            if (joinIP != null)
            {
                joinIP.text = GameManager.Instance.localIP;
                joinIP.Select();
                joinIP.ActivateInputField();
            }
        }
    }
}

