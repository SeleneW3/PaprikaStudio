using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private static CameraShake instance;
    public static CameraShake Instance { get { return instance; } }

    [Header("Shake Settings")]
    [SerializeField] private float shakeIntensity = 0.2f;    // 抖动强度
    [SerializeField] private float shakeDuration = 0.3f;    // 抖动持续时间
    [SerializeField] private float decreaseFactor = 2f;    // 抖动衰减系数

    private Vector3 originalPosition;
    private float currentDuration;
    private bool isShaking = false;

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    void Start()
    {
        originalPosition = transform.localPosition;
    }

    // 公共方法：开始相机抖动
    public void ShakeCamera(float intensity = -1, float duration = -1)
    {
        // 如果没有指定参数，使用默认值
        float shakeAmount = intensity < 0 ? shakeIntensity : intensity;
        float shakeDur = duration < 0 ? shakeDuration : duration;

        StopAllCoroutines();
        StartCoroutine(ShakeCoroutine(shakeAmount, shakeDur));
    }

    private IEnumerator ShakeCoroutine(float intensity, float duration)
    {
        isShaking = true;
        currentDuration = duration;

        while (currentDuration > 0)
        {
            if (!isShaking)
            {
                transform.localPosition = originalPosition;
                yield break;
            }

            // 生成随机抖动位置
            transform.localPosition = originalPosition + Random.insideUnitSphere * intensity;

            // 随时间减小抖动强度
            intensity *= 1f - Time.deltaTime * decreaseFactor;
            currentDuration -= Time.deltaTime;

            yield return null;
        }

        // 恢复原始位置
        transform.localPosition = originalPosition;
        isShaking = false;
    }

    // 停止抖动
    public void StopShake()
    {
        isShaking = false;
        StopAllCoroutines();
        transform.localPosition = originalPosition;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
