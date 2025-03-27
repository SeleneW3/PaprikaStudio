using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float initialHeight = 5f;    // 初始高度
    [SerializeField] private float additionalForce = 12f; // 额外的向下力
    
    [Header("Camera Shake Settings")]
    [SerializeField] private float shakeIntensityOnCollision = 0.05f;  // 碰撞时的抖动强度
    [SerializeField] private float shakeDurationOnCollision = 0.1f;    // 碰撞时的抖动持续时间
    
    private Rigidbody rb;
    private Vector3 startPosition;
    private bool hasLanded = false;
    private float lastShakeTime = 0f;       // 上次触发抖动的时间
    private float shakeInterval = 0.1f;     // 最小抖动间隔

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

        // 在开始时添加一个向下的初始力
        rb.AddForce(Vector3.down * additionalForce, ForceMode.Impulse);
    }

    void FixedUpdate()
    {
        if (!hasLanded)
        {
            // 持续添加向下的力
            rb.AddForce(Vector3.down * additionalForce, ForceMode.Force);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Table"))
        {
            // 计算碰撞强度
            float collisionForce = collision.relativeVelocity.magnitude;
            
            // 只有当碰撞力足够大且距离上次抖动有一定间隔时才触发相机抖动
            if (collisionForce > 0.1f && Time.time - lastShakeTime > shakeInterval)
            {
                // 根据碰撞力度调整抖动强度
                float intensity = Mathf.Clamp(collisionForce * 0.01f, 0, shakeIntensityOnCollision);
                
                // 触发相机抖动
                if (CameraShake.Instance != null)
                {
                    CameraShake.Instance.ShakeCamera(intensity, shakeDurationOnCollision);
                    lastShakeTime = Time.time;
                }
            }

            if (!hasLanded && rb.velocity.magnitude < 0.1f)
            {
                hasLanded = true;
            }
        }
    }

    // 重置动画的公共方法
    public void ResetAnimation()
    {
        hasLanded = false;
        rb.velocity = Vector3.zero;
        transform.position = new Vector3(startPosition.x, initialHeight, startPosition.z);
        // 重置时也添加初始力
        rb.AddForce(Vector3.down * additionalForce, ForceMode.Impulse);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
