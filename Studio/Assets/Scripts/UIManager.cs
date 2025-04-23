using UnityEngine;
using TMPro;
using UnityEngine.UI;  // 用于 CanvasScaler 等 UI 组件
using Unity.Netcode;

public class UIManager : MonoBehaviour
{
    [Header("Score UI")]
    public TextMeshProUGUI player1ScoreText;
    public TextMeshProUGUI player2ScoreText;
    
    [Header("Score Position References")]
    public Transform player1ScoreAnchor; // 天平上的第一个空子物体
    public Transform player2ScoreAnchor; // 天平上的第二个空子物体

    [Header("Bullets UI")]
    public TextMeshProUGUI player1BulletsText; // 显示玩家1剩余子弹的文本
    public TextMeshProUGUI player2BulletsText; // 显示玩家2剩余子弹的文本
    public Transform player1BulletsAnchor; // 玩家1子弹数显示锚点
    public Transform player2BulletsAnchor; // 玩家2子弹数显示锚点

    [Header("Round UI")]
    public TextMeshProUGUI roundText; // 回合显示UI
    public Transform roundAnchor; // 回合显示锚点

    [Header("Screen Space UI")]
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI player1DebugText;
    public TextMeshProUGUI player2DebugText;

    [Header("Movement Settings")]
    [Range(1f, 50f)]
    public float smoothSpeed = 15f; // 更高的平滑速度，减少滞后感
    public Vector2 scoreOffset = new Vector2(0, 50); // 位置偏移
    public Vector2 bulletsOffset = new Vector2(0, 0); // 子弹数显示的位置偏移
    public Vector2 roundOffset = new Vector2(0, 20); // 回合显示的位置偏移
    
    // 最小移动阈值，低于此值就直接设置到目标位置
    public float snapThreshold = 2.0f; 
    private Canvas parentCanvas;

    // 引用GunController
    private GunController gun1;
    private GunController gun2;

    void Start()
    {
        // 获取父级Canvas
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogError("UIManager: No parent Canvas found!");
            return;
        }
        
        // 设置高 sorting order 确保UI在其他物体前面
        parentCanvas.sortingOrder = 100;
        
