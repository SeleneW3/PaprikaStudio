using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GunController : NetworkBehaviour
{
    private Animator gunAnimator;  // 该枪的 Animator
    private NetworkObject networkObject;  // 引用 NetworkObject 组件
    private RoundManager roundManager;  // 添加RoundManager引用

    public NetworkVariable<int> remainingChances = new NetworkVariable<int>(6);  // 剩余的机会次数
    public NetworkVariable<int> realBulletPosition = new NetworkVariable<int>(0); // 真子弹的位置，初始为 0（无效值）
    public NetworkVariable<bool> gameEnded = new NetworkVariable<bool>(false);   // 游戏是否结束
    
    private Vector3 originalPosition;  // 记录原始位置
    private Quaternion originalRotation; // 记录原始旋转
    private bool isHovered = false;    // 记录是否正在悬停
    private float hoverHeight = 0.1f;  // 抬起高度
    public bool isAnimating = false;  // 记录是否正在播放动画

    [Header("UI Anchor")]
    public Transform gunUIAnchor;  // 用于放置UI按钮的锚点
    
    // Gun action events
    public delegate void GunActionHandler(GunController gun);
    public event GunActionHandler OnGunThrown;  // 扔枪事件
    public event GunActionHandler OnGunSpun;    // 甩枪事件

    void Awake()
    {
        // 在 Awake 中获取 NetworkObject，确保最早获取到组件
        networkObject = GetComponent<NetworkObject>();
        //Debug.Log($"Awake - NetworkObject component {(networkObject != null ? "found" : "not found")}");

        // 在 Awake 中记录初始位置和旋转
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        //Debug.Log($"Original position set for {gameObject.name}: {originalPosition}");
    }

    void Start()
    {
        // 重新获取一次，以防在运行时添加
        if (networkObject == null)
        {
            networkObject = GetComponent<NetworkObject>();
        }

        if (networkObject == null)
        {
            //Debug.LogError("NetworkObject component missing on GunController!");
            return;
        }

        // 检查碰撞器
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            //Debug.LogError($"[{gameObject.name}] Missing Collider component on parent object - required for mouse interactions!");
            // 自动添加Box Collider
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            // 获取所有子物体的渲染器
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                // 计算所有子物体渲染器的包围盒
                Bounds bounds = new Bounds(renderers[0].bounds.center, Vector3.zero);
                foreach (Renderer renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                // 设置Box Collider的大小和中心点
                boxCollider.center = transform.InverseTransformPoint(bounds.center);
                boxCollider.size = bounds.size;
                //Debug.Log($"[{gameObject.name}] Added Box Collider with size: {boxCollider.size} and center: {boxCollider.center}");
            }
        }

        // 检查子物体的渲染器
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
        if (childRenderers.Length == 0)
        {
            //Debug.LogError($"[{gameObject.name}] No Renderer found in children - required for mouse interactions!");
        }
        else
        {
            //Debug.Log($"[{gameObject.name}] Found {childRenderers.Length} renderers in children");
            foreach (Renderer renderer in childRenderers)
            {
                if (!renderer.enabled)
                {
                    //Debug.LogWarning($"[{gameObject.name}] Renderer '{renderer.name}' is disabled!");
                }
                if (renderer.sharedMaterial == null)
                {
                    //Debug.LogWarning($"[{gameObject.name}] Renderer '{renderer.name}' has no material assigned!");
                }
            }
        }

        //Debug.Log($"Start - NetworkObject state - IsSpawned: {networkObject.IsSpawned}, IsLocalPlayer: {networkObject.IsLocalPlayer}, IsOwner: {networkObject.IsOwner}");
        
        // 如果在服务器上且还没有生成，则生成对象
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && !networkObject.IsSpawned)
        {
            //Debug.Log("Start - Spawning NetworkObject for GunController");
            networkObject.Spawn();
        }

        // 获取RoundManager引用
        roundManager = FindObjectOfType<RoundManager>();
        if (roundManager == null)
        {
            //Debug.LogError("RoundManager not found!");
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        //Debug.Log($"OnNetworkSpawn - IsServer: {IsServer}, NetworkManager.Singleton.IsServer: {NetworkManager.Singleton.IsServer}, IsSpawned: {IsSpawned}, IsClient: {IsClient}");
        
        // 获取枪的 Animator 组件
        gunAnimator = GetComponent<Animator>();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) // 检查 NetworkManager 是否存在
        {
            //Debug.Log("Server side - About to call InitializeBulletChancesServerRpc");
            try 
            {
                InitializeBulletChancesServerRpc();
                Debug.Log("Successfully called InitializeBulletChancesServerRpc");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to call InitializeBulletChancesServerRpc: {e.Message}");
            }
        }
        else
        {
            //Debug.Log($"Client side or NetworkManager not ready - NetworkManager.Singleton is {(NetworkManager.Singleton == null ? "null" : "not null")}");
        }
    }

    // ServerRpc 用于初始化 "真子弹" 的位置
    [ServerRpc(RequireOwnership = false)]
    void InitializeBulletChancesServerRpc()
    {
        Debug.Log($"InitializeBulletChancesServerRpc called on {(IsServer ? "Server" : "Client")}");
        // 在游戏开始时，随机生成一个"真子弹"的位置 (1 到 6)
        realBulletPosition.Value = Random.Range(1, 7);  // 返回 1 到 6 之间的整数
        Debug.Log($"Server initialized with a real bullet at position: {realBulletPosition.Value}");
    }

    public void FireGun()
    {
        if (gameEnded.Value)
        {
            Debug.Log("The game has already ended.");
            return;
        }

        if (remainingChances.Value <= 0)
        {
            Debug.Log("No remaining chances.");
            return;
        }

        // 当前位置是 7 - remainingChances (从1开始，因为remainingChances初始值是6)
        int currentPosition = 7 - remainingChances.Value;
        remainingChances.Value--;  // 使用 .Value 访问 NetworkVariable 的值
        Debug.Log($"Current position: {currentPosition}, Remaining chances: {remainingChances.Value}, Real Bullet is at position {realBulletPosition.Value}");

        // 使用ClientRpc来同步动画
        PlayFireAnimationClientRpc();
        
        // 延迟播放开枪音效
        Invoke("PlayGunFireSound", 2f);

        if (currentPosition == realBulletPosition.Value) // 检查当前位置是否是真子弹位置
        {
            Debug.Log("Bang! A real bullet! The enemy is dead.");
            gameEnded.Value = true;  // 设置 gameEnded 的值
            
            // 触发真子弹震动效果（在所有客户端）
            TriggerRealBulletShakeClientRpc();
            
            // 延迟播放击中音效
            Invoke("PlayBulletHitSound", 2f);
        }
        else
        {
            // 延迟播放未击中音效
            Invoke("PlayEmptyShotSound", 2f);
            
            if (remainingChances.Value == 0)
            {
                InitializeBulletChancesServerRpc(); // 使用 ServerRpc 来初始化真子弹的位置
                remainingChances.Value = 6;  // 重置剩余次数
            }
        }
    }
    
    // 播放开枪音效
    private void PlayGunFireSound()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("GunFire");
        }
    }
    
    // 播放击中音效
    private void PlayBulletHitSound()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("BulletHit");
        }
    }
    
    // 播放未击中音效
    private void PlayEmptyShotSound()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("EmptyShot");
        }
    }

    [ClientRpc]
    void PlayFireAnimationClientRpc()
    {
        Debug.Log("PlayFireAnimationClientRpc called on " + (IsServer ? "Server" : "Client"));
        if (gunAnimator != null)
        {
            isAnimating = true;
            gunAnimator.SetTrigger("Grab");
        }
        else
        {
            Debug.LogError("gunAnimator is null on " + (IsServer ? "Server" : "Client"));
        }
    }
    
    public void ResetGun()
    {
        Debug.Log("ResetGun method called");
        if (NetworkManager.Singleton.IsServer)  // 修改这里也使用 NetworkManager.Singleton.IsServer
        {
            remainingChances.Value = 6;
            gameEnded.Value = false;
            InitializeBulletChancesServerRpc(); // 在重置时重新初始化
            
            Debug.Log("About to call PlayResetGunSoundClientRpc");
            // 播放重置枪音效
            PlayResetGunSoundClientRpc();
        }
    }

    [ClientRpc]
    void PlayResetGunSoundClientRpc()
    {
        Debug.Log("PlayResetGunSoundClientRpc called");
        // 延迟播放重置枪音效
        Invoke("PlayGunResetSound", 2f);
    }

    // 播放重置枪音效
    private void PlayGunResetSound()
    {
        Debug.Log("PlayGunResetSound called");
        if (SoundManager.Instance != null)
        {
            Debug.Log("Attempting to play GunReset sound");
            SoundManager.Instance.PlaySFX("GunReset");
        }
        else
        {
            Debug.LogError("SoundManager.Instance is null");
        }
    }

    // 添加一个方法来手动生成对象（如果需要的话）
    public void ManualSpawn()
    {
        if (networkObject != null && !networkObject.IsSpawned && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            //  Debug.Log("Manually spawning NetworkObject");
            networkObject.Spawn();
        }
    }


    /*void OnMouseDown()
    {
        if (!IsSpawned || roundManager == null) return;

        // 检查是否是本地玩家的枪且有开枪权限
        if (gameObject.name == "Gun1" && NetworkManager.Singleton.LocalClientId == 0 && roundManager.player1CanFire.Value)
        {
            FireGunServerRpc();
        }
        else if (gameObject.name == "Gun2" && NetworkManager.Singleton.LocalClientId == 1 && roundManager.player2CanFire.Value)
        {
            FireGunServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void FireGunServerRpc()
    {
        // 再次检查权限（服务器端验证）
        if ((gameObject.name == "Gun1" && roundManager.player1CanFire.Value) ||
            (gameObject.name == "Gun2" && roundManager.player2CanFire.Value))
        {
            // 调用原有的开枪方法
            FireGun();
            
            // 移除开枪权限
            if (gameObject.name == "Gun1")
            {
                roundManager.player1CanFire.Value = false;
            }
            else if (gameObject.name == "Gun2")
            {
                roundManager.player2CanFire.Value = false;
            }
        }
    }*/

    void OnMouseEnter()
    {
        //Debug.Log($"OnMouseEnter triggered on {gameObject.name}");
        
        // 如果是联机状态
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            if (!IsSpawned || roundManager == null) return;

            // 检查是否是本地玩家的枪
            if ((gameObject.name == "Gun1" && NetworkManager.Singleton.LocalClientId == 0) ||
                (gameObject.name == "Gun2" && NetworkManager.Singleton.LocalClientId == 1))
            {
                HoverGunServerRpc(true);
            }
        }
        // 单机状态
        else
        {
            HoverGun(true);
        }
    }

    void OnMouseExit()
    {
        //Debug.Log($"OnMouseExit triggered on {gameObject.name}");
        
        // 如果是联机状态
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            if (!IsSpawned || roundManager == null) return;

            // 检查是否是本地玩家的枪
            if ((gameObject.name == "Gun1" && NetworkManager.Singleton.LocalClientId == 0) ||
                (gameObject.name == "Gun2" && NetworkManager.Singleton.LocalClientId == 1))
            {
                HoverGunServerRpc(false);
            }
        }
        // 单机状态
        else
        {
            //Debug.Log($"Executing unhover effect on {gameObject.name} in single player mode");
            // 直接执行取消悬停效果
            HoverGun(false);
        }
    }

    // 修改HoverGun方法，移除UI显示/隐藏逻辑
    private void HoverGun(bool hover)
    {
        if (isAnimating) return; // 如果正在播放动画，不执行悬停效果

        if (hover && !isHovered)
        {
            // 抬起枪
            transform.position = originalPosition + Vector3.up * hoverHeight;
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX("GunReset");
            }
            isHovered = true;
        }
        else if (!hover && isHovered)
        {
            // 放下枪
            transform.position = originalPosition;
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX("GunReset");
            }
            isHovered = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void HoverGunServerRpc(bool hover)
    {
        // 在服务器端更新状态并同步到所有客户端
        HoverGunClientRpc(hover);
    }

    [ClientRpc]
    private void HoverGunClientRpc(bool hover)
    {
        HoverGun(hover);
    }


    // 动画接口
    public void PlayThrowAnimation()
    {
        if (gunAnimator != null)
        {
            gunAnimator.SetTrigger("Throw");
        }
    }
    
    public void PlaySpinAnimation()
    {
        if (gunAnimator != null)
        {
            isAnimating = true;
            gunAnimator.SetTrigger("Spin");
            // 根据动画实际长度设置延时
            StartCoroutine(ResetAnimatingAfterDelay(1f)); // 假设动画长度为1秒，请根据实际动画长度调整
        }
    }

    private IEnumerator ResetAnimatingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isAnimating = false;
    }

    // 添加事件触发方法
    public void TriggerThrowEvent()
    {
        OnGunThrown?.Invoke(this);
    }

    public void TriggerSpinEvent()
    {
        OnGunSpun?.Invoke(this);
    }

    // 新增：在所有客户端触发真子弹震动效果的 ClientRpc
    [ClientRpc]
    private void TriggerRealBulletShakeClientRpc()
    {
        // 直接获取当前枪的 GunShake 组件
        GunShake gunShake = GetComponent<GunShake>();
        if (gunShake != null)
        {
            gunShake.OnSuccessfulShot();
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] GunShake component not found!");
        }
        
        // 相机震动保持不变
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeCamera(0.3f, 0.4f);
        }
    }

    // 在动画开始时调用
    public void OnAnimationStart()
    {
        isAnimating = true;
        Debug.LogWarning($"[GunController] Animation started on {gameObject.name}, isAnimating set to: {isAnimating}");
    }

    // 在动画结束时调用
    public void OnAnimationEnd()
    {
        isAnimating = false;
        Debug.LogWarning($"[GunController] Animation ended on {gameObject.name}, isAnimating set to: {isAnimating}");
        // 确保枪回到正确位置和旋转
        transform.position = originalPosition;
        transform.rotation = originalRotation;
    }

}