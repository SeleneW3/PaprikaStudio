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
        playerName.text = "Player" + playerInfo.id;
        stateText.text = playerInfo.isReady ? "Ready" : "Not Ready";
    }

    internal void SetReady(bool arg0)
    {
        stateText.text = arg0 ? "Ready" : "Not Ready";
    }
}
