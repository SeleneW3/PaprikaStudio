using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessLogic : MonoBehaviour
{
    public Transform stateBeingClicked;
    public Transform clickPoint;

    public float originToBeingClickedDuration = 0.5f;

    public float beingClickedToClickPointDuration = 1f;

    public float heightOffset = 1f;

    public int state = 0;

    private float timer = 0f;

    private Vector3 originalPos;
    private Quaternion originalRot;

    // Start is called before the first frame update
    void Start()
    {
        originalPos = transform.position;
        originalRot = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if(state == 1)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer/originToBeingClickedDuration);
            transform.position = Vector3.Lerp(originalPos, stateBeingClicked.position, t);
            transform.rotation = Quaternion.Lerp(originalRot, stateBeingClicked.rotation, t);

            if(t >= 1f)
            {
                state = 2;
                timer = 0f;
            }
        }
        else if(state == 2)
        {
            if(Input.GetMouseButtonDown(0))
            {
                state = 3;
                timer = 0f;
            }
        }
        else if(state == 3)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer/beingClickedToClickPointDuration);
            Vector3 linearPos = Vector3.Lerp(stateBeingClicked.position, clickPoint.position, t);
            float verticalOffset = Mathf.Sin(t * Mathf.PI) * heightOffset;
            transform.position = linearPos + Vector3.up * verticalOffset;
            transform.rotation = Quaternion.Slerp(stateBeingClicked.rotation, clickPoint.rotation, t);
        }
    }

    private void OnMouseDown()
    {
        if (state == 0)
        {
            state = 1;
            timer = 0f;
        }
        Debug.Log("Mouse Down");
    }
}
