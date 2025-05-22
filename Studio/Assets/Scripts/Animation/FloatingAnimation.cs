using UnityEngine;

public class FloatingAnimation : MonoBehaviour
{
    [Header("浮动设置")]
    [SerializeField] private float amplitude = 0.5f;    // 浮动幅度
    [SerializeField] private float frequency = 1f;      // 浮动频率
    [SerializeField] private bool randomizeStart = true; // 是否随机起始位置

    [Header("旋转设置（可选）")]
    [SerializeField] private bool enableRotation = false;  // 是否启用旋转
    [SerializeField] private float rotationSpeed = 30f;    // 旋转速度
    [SerializeField] private Vector3 rotationAxis = Vector3.up; // 旋转轴

    private Vector3 startPosition;
    private float startTime;
    private float randomOffset;

    void Start()
    {
        // 记录初始位置
        startPosition = transform.position;
        
        // 如果启用随机起始位置，生成一个随机偏移
        if (randomizeStart)
        {
            randomOffset = Random.Range(0f, 2f * Mathf.PI);
        }
        
        startTime = Time.time;
    }

    void Update()
    {
        // 计算当前时间点
        float timeSinceStart = Time.time - startTime;
        
        // 计算正弦波形
        float sine = Mathf.Sin((timeSinceStart * frequency + randomOffset) * Mathf.PI * 2f);
        
        // 应用浮动效果
        Vector3 newPosition = startPosition + Vector3.up * (sine * amplitude);
        transform.position = newPosition;

        // 如果启用了旋转，应用旋转效果
        if (enableRotation)
        {
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
        }
    }

    // 重置位置（如果需要的话）
    public void ResetPosition()
    {
        transform.position = startPosition;
        startTime = Time.time;
        if (randomizeStart)
        {
            randomOffset = Random.Range(0f, 2f * Mathf.PI);
        }
    }

    // 设置浮动参数的方法
    public void SetFloatingParameters(float newAmplitude, float newFrequency)
    {
        amplitude = newAmplitude;
        frequency = newFrequency;
    }

    // 启用/禁用旋转
    public void SetRotation(bool enable)
    {
        enableRotation = enable;
    }
} 