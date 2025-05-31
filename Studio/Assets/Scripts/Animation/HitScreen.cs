using UnityEngine;

public class HitScreen : MonoBehaviour
{
    private ScreenDamage screenDamage;

    [Header("按 A 时：血量将下降到此值，并恢复到 targetOnA 后停止，之后增速变为 0")]
    public float hitHealth = 10f;
    [Header("按 A 时恢复目标血量（达到后停止）")]
    public float targetOnA = 50f;

    [Header("按 D 时：血量将设为 startOnD，并恢复到 targetOnD 后停止")]
    public float startOnD = 50f;
    [Header("按 D 时恢复目标血量（达到后停止）")]
    public float targetOnD = 100f;

    [Header("血量恢复速度（每秒增量）")]
    public float changeSpeed = 4f;

    // 内部状态
    private bool isRecovering = false;
    private float recoverGoal;
    private float defaultSpeed;

    void Start()
    {
        screenDamage = GetComponent<ScreenDamage>();
        if (screenDamage == null)
        {
            Debug.LogError("HitScreen: 未找到 ScreenDamage 组件");
            enabled = false;
            return;
        }
        // 保存初始速度，用于重置
        defaultSpeed = changeSpeed;
    }

    public void TriggerHitEffect()
    {
        // 模拟按 A 键的效果
        screenDamage.CurrentHealth = hitHealth;
        screenDamage.ShowDamage(hitHealth);
        screenDamage.ShowBlur();

        changeSpeed = defaultSpeed;  // 确保使用默认速度
        recoverGoal = targetOnA;
        isRecovering = true;

        Debug.Log($"Hit Effect Triggered: 血量设为 {hitHealth}，开始恢复到 {targetOnA}");
    }

    public void CancelEffect()
    {
        // 模拟按 D 键的效果
        screenDamage.CurrentHealth = startOnD;
        screenDamage.ShowDamage(startOnD);

        changeSpeed = defaultSpeed;
        recoverGoal = targetOnD;
        isRecovering = true;

        Debug.Log($"Hit Effect Cancelled: 血量设为 {startOnD}，开始恢复到 {targetOnD}");
    }

    void Update()
    {
        // 按 A：设血量到 hitHealth，触发模糊，开始恢复到 targetOnA
        if (Input.GetKeyDown(KeyCode.A))
        {
            TriggerHitEffect();
        }

        // 按 D：设血量到 startOnD，清除模糊，开始恢复到 targetOnD
        if (Input.GetKeyDown(KeyCode.D))
        {
            CancelEffect();
        }

        // 渐变恢复逻辑
        if (isRecovering)
        {
            float newHealth = Mathf.MoveTowards(
                screenDamage.CurrentHealth,
                recoverGoal,
                changeSpeed * Time.deltaTime
            );
            screenDamage.CurrentHealth = newHealth;
            screenDamage.ShowDamage(newHealth);

            if (Mathf.Approximately(newHealth, recoverGoal))
            {
                isRecovering = false;
                // 如果是 A 的目标，血量到达 50 后，将速度置 0
                if (Mathf.Approximately(recoverGoal, targetOnA))
                {
                    changeSpeed = 0f;
                    Debug.Log($"按 A 恢复完成，血量 = {newHealth}，恢复速度已置 0");
                }
                else
                {
                    Debug.Log($"按 D 恢复完成，血量 = {newHealth}");
                }
            }
        }

        // 空格打印当前血量
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"当前血量 = {screenDamage.CurrentHealth}");
        }
    }
}
