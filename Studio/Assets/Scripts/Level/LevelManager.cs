using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public int currentLevel = 0;          // 当前进行到第几关
    public const int TOTAL_LEVELS = 3;    // 总关卡数
    
    // 分别存储两位玩家在每关的得分
    public float[] player1Scores = new float[TOTAL_LEVELS];
    public float[] player2Scores = new float[TOTAL_LEVELS];

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 开始指定关卡
    public void StartLevel(int levelIndex)
    {
        if (levelIndex < TOTAL_LEVELS)
        {
            currentLevel = levelIndex;
            // 使用GameManager加载游戏场景
            GameManager.Instance.LoadScene("Game");
        }
    }

    // 完成当前关卡
    public void CompleteCurrentLevel(float player1Score, float player2Score)
    {
        // 记录两位玩家的分数
        player1Scores[currentLevel] = player1Score;
        player2Scores[currentLevel] = player2Score;
        
        // 检查是否完成所有关卡
        if (currentLevel >= TOTAL_LEVELS - 1)
        {
            Debug.Log("所有关卡完成！");
            // 这里可以添加游戏结束的逻辑
        }

        // 返回地图场景
        GameManager.Instance.LoadScene("MapScene");
    }

    // 重置所有进度
    public void ResetProgress()
    {
        currentLevel = 0;
        for (int i = 0; i < TOTAL_LEVELS; i++)
        {
            player1Scores[i] = 0;
            player2Scores[i] = 0;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
