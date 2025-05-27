using UnityEngine;

public class GunShake : MonoBehaviour
{
    private static GunShake instance;
    public static GunShake Instance 
    { 
        get 
        { 
            if (instance == null)
            {
                instance = FindObjectOfType<GunShake>();
                if (instance == null)
                {
                    Debug.LogError("No GunShake instance found in scene!");
                }
            }
            return instance;
        } 
    }

    [Header("Gun Shake Settings")]
    [SerializeField] private float shakeIntensity = 0.5f;     // 更大的抖动强度
    [SerializeField] private float shakeDuration = 0.8f;      // 较短的持续时间
    [SerializeField] private float decreaseFactor = 2.5f;     // 更快的衰减
    [SerializeField] private float recoilKickback = 0.3f;     // 后坐力强度
    [SerializeField] private float recoilUpward = 0.2f;       // 向上抬升强度

    [Header("Test Key")]
    //[SerializeField] private KeyCode testKey = KeyCode.Space; // 添加测试按键设置！！！！

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float currentDuration;
    private float currentShakeIntensity;
    private bool isShaking = false;

    void Awake()
    {
        // 单例模式检查
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"Found duplicate GunShake instance on {gameObject.name}. Destroying this instance.");
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        
        // 如果需要在场景切换时保留，取消下面的注释
        // DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
    }

    void Update()
    {
        // 测试按键
        //if (Input.GetKeyDown(testKey))
        //{
        //    OnSuccessfulShot();
        //}

        // 处理震动
        if (isShaking)
        {
            if (currentDuration > 0)
            {
                // 生成随机震动
                Vector3 randomShake = new Vector3(
                    Random.Range(-1f, 1f) * currentShakeIntensity,
                    Random.Range(-0.5f, 1f) * currentShakeIntensity,
                    Random.Range(-0.3f, 0.3f) * currentShakeIntensity
                );

                // 应用震动效果
                transform.localPosition = originalPosition + randomShake;

                // 添加随机旋转
                transform.localRotation = originalRotation * Quaternion.Euler(
                    Random.Range(-2f, 2f) * currentShakeIntensity,
                    Random.Range(-2f, 2f) * currentShakeIntensity,
                    Random.Range(-1f, 1f) * currentShakeIntensity
                );

                // 更新时间和强度
                currentShakeIntensity *= 1f - Time.deltaTime * decreaseFactor;
                currentDuration -= Time.deltaTime;
            }
            else
            {
                // 震动结束，重置位置
                ResetCameraPosition();
            }
        }
    }

    // 公共接口：当射击成功时调用
    public void OnSuccessfulShot()
    {
        // 初始化震动
        isShaking = true;
        currentDuration = shakeDuration;
        currentShakeIntensity = shakeIntensity;
        
        // 应用后坐力
        ApplyRecoil();
    }

    private void ApplyRecoil()
    {
        // 应用后坐力效果
        transform.localPosition -= Vector3.forward * recoilKickback; // 向后推
        transform.localRotation *= Quaternion.Euler(-recoilUpward, 0, 0); // 向上抬
    }

    private void ResetCameraPosition()
    {
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;
        isShaking = false;
    }

    // 停止抖动
    public void StopShake()
    {
        isShaking = false;
        ResetCameraPosition();
    }

    // 提供一个方法来调整抖动参数
    public void SetShakeParameters(float intensity, float duration)
    {
        shakeIntensity = intensity;
        shakeDuration = duration;
    }
}
