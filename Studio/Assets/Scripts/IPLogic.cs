using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using TMPro;
using UnityEngine;

public class IPLogic : MonoBehaviour
{
    
    public TextMeshProUGUI ipText;
    void Start()
    {
        ipText = GetComponent<TextMeshProUGUI>();
        string localIp = GetLocalIPAddress();
        GameManager.Instance.localIP = localIp;
        ipText.text = "IP: " + GameManager.Instance.localIP;
    }


    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        GameManager.Instance.localIP = host.AddressList[0].ToString();
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("ϵͳ��û���ҵ�IPv4��ַ��������������");
    }
}
