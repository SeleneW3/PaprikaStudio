using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using TMPro;

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


    private void Start()
    {
       
    }

    
    private void OnMouseDown()
    {
        Debug.Log($"Button clicked: {buttonType}");
        if(buttonType == ButtonType.Create)
        {
            CreateClick();
        }
        else if(buttonType == ButtonType.Join)
        {
            JoinClick();
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

    private void CreateClick()
    {
        string localIp = GameManager.Instance.localIP;


        GameManager.Instance.localIP = localIp; var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        transport.SetConnectionData(localIp, 7777);

        NetworkManager.Singleton.StartHost();
        GameManager.Instance.LoadScene("Lobby");
    }

    private void JoinClick()
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
