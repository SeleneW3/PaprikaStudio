using UnityEngine;
using TMPro;
using UnityEngine.UI;  // 用于 CanvasScaler 等 UI 组件
using Unity.Netcode;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("Score UI")]
    public TextMeshProUGUI player1ScoreText;
    public TextMeshProUGUI player2ScoreText;
    
    [Header("Score Position References")]
    public Transform player1ScoreAnchor; // 天平上的第一个空子物体
    public Transform player2ScoreAnchor; // 天平上的第二个空子物体

    [Header("Debug Text UI")]
    public TextMeshProUGUI player1DebugText;
    public TextMeshProUGUI player2DebugText;
    public Transform player1DebugAnchor; // 玩家1 debug text 显示锚点
    public Transform player2DebugAnchor; // 玩家2 debug text 显示锚点
    public Vector2 debugTextOffset = new Vector2(0, 30); // debug text 的位置偏移

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

    [Header("Choice Status UI")]
    public TextMeshProUGUI player1ChoiceStatusText; // 玩家1选择状态
    public TextMeshProUGUI player2ChoiceStatusText; // 玩家2选择状态
    public Transform player1ChoiceStatusAnchor; // 玩家1状态显示锚点
    public Transform player2ChoiceStatusAnchor; // 玩家2状态显示锚点
    public Vector2 choiceStatusOffset = new Vector2(0, 30); // 状态显示的位置偏移

    [Header("Debug Text Animation")]
    public float debugTextAnimDuration = 0.5f;
    public float scaleAmount = 1.5f;
    public float showDuration = 1.5f;  // 显示持续时间
    public float fadeOutDuration = 0.3f;  // 淡出动画时间
    private Vector3 debugTextOriginalScale1;
    private Vector3 debugTextOriginalScale2;

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

        // 初始化时隐藏回合文本
        if (roundText != null)
        {
            roundText.gameObject.SetActive(false);
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
        
        // 初始化选择状态文本
        if (player1ChoiceStatusText != null)
        {
            player1ChoiceStatusText.gameObject.SetActive(true);
            player1ChoiceStatusText.text = "...";
            player1ChoiceStatusText.color = Color.white;
        }
        
        if (player2ChoiceStatusText != null)
        {
            player2ChoiceStatusText.gameObject.SetActive(true);
            player2ChoiceStatusText.text = "...";
            player2ChoiceStatusText.color = Color.white;
        }

        // 保存原始缩放值
        if (player1DebugText != null)
        {
            debugTextOriginalScale1 = player1DebugText.transform.localScale;
            player1DebugText.gameObject.SetActive(false);
        }
        if (player2DebugText != null)
        {
            debugTextOriginalScale2 = player2DebugText.transform.localScale;
            player2DebugText.gameObject.SetActive(false);
        }

        // 订阅棋子动画完成事件
        ChessLogic.OnBothChessAnimationComplete += OnChessAnimationComplete;
    }

    void OnDestroy()
    {
        // 取消订阅事件
        ChessLogic.OnBothChessAnimationComplete -= OnChessAnimationComplete;
    }

    private void OnChessAnimationComplete()
    {
        // 当棋子动画完成时，显示并播放debug text动画
        StartCoroutine(PlayDebugTextAnimation());
    }

    private IEnumerator PlayDebugTextAnimation()
    {
        // 确保debug text是可见的，并重置透明度
        if (player1DebugText != null)
        {
            player1DebugText.gameObject.SetActive(true);
            SetTextAlpha(player1DebugText, 1f);
        }
        if (player2DebugText != null)
        {
            player2DebugText.gameObject.SetActive(true);
            SetTextAlpha(player2DebugText, 1f);
        }

        // 缩放动画
        float elapsed = 0f;
        while (elapsed < debugTextAnimDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / debugTextAnimDuration;
            
            float scale = 1f + Mathf.Sin(progress * Mathf.PI) * (scaleAmount - 1f);
            
            if (player1DebugText != null)
                player1DebugText.transform.localScale = debugTextOriginalScale1 * scale;
            if (player2DebugText != null)
                player2DebugText.transform.localScale = debugTextOriginalScale2 * scale;

            yield return null;
        }

        // 确保回到原始大小
        if (player1DebugText != null)
            player1DebugText.transform.localScale = debugTextOriginalScale1;
        if (player2DebugText != null)
            player2DebugText.transform.localScale = debugTextOriginalScale2;

        // 等待显示时间
        yield return new WaitForSeconds(showDuration);

        // 淡出动画
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / fadeOutDuration);  // 从1渐变到0
            
            if (player1DebugText != null)
                SetTextAlpha(player1DebugText, alpha);
            if (player2DebugText != null)
                SetTextAlpha(player2DebugText, alpha);

            yield return null;
        }

        // 完全隐藏
        if (player1DebugText != null)
        {
            SetTextAlpha(player1DebugText, 0f);
            player1DebugText.gameObject.SetActive(false);
        }
        if (player2DebugText != null)
        {
            SetTextAlpha(player2DebugText, 0f);
            player2DebugText.gameObject.SetActive(false);
        }
    }

    // 辅助方法：设置文本透明度
    private void SetTextAlpha(TextMeshProUGUI text, float alpha)
    {
        Color color = text.color;
        color.a = alpha;
        text.color = color;
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

        // 更新 debug text 位置
        if (player1DebugText != null && player2DebugText != null && 
            player1DebugAnchor != null && player2DebugAnchor != null)
        {
            Vector3 debug1ScreenPos = Camera.main.WorldToScreenPoint(player1DebugAnchor.position);
            Vector3 debug2ScreenPos = Camera.main.WorldToScreenPoint(player2DebugAnchor.position);
            
            UpdateTextPosition(player1DebugText, debug1ScreenPos, debugTextOffset, debug1ScreenPos.z < 0);
            UpdateTextPosition(player2DebugText, debug2ScreenPos, debugTextOffset, debug2ScreenPos.z < 0);
        }
        
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

        // 更新选择状态文本位置
        if (player1ChoiceStatusText != null && player2ChoiceStatusText != null)
        {
            Vector3 status1ScreenPos = Camera.main.WorldToScreenPoint(player1ChoiceStatusAnchor.position);
            Vector3 status2ScreenPos = Camera.main.WorldToScreenPoint(player2ChoiceStatusAnchor.position);
            
            UpdateTextPosition(player1ChoiceStatusText, status1ScreenPos, choiceStatusOffset, status1ScreenPos.z < 0);
            UpdateTextPosition(player2ChoiceStatusText, status2ScreenPos, choiceStatusOffset, status2ScreenPos.z < 0);
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
        
        // 初始化分数文本
        if (player1ScoreText != null)
        {
            player1ScoreText.gameObject.SetActive(true);
            player1ScoreText.text = "Player 1: 0";
            player1ScoreText.color = Color.red;
        }

        // 初始化 debug text
        if (player1DebugText != null)
        {
            player1DebugText.gameObject.SetActive(true);
            player1DebugText.text = "+0";
            player1DebugText.color = Color.white;
        }
        
        if (player2DebugText != null)
        {
            player2DebugText.gameObject.SetActive(true);
            player2DebugText.text = "+0";
            player2DebugText.color = Color.white;
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
            roundText.gameObject.SetActive(false);
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
            // 存储文本内容供延迟方法使用
            gameOverText.text = "GAME OVER" + (string.IsNullOrEmpty(reason) ? "" : "\n" + reason);
            
            // 先隐藏文本
            gameOverText.gameObject.SetActive(false);
            
            // 延迟2秒显示
            Invoke("DisplayGameOverText", 2f);
        }
    }
    
    // 延迟调用的方法，显示游戏结束文本
    private void DisplayGameOverText()
    {
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
            
            // 可选：播放游戏结束音效
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX("GameOver");
            }
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

    // 网络同步版本的显示游戏结束文本
    public void NetworkShowGameOver(string reason = "")
    {
        if (NetworkManager.Singleton == null)
        {
            // 如果没有网络管理器，直接显示
            ShowGameOver(reason);
            return;
        }

        if (NetworkManager.Singleton.IsServer)
        {
            // 如果是服务器，则通知所有客户端显示游戏结束文本
            NetworkShowGameOverClientRpc(reason);
        }
    }

    [ClientRpc]
    private void NetworkShowGameOverClientRpc(string reason)
    {
        // 确保在所有客户端上显示
        ShowGameOver(reason);
        Debug.Log($"Showing game over text on client: {reason}");
    }

    // 修改UpdateDebugInfo方法
    public void UpdateDebugInfo(string player1Debug, string player2Debug)
    {
        if (player1DebugText != null)
        {
            player1DebugText.text = player1Debug;
            player1DebugText.gameObject.SetActive(false);
            SetTextAlpha(player1DebugText, 1f);  // 重置透明度
        }
        if (player2DebugText != null)
        {
            player2DebugText.text = player2Debug;
            player2DebugText.gameObject.SetActive(false);
            SetTextAlpha(player2DebugText, 1f);  // 重置透明度
        }
    }

    // 修改ClearDebugInfo方法
    public void ClearDebugInfo()
    {
        if (player1DebugText != null)
        {
            player1DebugText.text = "";
            player1DebugText.gameObject.SetActive(false);
            SetTextAlpha(player1DebugText, 1f);  // 重置透明度
        }
        if (player2DebugText != null)
        {
            player2DebugText.text = "";
            player2DebugText.gameObject.SetActive(false);
            SetTextAlpha(player2DebugText, 1f);  // 重置透明度
        }
    }

    // 添加更新回合文本的方法
    public void UpdateRoundText(int currentRound, int totalRounds = 5)
    {
        if (roundText != null)
        {
            roundText.text = $"ROUND {currentRound}/{totalRounds}";
            roundText.gameObject.SetActive(true);  // 显示文本
        }
    }

    // 更新选择状态的方法
    public void UpdateChoiceStatus(bool player1Selected, bool player2Selected)
    {
        if (player1ChoiceStatusText != null)
        {
            player1ChoiceStatusText.text = player1Selected ? "✓" : "...";
            player1ChoiceStatusText.color = player1Selected ? Color.green : Color.white;
        }
        
        if (player2ChoiceStatusText != null)
        {
            player2ChoiceStatusText.text = player2Selected ? "✓" : "...";
            player2ChoiceStatusText.color = player2Selected ? Color.green : Color.white;
        }
    }
} 