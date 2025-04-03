using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

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
        string localIp = GetLocalIPAddress();
        GameManager.Instance.ip = localIp;

        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        transport.SetConnectionData(localIp, 7777);

        NetworkManager.Singleton.StartHost();
        GameManager.Instance.LoadScene("Lobby");
    }

    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("系统中没有找到IPv4地址的网络适配器！");
    }

    private void JoinClick()
    {
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        transport.SetConnectionData(GameManager.Instance.ip, 7777);

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
