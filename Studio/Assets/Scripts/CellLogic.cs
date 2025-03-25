using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CellLogic : MonoBehaviour
{
    [SerializeField]
    TMP_Text playerName;
    TMP_Text stateText;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Initial(ulong playerID,bool isReady)
    {
        playerName = transform.Find("Name").GetComponent<TMP_Text>();
        stateText = transform.Find("State").GetComponent<TMP_Text>();
        playerName.text = "Player" + playerID.ToString();
        stateText.text = isReady ? "Ready" : "Not Ready";
    }
}
