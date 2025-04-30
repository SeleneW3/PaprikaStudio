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
        string ip = GameManager.Instance.joinIP;
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        transport.SetConnectionData(ip, 7777);

        NetworkManager.Singleton.StartClient();
    }

    private void SettingsClick()
    {
        
    }

    private void ExitClick()
    {
        Application.Quit();
    }
}
