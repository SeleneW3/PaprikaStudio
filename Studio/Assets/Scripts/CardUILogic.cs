using UnityEngine;

public class CardUILogic : MonoBehaviour
{
    public Vector3 originalPos;
    public Quaternion originalRot;

    public Vector3 targetPos;
    public Quaternion targetRot;

    public float transitionDuration = 0.5f;
    
    [Header("Sound Effects")]
    [SerializeField] private string cardMoveSound = "CardMove"; // 卡牌悬停音效名称

    private float t = 0f;
    private bool transitioningToTarget = false;
    private bool transitioningToOriginal = false;
    private bool hasSoundPlayed = false; // 跟踪是否已播放音效

    void Start()
    {
        transform.position = originalPos;
        transform.rotation = originalRot;
    }

    void Update()
    {
        if (transitioningToTarget)
        {
            // 只在转场开始时播放一次音效
            if (!hasSoundPlayed)
            {
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySFX(cardMoveSound);
                }
                hasSoundPlayed = true;
            }
            
            t += Time.deltaTime / transitionDuration;
            t = Mathf.Clamp01(t);
            transform.position = Vector3.Lerp(originalPos, targetPos, t);
            transform.rotation = Quaternion.Lerp(originalRot, targetRot, t);

            if (t >= 1f)
            {
                transitioningToTarget = false;
            }
        }
        else if (transitioningToOriginal)
        {
            // 只在转场回原位开始时播放一次音效
            if (hasSoundPlayed)
            {
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySFX(cardMoveSound);
                }
                hasSoundPlayed = false;
            }
            
            t -= Time.deltaTime / transitionDuration;
            t = Mathf.Clamp01(t);
            transform.position = Vector3.Lerp(originalPos, targetPos, t);
            transform.rotation = Quaternion.Lerp(originalRot, targetRot, t);

            if (t <= 0f)
            {
                transitioningToOriginal = false;
            }
        }
    }

    private void OnMouseEnter()
    {
        transitioningToTarget = true;
        transitioningToOriginal = false;
        //Debug.Log("Mouse Entered");
    }

    private void OnMouseExit()
    {
        transitioningToTarget = false;
        transitioningToOriginal = true;
        //Debug.Log("Mouse Exited");
    }
}
