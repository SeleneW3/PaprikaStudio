using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class ButtonLogic : MonoBehaviour
{
    public enum ButtonType
    {
        Create,
        Join,
        Settings,
        Exit
    }

    public ButtonType buttonType;
    public JoinIpLogic joinIpLogic;  // 新增：JoinIpLogic引用
    
    [Header("Sound Effects")]
    [SerializeField] private string buttonClickSound = "CardClick"; // 按钮点击音效

    private void Start()
    {
        if (joinIpLogic != null)
        {
            joinIpLogic.Initialize(this);
        }
    }

    private void OnMouseDown()
    {
        // 如果点击在UI上，不处理按钮点击
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // 播放点击音效
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(buttonClickSound);
        }

        Debug.Log($"Button clicked: {buttonType}");
        if(buttonType == ButtonType.Create)
        {
            CreateClick();
        }
        else if(buttonType == ButtonType.Join)
        {
            Debug.Log("Join button clicked, attempting to show panel...");
            if (joinIpLogic != null)
            {
                joinIpLogic.ShowJoinPanel();
            }
            else
            {
                Debug.LogError("joinIpLogic reference is null!");
            }
        }
        else if(buttonType == ButtonType.Settings)
        {
            SettingsClick();
        }
        else if(buttonType == ButtonType.Exit)
        {
            ExitClick();
        }
    }

    private async void CreateClick()
    {
        string localIp = GameManager.Instance.localIP;
        GameManager.Instance.localIP = localIp;

        // 配置 Transport
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as Unity.Netcode.Transports.UTP.UnityTransport;
        transport.SetConnectionData(localIp, 7777);

        // 启动 Host
        bool started = NetworkManager.Singleton.StartHost();
        if (!started)
        {
            Debug.LogError("StartHost failed. Cannot create lobby.");
            return;
        }

        // 等待一帧让底层 Transport 完全启动
        await System.Threading.Tasks.Task.Yield();

        // 确认自己是服务器后再切换场景
        if (NetworkManager.Singleton.IsServer)
        {
            GameManager.Instance.LoadScene("Lobby");
        }
        else
        {
            Debug.LogError("NotServerException: Only server can load scenes.");
        }
    }

    // 新增：将原来的JoinClick改名并设为public，供JoinIpLogic调用
    public void ExecuteJoin()
    {
        try
        {
            string ip = GameManager.Instance.joinIP;
            Debug.Log($"Attempting to join game at IP: {ip}");

            if (string.IsNullOrEmpty(ip))
            {
                Debug.LogError("IP address is empty!");
                return;
            }

            // 如果已经连接，先关闭现有连接
            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
            {
                Debug.Log("Shutting down existing connection...");
                NetworkManager.Singleton.Shutdown();
            }

            var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
            if (transport == null)
            {
                Debug.LogError("Failed to get UnityTransport!");
                return;
            }

            transport.SetConnectionData(ip, 7777);
            Debug.Log($"Transport configured with IP: {ip}, Port: 7777");

            NetworkManager.Singleton.StartClient();
            Debug.Log("StartClient called");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error joining game: {e.Message}");
        }
    }

    private void SettingsClick()
    {
        
    }

    private void ExitClick()
    {
        Application.Quit();
    }
}
