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
    
    [Header("Sound Effects")]
    [SerializeField] private string uiClickSound = "ButtonClick"; // UI点击音效

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
                // 播放UI点击音效
                PlayUIClickSound();
                
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
                // 播放UI点击音效
                PlayUIClickSound();
                
                joinPanel.SetActive(false);
            });
        }
    }
    
    // 播放UI点击音效
    private void PlayUIClickSound()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(uiClickSound);
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
            // 播放UI点击音效
            PlayUIClickSound();
            
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

