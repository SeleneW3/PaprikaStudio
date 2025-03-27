using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float initialHeight = 5f;    // 初始高度
    [SerializeField] private float additionalForce = 12f; // 额外的向下力
    
    private Rigidbody rb;
    private Vector3 startPosition;
    private bool hasLanded = false;

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
            // 可以在这里添加碰撞音效或特效
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
