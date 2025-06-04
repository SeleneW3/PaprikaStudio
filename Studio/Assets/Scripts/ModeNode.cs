using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModeNode : MonoBehaviour
{
    public LevelManager.Mode type;

    private void Start()
    {
    }


    private void OnMouseDown()
    {
        Debug.Log($"[ModeNode] Switching to mode: {type}");
        LevelManager.Instance.currentMode.Value = type;
        GameManager.Instance.currentGameState = GameManager.GameState.Ready;
        GameManager.Instance.LoadScene("Game");
    }

}
