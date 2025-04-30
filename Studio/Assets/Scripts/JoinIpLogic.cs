using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class JoinIpLogic : MonoBehaviour
{
    public TMP_InputField joinIP;
    void Start()
    {
        joinIP.text = "Join IP:" + GameManager.Instance.localIP;

        joinIP.onEndEdit.AddListener(OnJoinIPEndEdit);
    }

    private void OnJoinIPEndEdit(string ip)
    {
        joinIP.text = "Join IP:" + ip;
        GameManager.Instance.joinIP = ip;

    }


}
