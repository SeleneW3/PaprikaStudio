using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CellLogic : MonoBehaviour
{
    [SerializeField]
    TMP_Text playerName;
    TMP_Text stateText;
    public OnlinePlayerInfo playerInfo { get; private set; }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Initial(OnlinePlayerInfo playerInfo)
    {
        playerName = transform.Find("Name").GetComponent<TMP_Text>();
        stateText = transform.Find("State").GetComponent<TMP_Text>();
        
        // Convert player IDs to player numbers (0->1, 1->2)
        int playerNumber = (int)playerInfo.id + 1;
        playerName.text = "玩家" + playerNumber;
        
        stateText.text = playerInfo.isReady ? "准备完毕" : "未准备";
    }

    internal void SetReady(bool arg0)
    {
        stateText.text = arg0 ? "准备完毕" : "未准备";
    }
}
