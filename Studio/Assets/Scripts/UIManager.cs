using UnityEngine;
using TMPro;
using UnityEngine.UI;  // 用于 CanvasScaler 等 UI 组件

public class UIManager : MonoBehaviour
{
    [Header("Score UI")]
    public TextMeshProUGUI player1ScoreText;
    public TextMeshProUGUI player2ScoreText;
    
    [Header("Score Position References")]
    public Transform player1ScoreAnchor; // 天平上的第一个空子物体
    public Transform player2ScoreAnchor; // 天平上的第二个空子物体

    [Header("Screen Space UI")]
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI player1DebugText;
    public TextMeshProUGUI player2DebugText;

    [Header("Movement Settings")]
    [Range(1f, 50f)]
    public float smoothSpeed = 15f; // 更高的平滑速度，减少滞后感
    public Vector2 scoreOffset = new Vector2(0, 50); // 位置偏移
    
    // 最小移动阈值，低于此值就直接设置到目标位置
    public float snapThreshold = 2.0f; 
    private Canvas parentCanvas;

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
        if (player1ScreenPos.z < 0)
        {
            player1ScoreText.gameObject.SetActive(false);
        }
        else
        {
            player1ScoreText.gameObject.SetActive(true);
            
            // 创建目标屏幕位置（包括偏移）
            Vector3 targetScreenPos = new Vector3(
                player1ScreenPos.x + scoreOffset.x, 
                player1ScreenPos.y + scoreOffset.y,
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
                player1ScoreText.transform.position = Vector3.Lerp(
                    player1ScoreText.transform.position,
                    worldPos,
                    Time.deltaTime * smoothSpeed
                );
            }
            else // Screen Space - Overlay
            {
                // 直接使用屏幕坐标
                player1ScoreText.transform.position = Vector3.Lerp(
                    player1ScoreText.transform.position,
                    targetScreenPos,
                    Time.deltaTime * smoothSpeed
                );
            }
        }
        
        // 处理玩家2分数文本位置（类似逻辑）
        if (player2ScreenPos.z < 0)
        {
            player2ScoreText.gameObject.SetActive(false);
        }
        else
        {
            player2ScoreText.gameObject.SetActive(true);
            
            Vector3 targetScreenPos = new Vector3(
                player2ScreenPos.x + scoreOffset.x, 
                player2ScreenPos.y + scoreOffset.y,
                0);
            
            if (parentCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                Ray ray = parentCanvas.worldCamera.ScreenPointToRay(targetScreenPos);
                float distance = parentCanvas.planeDistance;
                Vector3 worldPos = ray.origin + ray.direction * distance;
                
                player2ScoreText.transform.position = Vector3.Lerp(
                    player2ScoreText.transform.position,
                    worldPos,
                    Time.deltaTime * smoothSpeed
                );
            }
            else
            {
                player2ScoreText.transform.position = Vector3.Lerp(
                    player2ScoreText.transform.position,
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
        
        // 玩家2同理...
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
} 