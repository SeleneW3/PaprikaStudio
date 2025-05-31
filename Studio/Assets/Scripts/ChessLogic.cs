using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System;  // 添加这行用于事件系统

public class ChessLogic : NetworkBehaviour
{
    // 添加静态事件用于通知动画完成
    public static event Action OnBothChessAnimationComplete;
    private static int completedAnimationCount = 0;

    private NetworkObject networkObject;
    private bool isInitialized = false;

    // 将 NetworkVariable 的初始化移到 OnNetworkSpawn 中
    private NetworkVariable<bool> hasCompletedAnimation;

    public Transform stateBeingClicked;

    public Vector3 clickPointPos;
    public Quaternion clickPointRot;

    public float originToBeingClickedDuration = 0.5f;

    public float beingClickedToClickPointDuration = 1.15f;  //移动速度减慢

    public float heightOffset = 1f;

    public int state = 0;

    public float timer = 0f;

    public float moveSpeed = 2f;

    private Vector3 originalPos;
    private Quaternion originalRot;

    public bool backToOriginal = false;
    public bool isOnGround = false;

    public enum Belonging
    {
        Player1,
        Player2
    }

    public Belonging belonging;

    private void Awake()
    {
        networkObject = GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError($"[ChessLogic] NetworkObject component missing on {gameObject.name}!");
        }
        
