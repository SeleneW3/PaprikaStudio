using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinIpLogic : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField joinIP;
    public GameObject joinPanel;
    public Button confirmButton;
    public Button exitButton;
    public ButtonLogic buttonLogic;

    [Header("Sound Effects")]
    [SerializeField] private string uiClickSound = "ButtonClick"; // UI 点击音效

    void Start()
    {
        // 初始时隐藏加入面板
        if (joinPanel != null)
        {
            joinPanel.SetActive(false);
        }

        // 1) 当用户在输入框按下 Enter 时，立刻更新 GameManager.Instance.joinIP（但不执行加入逻辑）
        if (joinIP != null)
        {
            // onSubmit 会在用户按下 Enter 提交时触发
            joinIP.onSubmit.AddListener((string value) =>
            {
                string enteredIp = value.Trim();
                if (string.IsNullOrEmpty(enteredIp))
                {
                    Debug.LogWarning("JoinIpLogic：按下 Enter 时输入框内容为空，跳过更新 joinIP。");
                }
                else
                {
                    GameManager.Instance.joinIP = enteredIp;
                    Debug.Log($"JoinIpLogic：按下 Enter，已将 joinIP 更新为 {enteredIp}");
                }
            });
        }

        // 2) “确认”按钮：点击时播放音效并调用 ExecuteJoin 来执行加入逻辑
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(() =>
            {
                PlayUIClickSound();

                if (buttonLogic != null)
                {
                    buttonLogic.ExecuteJoin();
                }
                else
                {
                    Debug.LogError("JoinIpLogic：buttonLogic 引用为 null，无法调用 ExecuteJoin()");
                }
            });
        }

        // 3) “退出”按钮：点击时关闭面板
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(() =>
            {
                PlayUIClickSound();

                if (joinPanel != null)
                {
                    joinPanel.SetActive(false);
                }
            });
        }
    }

    // 播放 UI 点击音效
    private void PlayUIClickSound()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(uiClickSound);
        }
    }

    // 显示加入面板的方法，在 ButtonLogic 点击“Join”时调用
    public void ShowJoinPanel()
    {
        Debug.Log("Showing join panel");
        if (joinPanel != null)
        {
            PlayUIClickSound();
            joinPanel.SetActive(true);

            if (joinIP != null)
            {
                // 预填本地 IP（可选），并聚焦输入框
                joinIP.text = GameManager.Instance.localIP;
                joinIP.Select();
                joinIP.ActivateInputField();
            }
        }
    }
}
