using Unity.Netcode;
using UnityEngine;

public class BalanceScale : NetworkBehaviour
{
    private NetworkVariable<float> scoreDifference = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Animation 设置")]
    [SerializeField] private Animator animator;
    [SerializeField] private float transitionSpeed = 1f;
    [SerializeField] private float maxScoreDifference = 10f;
    [SerializeField] private float minScoreDifference = 1f;

    [Header("Animation State Names")]
    [SerializeField] private string rotateStateName = "Rotate";
    [SerializeField] private string player1StateName = "Player1";
    [SerializeField] private string player2StateName = "Player2";

    private float currentFrame = 0f;
    private float targetFrame = 0f;

    private void Start()
    {
        SetAllAnimationsFrame(0f);
        if (IsClient)
        {
            scoreDifference.OnValueChanged += OnScoreChanged;
        }
    }

    private void OnDestroy()
    {
        if (IsClient)
        {
            scoreDifference.OnValueChanged -= OnScoreChanged;
        }
    }

    private void OnScoreChanged(float oldValue, float newValue)
    {
        UpdateScoreInternal(newValue);
    }

    private void UpdateScoreInternal(float diff)
    {
        if (Mathf.Abs(diff) < minScoreDifference)
        {
            targetFrame = 0f;
        }
        else if (diff > 0f)
        {
            float normalizedScore = Mathf.Clamp01((diff - minScoreDifference) / (maxScoreDifference - minScoreDifference));
            targetFrame = Mathf.Lerp(1f, 40f, normalizedScore);
        }
        else
        {
            float normalizedScore = Mathf.Clamp01((-diff - minScoreDifference) / (maxScoreDifference - minScoreDifference));
            targetFrame = Mathf.Lerp(51f, 90f, normalizedScore);
        }
    }

    private void Update()
    {
        if (!IsClient) return;

        if (!Mathf.Approximately(currentFrame, targetFrame))
        {
            currentFrame = Mathf.MoveTowards(currentFrame, targetFrame, transitionSpeed * Time.deltaTime * 60f);
            float normalizedTime = currentFrame / 100f;
            SetAllAnimationsFrame(normalizedTime);
        }
    }

    private void SetAllAnimationsFrame(float normalizedTime)
    {
        if (animator == null) return;
        animator.Play(rotateStateName, 0, normalizedTime);
        animator.Play(player1StateName, 1, normalizedTime);
        animator.Play(player2StateName, 2, normalizedTime);
        animator.Update(0f);
        animator.speed = 0f;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetScoreDiffServerRpc(float newDiff)
    {
        scoreDifference.Value = newDiff;
    }
}