        // 在 Awake 中初始化 NetworkVariable
        hasCompletedAnimation = new NetworkVariable<bool>(false);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"[ChessLogic] OnNetworkSpawn called for {gameObject.name}. IsServer: {IsServer}, IsClient: {IsClient}, IsSpawned: {IsSpawned}");
        
        if (!IsSpawned)
        {
            Debug.LogError($"[ChessLogic] {gameObject.name} is not properly spawned!");
            return;
        }
        
        InitializeChess();
    }

    private void InitializeChess()
    {
        try
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError($"[ChessLogic] GameManager.Instance is null during initialization of {gameObject.name}");
                return;
            }

            originalPos = transform.position;
            originalRot = transform.rotation;

            if (GameManager.Instance.chessComponents == null)
            {
                Debug.LogError($"[ChessLogic] GameManager.Instance.chessComponents is null during initialization of {gameObject.name}");
                return;
            }

            int index = belonging == Belonging.Player1 ? 0 : 1;
            if (index < GameManager.Instance.chessComponents.Count)
            {
                GameManager.Instance.chessComponents[index] = this;
                Debug.Log($"[ChessLogic] Successfully registered {gameObject.name} as {belonging}'s chess");
            }
            else
            {
                Debug.LogError($"[ChessLogic] Invalid index {index} for chess registration");
            }

            isInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ChessLogic] Error during initialization of {gameObject.name}: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }

    void Start()
    {
        if (!isInitialized)
        {
            InitializeChess();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(state == 1)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer/originToBeingClickedDuration);
            transform.position = Vector3.Lerp(originalPos, stateBeingClicked.position, t);
            transform.rotation = Quaternion.Lerp(originalRot, stateBeingClicked.rotation, t);

            if(t >= 1f)
            {
                transform.position = stateBeingClicked.position;
                transform.rotation = stateBeingClicked.rotation;
                state = 2;
                timer = 0f;
            }
        }
        else if(state == 3)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer/beingClickedToClickPointDuration);
            Vector3 linearPos = Vector3.Lerp(stateBeingClicked.position, clickPointPos, t);
            float verticalOffset = Mathf.Sin(t * Mathf.PI) * heightOffset;
            transform.position = linearPos + Vector3.up * verticalOffset;
            transform.rotation = Quaternion.Slerp(stateBeingClicked.rotation, clickPointRot, t);
            if(t >= 1f)
            {
                state = 4;
                timer = 0f;
                isOnGround = true;
                
                // 播放棋子放置音效
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySFX("ChessDown");
                }

                // 通知一个棋子动画完成
                NotifyAnimationComplete();
            }
        }
        else if(state == 4 && backToOriginal)
        {
            ReturnToFloatPoint();
        }
        else if(state == 5)
        {
            ResetBack();
        }
    }

    private void OnMouseDown()
    {
        // 先检查网络管理器
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[ChessLogic] NetworkManager.Singleton is null in OnMouseDown!");
            return;
        }

        // 检查是否已经生成
        if (!IsSpawned)
        {
            Debug.LogError($"[ChessLogic] Attempting to handle OnMouseDown but {gameObject.name} is not spawned!");
            return;
        }

        // 检查是否初始化
        if (!isInitialized)
        {
            Debug.LogError($"[ChessLogic] Attempting to handle OnMouseDown but {gameObject.name} is not initialized!");
            return;
        }

        // 检查 GameManager
        if (GameManager.Instance == null)
        {
            Debug.LogError("[ChessLogic] GameManager.Instance is null in OnMouseDown!");
            return;
        }

        try
        {
            // 播放音效
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX("ChessUp");
            }

            // 根据玩家ID处理点击
            if (NetworkManager.Singleton.LocalClientId == 0 && belonging == Belonging.Player1)
            {
                Debug.Log("[ChessLogic] Player1 attempting to change state via ServerRpc");
                GameManager.Instance.ChangeChess1StateServerRpc();
            }
            else if (NetworkManager.Singleton.LocalClientId == 1 && belonging == Belonging.Player2)
            {
                Debug.Log("[ChessLogic] Player2 attempting to change state via ServerRpc");
                GameManager.Instance.ChangeChess2StateServerRpc();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ChessLogic] Error in OnMouseDown: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }

    public void Move()
    {

         state = 3; 
         timer = 0f;

    }

    public void ReturnToFloatPoint()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer/originToBeingClickedDuration);
        transform.position = Vector3.Lerp(transform.position, stateBeingClicked.position, t);
        transform.rotation = Quaternion.Lerp(transform.rotation, stateBeingClicked.rotation, t);
        if(t >= 1f)
        {
            state = 5;
            timer = 0f;
        }
    }

    public void ResetBack()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer/originToBeingClickedDuration);
        transform.position = Vector3.Lerp(transform.position,originalPos, t);
        transform.rotation = Quaternion.Lerp(transform.rotation, originalRot, t);
        if(t >= 1f)
        {
            state = 0;
            timer = 0f;
            backToOriginal = false;
            
        }
    }

    private void OnEnable()
    {
        completedAnimationCount = 0;
    }

    private void OnDisable()
    {
        completedAnimationCount = 0;
    }

    private void NotifyAnimationComplete()
    {
        if (!IsSpawned || !isInitialized)
        {
            Debug.LogWarning($"[ChessLogic] NotifyAnimationComplete called but object not ready. IsSpawned: {IsSpawned}, IsInitialized: {isInitialized}");
            return;
        }

        // 如果是客户端，请求服务器执行
        if (!IsServer)
        {
            NotifyAnimationCompleteServerRpc();
            return;
        }

        try
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[ChessLogic] GameManager.Instance is null in NotifyAnimationComplete");
                return;
            }

            hasCompletedAnimation.Value = true;
            completedAnimationCount++;
            Debug.Log($"[ChessLogic] Animation completed for {gameObject.name}. Total completed: {completedAnimationCount}");

            if (completedAnimationCount >= 2)
            {
                Debug.Log("[ChessLogic] Both chess pieces completed animation, invoking event");
                completedAnimationCount = 0;
                NotifyBothAnimationsCompleteClientRpc();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ChessLogic] Error in NotifyAnimationComplete: {e.Message}\nStack trace: {e.StackTrace}");
            completedAnimationCount = 0;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotifyAnimationCompleteServerRpc()
    {
        NotifyAnimationComplete();
    }

    [ClientRpc]
    private void NotifyBothAnimationsCompleteClientRpc()
    {
        try 
        {
            if (OnBothChessAnimationComplete != null)
            {
                OnBothChessAnimationComplete.Invoke();
            }
            else
            {
                Debug.LogWarning("[ChessLogic] OnBothChessAnimationComplete is null");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ChessLogic] Error in NotifyBothAnimationsCompleteClientRpc: {e.Message}");
        }
    }
}
