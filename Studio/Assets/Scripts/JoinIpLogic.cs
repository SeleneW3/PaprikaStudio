using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class JoinIpLogic : MonoBehaviour
{
    public TMP_InputField joinIP;
    void Start()
    {
        joinIP.text = GameManager.Instance.localIP;
        joinIP.onEndEdit.AddListener(OnJoinIPEndEdit);
    }

    private void OnJoinIPEndEdit(string ip)
    {
        // 移除可能存在的"Join IP:"前缀
        ip = ip.Replace("Join IP:", "").Trim();
        
        // 存储纯IP地址
        GameManager.Instance.joinIP = ip;
        Debug.Log($"Join IP set to: {ip}");
    }
}
