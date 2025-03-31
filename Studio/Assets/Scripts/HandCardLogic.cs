using UnityEngine;

public class HandCardLogic : MonoBehaviour
{
    public float fanAngle = 45f;    // 卡牌整体扇形角度
    public float duration = 0.5f;   // 动画时长（从0到1所需时间）
    public float spread = 0.5f;     // 每张卡牌左右偏移的距离

    // 状态标记，true 表示展开，false 表示收回
    public bool opened = false;

    public Transform[] cards;
    private Vector3[] originalPositions;
    private Quaternion[] originalRotations;

    // 插值变量 0 表示关闭状态，1 表示展开状态
    private float transition = 0f;

    void Start()
    {
        // 如果发牌时已经有子物体，则初始化缓存
        if (transform.childCount > 0)
            Initialize();
    }

    // 发牌后调用，用于缓存卡牌初始状态
    public void Initialize()
    {
        Debug.Log("Initialize");
        int count = transform.childCount;
        cards = new Transform[count];
        originalPositions = new Vector3[count];
        originalRotations = new Quaternion[count];

        for (int i = 0; i < count; i++)
        {
            cards[i] = transform.GetChild(i);
            originalPositions[i] = cards[i].localPosition;
            originalRotations[i] = cards[i].localRotation;
        }
    }

    // 外部可调用，设置状态为展开
    public void Open()
    {
        opened = true;
    }

    // 外部可调用，设置状态为收回
    public void Close()
    {
        opened = false;
    }

    void Update()
    {
        if (cards == null || cards.Length == 0)
            return;

        // 根据 desired state 调整 transition 值（0～1之间）
        if (opened)
        {
            transition += Time.deltaTime / duration;
        }
        else
        {
            transition -= Time.deltaTime / duration;
        }
        transition = Mathf.Clamp01(transition);

        int count = cards.Length;
        for (int i = 0; i < count; i++)
        {
            // 计算目标旋转角度：从 fanAngle/2 到 -fanAngle/2（即展开时卡牌上侧聚拢，下侧展开）
            float angle = fanAngle / 2 - (fanAngle / (count - 1)) * i;
            Quaternion targetRot = Quaternion.Euler(0, 0, angle);
            // 根据索引计算左右偏移，让中间的卡牌保持不动，两侧分别向左右移动
            float offsetX = (i - (count - 1) / 2f) * spread;
            Vector3 targetPos = originalPositions[i] + new Vector3(offsetX, 0, 0);

            // 通过插值计算当前状态
            cards[i].localPosition = Vector3.Lerp(originalPositions[i], targetPos, transition);
            cards[i].localRotation = Quaternion.Lerp(originalRotations[i], targetRot, transition);
        }
    }
}