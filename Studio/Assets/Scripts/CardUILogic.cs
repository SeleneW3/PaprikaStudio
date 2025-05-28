using UnityEngine;

public class CardUILogic : MonoBehaviour
{
    public Vector3 originalPos;
    public Quaternion originalRot;

    public Vector3 targetPos;
    public Quaternion targetRot;

    public float transitionDuration = 0.5f;

    private float t = 0f;
    private bool transitioningToTarget = false;
    private bool transitioningToOriginal = false;



    void Start()
    {
        transform.position = originalPos;
        transform.rotation = originalRot;
    }

    void Update()
    {
        if (transitioningToTarget)
        {
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