        // 初始化时隐藏游戏结束文本
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(false);
        }
        
        // 获取GunController引用
        GameObject gun1Obj = GameObject.Find("Gun1");
        GameObject gun2Obj = GameObject.Find("Gun2");
        
        if (gun1Obj) gun1 = gun1Obj.GetComponent<GunController>();
        if (gun2Obj) gun2 = gun2Obj.GetComponent<GunController>();
        
        if (gun1 == null || gun2 == null)
        {
            Debug.LogWarning("无法找到枪支控制器引用");
        }
        
        // 检查子弹锚点是否存在
        if (player1BulletsAnchor == null)
        {
            Debug.LogWarning("Player1BulletsAnchor未设置，将使用player1ScoreAnchor作为备用");
            player1BulletsAnchor = player1ScoreAnchor;
        }
        
        if (player2BulletsAnchor == null)
        {
            Debug.LogWarning("Player2BulletsAnchor未设置，将使用player2ScoreAnchor作为备用");
            player2BulletsAnchor = player2ScoreAnchor;
        }
        
        // 检查回合锚点是否存在
        if (roundAnchor == null)
        {
            Debug.LogWarning("RoundAnchor未设置，回合显示可能不会正确定位");
        }
        
        // 强制UI可见性
        ForceUIVisibility();
    }

    void Update()
    {
        // 直接调用统一的位置更新方法
        UpdateUIPositions();
        
        if (GameManager.Instance != null)
        {
            // 更新分数
            UpdateScoreText();
            
            // 更新子弹数
            UpdateBulletsText();
            
            // 更新调试信息
            if (player1DebugText != null && player2DebugText != null)
            {
                player1DebugText.text = GameManager.Instance.playerComponents[0].debugInfo.Value.ToString();
                player2DebugText.text = GameManager.Instance.playerComponents[1].debugInfo.Value.ToString();
            }
        }
    }

    void UpdateScoreText()
    {
        if (player1ScoreText != null && player2ScoreText != null && 
            GameManager.Instance != null && GameManager.Instance.playerComponents.Count >= 2)
        {
            player1ScoreText.text = $"Player 1: {GameManager.Instance.playerComponents[0].point.Value}";
            player2ScoreText.text = $"Player 2: {GameManager.Instance.playerComponents[1].point.Value}";
        }
    }
    
    void UpdateBulletsText()
    {
        // 确保我们有所有需要的组件
        if (player1BulletsText == null || player2BulletsText == null || gun1 == null || gun2 == null)
            return;
            
        // 计算已消耗的机会数（总共6次机会）
        int gun1UsedChances = 6 - gun1.remainingChances.Value; // 已消耗机会数 = 总机会 - 剩余机会
        int gun2UsedChances = 6 - gun2.remainingChances.Value;
        
        // 显示已消耗机会数/总机会数
        if (NetworkManager.Singleton != null)
        {
            // 判断当前是哪个玩家视角
            ulong localClientId = NetworkManager.Singleton.LocalClientId;
            
            if (localClientId == 0) // 玩家1视角
            {
                player1BulletsText.text = $"{gun1UsedChances}/6"; // 显示格式：已消耗/总数
                player2BulletsText.text = $"{gun1UsedChances}/6"; // 玩家1的子弹数显示在玩家2头顶
            }
            else // 玩家2视角
            {
                player1BulletsText.text = $"{gun2UsedChances}/6"; // 玩家2的子弹数显示在玩家1头顶
                player2BulletsText.text = $"{gun2UsedChances}/6";
            }
        }
        else
        {
            // 单机测试模式
            player1BulletsText.text = $"{gun1UsedChances}/6";
            player2BulletsText.text = $"{gun2UsedChances}/6";
        }
    }

    // 使用统一的位置更新方法，无论Canvas模式如何
    void UpdateUIPositions()
    {
        if (player1ScoreAnchor == null || player2ScoreAnchor == null || Camera.main == null ||
            player1ScoreText == null || player2ScoreText == null || parentCanvas == null)
        {
            return;
        }

        // 计算天平锚点的屏幕坐标
        Vector3 player1ScreenPos = Camera.main.WorldToScreenPoint(player1ScoreAnchor.position);
        Vector3 player2ScreenPos = Camera.main.WorldToScreenPoint(player2ScoreAnchor.position);
        
        // 处理玩家1分数文本位置
        UpdateTextPosition(player1ScoreText, player1ScreenPos, scoreOffset, player1ScreenPos.z < 0);
        
        // 处理玩家2分数文本位置
        UpdateTextPosition(player2ScoreText, player2ScreenPos, scoreOffset, player2ScreenPos.z < 0);
        
        // 更新子弹数文本位置（使用新的锚点）
        if (player1BulletsText != null && player2BulletsText != null)
        {
            // 使用专门的子弹锚点计算屏幕位置
            Vector3 bullets1ScreenPos = Camera.main.WorldToScreenPoint(player1BulletsAnchor.position);
            Vector3 bullets2ScreenPos = Camera.main.WorldToScreenPoint(player2BulletsAnchor.position);
            
            UpdateTextPosition(player1BulletsText, bullets1ScreenPos, bulletsOffset, bullets1ScreenPos.z < 0);
            UpdateTextPosition(player2BulletsText, bullets2ScreenPos, bulletsOffset, bullets2ScreenPos.z < 0);
        }
        
        // 更新回合显示文本位置
        if (roundText != null && roundAnchor != null)
        {
            Vector3 roundScreenPos = Camera.main.WorldToScreenPoint(roundAnchor.position);
            UpdateTextPosition(roundText, roundScreenPos, roundOffset, roundScreenPos.z < 0);
        }
    }
    
    // 提取更新UI文本位置的通用方法
    void UpdateTextPosition(TextMeshProUGUI textElement, Vector3 screenPos, Vector2 offset, bool isBehindCamera)
    {
        if (textElement == null) return;
        
        if (isBehindCamera)
        {
            textElement.gameObject.SetActive(false);
        }
        else
        {
            textElement.gameObject.SetActive(true);
            
            // 创建目标屏幕位置（包括偏移）
            Vector3 targetScreenPos = new Vector3(
                screenPos.x + offset.x, 
                screenPos.y + offset.y,
                0);
            
            // 转换为世界坐标（适用于Screen Space - Camera）
            if (parentCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                // 计算从屏幕到世界的射线
                Ray ray = parentCanvas.worldCamera.ScreenPointToRay(targetScreenPos);
                
                // 计算射线与Canvas平面的交点
                float distance = parentCanvas.planeDistance;
                Vector3 worldPos = ray.origin + ray.direction * distance;
                
                // 将世界坐标应用到UI元素
                textElement.transform.position = Vector3.Lerp(
                    textElement.transform.position,
                    worldPos,
                    Time.deltaTime * smoothSpeed
                );
            }
            else // Screen Space - Overlay
            {
                // 直接使用屏幕坐标
                textElement.transform.position = Vector3.Lerp(
                    textElement.transform.position,
                    targetScreenPos,
                    Time.deltaTime * smoothSpeed
                );
            }
        }
    }

    // 初始化位置方法也做相应调整
    void ForceUIVisibility()
    {
        if (parentCanvas == null)
        {
            Debug.LogError("Cannot force UI visibility: parentCanvas is null");
            return;
        }
        
        if (player1ScoreText != null)
        {
            player1ScoreText.gameObject.SetActive(true);
            player1ScoreText.text = "Player 1: 0";
            player1ScoreText.color = Color.red;
            
            if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                player1ScoreText.transform.position = new Vector3(Screen.width / 2, Screen.height / 2 + 100, 0);
            }
            else // Screen Space - Camera
            {
                // 将屏幕中心转换为世界坐标
                Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2 + 100, 0);
                Ray ray = parentCanvas.worldCamera.ScreenPointToRay(screenCenter);
                float distance = parentCanvas.planeDistance;
                Vector3 worldPos = ray.origin + ray.direction * distance;
                
                player1ScoreText.transform.position = worldPos;
            }
        }
        
        // 同样初始化子弹数文本
        if (player1BulletsText != null)
        {
            player1BulletsText.gameObject.SetActive(true);
            player1BulletsText.text = "0/6";
            player1BulletsText.color = Color.yellow;
            
            if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                player1BulletsText.transform.position = new Vector3(Screen.width / 2, Screen.height / 2 + 120, 0);
            }
            else // Screen Space - Camera
            {
                Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2 + 120, 0);
                Ray ray = parentCanvas.worldCamera.ScreenPointToRay(screenCenter);
                float distance = parentCanvas.planeDistance;
                Vector3 worldPos = ray.origin + ray.direction * distance;
                
                player1BulletsText.transform.position = worldPos;
            }
        }
        
        // 初始化回合显示文本
        if (roundText != null)
        {
            roundText.gameObject.SetActive(true);
            roundText.text = "ROUND 1/5";
            roundText.color = Color.white;
            
            if (roundAnchor != null)
            {
                // 如果存在锚点，会在Update中更新位置
            }
            else if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // 默认位置（如果没有锚点）
                roundText.transform.position = new Vector3(Screen.width / 2, Screen.height - 50, 0);
            }
            else // Screen Space - Camera
            {
                Vector3 screenPos = new Vector3(Screen.width / 2, Screen.height - 50, 0);
                Ray ray = parentCanvas.worldCamera.ScreenPointToRay(screenPos);
                float distance = parentCanvas.planeDistance;
                Vector3 worldPos = ray.origin + ray.direction * distance;
                
                roundText.transform.position = worldPos;
            }
        }
    }

    // 显示游戏结束
    public void ShowGameOver(string reason = "")
    {
        if (gameOverText != null)
        {
            gameOverText.text = "GAME OVER" + (string.IsNullOrEmpty(reason) ? "" : "\n" + reason);
            gameOverText.gameObject.SetActive(true);
        }
    }

    // 隐藏游戏结束
    public void HideGameOver()
    {
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(false);
        }
    }

    // 提供更新调试信息的接口
    public void UpdateDebugInfo(string player1Debug, string player2Debug)
    {
        if (player1DebugText != null) player1DebugText.text = player1Debug;
        if (player2DebugText != null) player2DebugText.text = player2Debug;
    }

    // 用于清空调试信息
    public void ClearDebugInfo()
    {
        if (player1DebugText != null) player1DebugText.text = "";
        if (player2DebugText != null) player2DebugText.text = "";
    }

    // 添加更新回合文本的方法
    public void UpdateRoundText(int currentRound, int totalRounds = 5)
    {
        if (roundText != null)
        {
            roundText.text = $"ROUND {currentRound}/{totalRounds}";
        }
    }
} 