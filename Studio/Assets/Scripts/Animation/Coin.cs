using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class Coin : NetworkBehaviour
{ 
    public static Coin Instance { get; private set; }

    [Header("Prefab & 延迟设置")]
    [Tooltip("带有 NetworkObject 组件 的硬币预制体")]
    public GameObject coinPrefab;
    [Tooltip("每次生成硬币之间的间隔（秒）")]
    public float spawnDelay = 0.2f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 在客户端或服务器端请求生成硬币
    /// </summary>
    public void RequestSpawnCoins(Vector3 position, int amount)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            // 如果已经是服务器，直接启动协程
            StartCoroutine(SpawnCoinsCoroutine(position, amount));
        }
        else
        {
            // 否则发起 ServerRpc 让服务器来做
            SpawnCoinsServerRpc(position, amount);
            Debug.Log("send request");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnCoinsServerRpc(Vector3 position, int amount)
    {
        StartCoroutine(SpawnCoinsCoroutine(position, amount));
    }

    /// <summary>
    /// 在服务器上按顺序生成指定数量的硬币，并在每个生成之间等待 spawnDelay
    /// </summary>
    private IEnumerator SpawnCoinsCoroutine(Vector3 position, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            // 可以加一点随机偏移，让硬币不完全重叠
            Vector3 randomOffset = Random.insideUnitSphere * 0.1f;
            Vector3 spawnPos = position + randomOffset + Vector3.up * 0.1f;

            GameObject coin = Instantiate(coinPrefab, spawnPos, Random.rotation);
            NetworkObject netObj = coin.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn(destroyWithScene: true);
            }

            yield return new WaitForSeconds(spawnDelay);
        }
    }
}


/*void CalculatePointWithoutCardAndGun()
    {
        if (gameEnded)
        {
            return;
        }

        string player1Debug = "+0";
        string player2Debug = "+0";
        UIManager uiManager = FindObjectOfType<UIManager>();

        if (NetworkManager.LocalClientId == 0)
        {
            ApplyEffect();

            if (player1.choice == PlayerLogic.playerChoice.Cooperate && player2.choice == PlayerLogic.playerChoice.Cooperate)
            {
                float p1PointsBefore = player1.point.Value;
                float p2PointsBefore = player2.point.Value;

                player1.point.Value += player1.coopPoint.Value;
                player2.point.Value += player2.coopPoint.Value;

                player1Debug = $"+{player1.coopPoint.Value}";
                player2Debug = $"+{player2.coopPoint.Value}";

                // 计算增加了多少分
                int p1PointsAdded = Mathf.FloorToInt(player1.point.Value - p1PointsBefore);
                int p2PointsAdded = Mathf.FloorToInt(player2.point.Value - p2PointsBefore);

                Coin coin = FindObjectOfType<Coin>();

                // 生成硬币
                coin.RequestSpawnCoins(player1CoinRespawnPos.position, p1PointsAdded);
                Debug.Log($"Player 1 spawned {p1PointsAdded} coins at {player1ScoreAnchor.position}");
                coin.RequestSpawnCoins(player2CoinRespawnPos.position, p2PointsAdded);

                UpdateBalanceScaleServerRpc(player1.point.Value, player2.point.Value);
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cooperate && player2.choice == PlayerLogic.playerChoice.Cheat)
            {
                

                UpdateBalanceScaleServerRpc(player1.point.Value, player2.point.Value);
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cheat && player2.choice == PlayerLogic.playerChoice.Cooperate)
            {
                float p1PointsBefore = player1.point.Value;
                float p2PointsBefore = player2.point.Value;

                player1.point.Value += player1.cheatPoint.Value;
                player2.point.Value += player2.coopPoint.Value;

                player1Debug = $"+{player1.cheatPoint.Value}";
                player2Debug = $"+{player2.coopPoint.Value}";

                // 计算增加了多少分
                int p1PointsAdded = Mathf.FloorToInt(player1.point.Value - p1PointsBefore);
                int p2PointsAdded = Mathf.FloorToInt(player2.point.Value - p2PointsBefore);

                Coin coin = FindObjectOfType<Coin>();

                // 生成硬币
                coin.RequestSpawnCoins(player1ScoreAnchor.position, p1PointsAdded);
                coin.RequestSpawnCoins(player2ScoreAnchor.position, p2PointsAdded);

                UpdateBalanceScaleServerRpc(player1.point.Value, player2.point.Value);
            }
            else if (player1.choice == PlayerLogic.playerChoice.Cheat && player2.choice == PlayerLogic.playerChoice.Cheat)
            {
                float p1PointsBefore = player1.point.Value;
                float p2PointsBefore = player2.point.Value;

                player1.point.Value += 0f;
                player2.point.Value += 0f;

                player1Debug = "+0";
                player2Debug = "+0";

                // 计算增加了多少分
                int p1PointsAdded = Mathf.FloorToInt(player1.point.Value - p1PointsBefore);
                int p2PointsAdded = Mathf.FloorToInt(player2.point.Value - p2PointsBefore);

                Coin coin = FindObjectOfType<Coin>();

                // 生成硬币
                coin.RequestSpawnCoins(player1ScoreAnchor.position, p1PointsAdded);
                coin.RequestSpawnCoins(player2ScoreAnchor.position, p2PointsAdded);

                UpdateBalanceScaleServerRpc(player1.point.Value, player2.point.Value);
            }

            // 调用 UIManager 来更新调试信息
            if (uiManager != null)
            {
                uiManager.UpdateDebugInfo(player1Debug, player2Debug);
            }

            // 关键部分：在服务器端更新网络变量，让所有客户端同步显示
            if (NetworkManager.Singleton.IsServer)
            {
                GameManager.Instance.playerComponents[0].debugInfo.Value = player1Debug;
                GameManager.Instance.playerComponents[1].debugInfo.Value = player2Debug;
            }
        }
        ResetPlayersChoice();
        GameManager.Instance.ResetAllBlocksServerRpc();
        GameManager.Instance.currentGameState = GameManager.GameState.TutorReady;
        GameManager.Instance.chessComponents[0].backToOriginal = true;
        GameManager.Instance.chessComponents[1].backToOriginal = true;
        if(showCard == true)
        {
            GameManager.Instance.deck.ResetPlayerCardServerRpc();
            GameManager.Instance.deck.SetPlayerCardBoolServerRpc(1, false);
            GameManager.Instance.deck.SetPlayerCardBoolServerRpc(2, false);
            showCard = false;
        }
        
        chessIsMoved = false;


        // 添加回合结束检查
        if (!gameEnded && currentRound.Value >= totalRounds)
        {
            EndGame(uiManager);
        }
        else if (!gameEnded)
        {
            currentRound.Value++;
            if (uiManager != null)
            {
                uiManager.UpdateRoundText(currentRound.Value, totalRounds);
            }
            Debug.Log($"Round {currentRound.Value}");
        }
    }*/