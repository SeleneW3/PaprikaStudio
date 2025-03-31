using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelNode : MonoBehaviour
{
    public int levelIndex;        // 在Inspector中设置，表示这是第几关
    public Button levelButton;    // 关卡按钮
    public Text player1ScoreText;  // 玩家1的分数文本
    public Text player2ScoreText;  // 玩家2的分数文本

    // Start is called before the first frame update
    void Start()
    {
        levelButton.onClick.AddListener(OnLevelClick);
        UpdateNodeState();
    }

    void UpdateNodeState()
    {
        // 更新两位玩家的分数显示
        if (player1ScoreText != null)
        {
            float score1 = LevelManager.Instance.player1Scores[levelIndex];
            player1ScoreText.text = $"P1: {score1}";
        }

        if (player2ScoreText != null)
        {
            float score2 = LevelManager.Instance.player2Scores[levelIndex];
            player2ScoreText.text = $"P2: {score2}";
        }
    }

    void OnLevelClick()
    {
        // 点击时启动对应关卡
        LevelManager.Instance.StartLevel(levelIndex);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
