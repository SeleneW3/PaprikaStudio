using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float initialHeight = 5f;    // 初始高度
    [SerializeField] private float additionalForce = 12f; // 额外的向下力
    [SerializeField] private float startDelay = 0f;       // 开始延迟时间
    
    private Rigidbody rb;
    private Vector3 startPosition;
    private bool hasLanded = false;
    private bool hasStarted = false;         // 是否已经开始下落
    private int collisionCount = 0;          // 碰撞次数计数

    // Start is called before the first frame update
    void Start()
    {
        // 获取或添加刚体组件
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // 设置刚体属性
        rb.useGravity = true;
        rb.drag = 0f;                       // 设置阻力为0
        rb.constraints = RigidbodyConstraints.FreezeRotation | 
                        RigidbodyConstraints.FreezePositionX | 
                        RigidbodyConstraints.FreezePositionZ;
        
        // 保存起始位置
        startPosition = transform.position;
        
        // 设置初始高度
        transform.position = new Vector3(startPosition.x, initialHeight, startPosition.z);

        // 初始时禁用重力
        rb.useGravity = false;
        
        // 使用Invoke延迟启动
        Invoke("StartFalling", startDelay);
    }

    void StartFalling()
    {
        if (!hasStarted)
        {
            hasStarted = true;
            rb.useGravity = true;
            rb.AddForce(Vector3.down * additionalForce, ForceMode.Impulse);
        }
    }

    void FixedUpdate()
    {
        if (!hasLanded && hasStarted)
        {
            // 持续添加向下的力
            rb.AddForce(Vector3.down * additionalForce, ForceMode.Force);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Table"))
        {
            collisionCount++;
            
            // 播放棋子落下音效，第二次碰撞音量减半
            if (SoundManager.Instance != null)
            {
                float volume = collisionCount > 1 ? 0.5f : 1f;
                SoundManager.Instance.SetSFXVolumeForClip("ChessDown", volume);
                SoundManager.Instance.PlaySFX("ChessDown");
            }

            // 添加相机抖动效果，第一次碰撞强度更大
            if (CameraShake.Instance != null)
            {
                float shakeIntensity = collisionCount > 1 ? 0.1f : 0.2f;  // 第二次碰撞抖动减半
                float shakeDuration = collisionCount > 1 ? 0.15f : 0.3f;  // 第二次碰撞持续时间减半
                CameraShake.Instance.ShakeCamera(shakeIntensity, shakeDuration);
            }
            
            // 检查是否已经落地
            if (!hasLanded && rb.velocity.magnitude < 0.1f)
            {
                hasLanded = true;
                // 可以在这里触发其他事件
            }
        }
    }

    // 重置动画的公共方法
    public void ResetAnimation()
    {
        hasLanded = false;
        hasStarted = false;
        collisionCount = 0;  // 重置碰撞计数
        rb.useGravity = false;
        rb.velocity = Vector3.zero;
        transform.position = new Vector3(startPosition.x, initialHeight, startPosition.z);
        
        // 重新延迟启动
        Invoke("StartFalling", startDelay);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
