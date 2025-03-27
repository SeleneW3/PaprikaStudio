using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ChessLogic : MonoBehaviour
{
    public Transform stateBeingClicked;

    public Vector3 clickPointPos;
    public Quaternion clickPointRot;

    public float originToBeingClickedDuration = 0.5f;

    public float beingClickedToClickPointDuration = 1f;

    public float heightOffset = 1f;

    public int state = 0;

    public float timer = 0f;

    public float moveSpeed = 1f;

    private Vector3 originalPos;
    private Quaternion originalRot;

    public bool backToOriginal = false;
    public bool isOnGround = false;

    public enum Belonging
    {
        Player1,
        Player2
    }

    public Belonging belonging;

    // Start is called before the first frame update
    void Start()
    {
        originalPos = transform.position;
        originalRot = transform.rotation;
        if(belonging == Belonging.Player1)
        {
            GameManager.Instance.chessComponents[0] = this;
        }
        else
        {
            GameManager.Instance.chessComponents[1] = this;
        }
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
        else if(state == 3)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer/beingClickedToClickPointDuration);
            Vector3 linearPos = Vector3.Lerp(stateBeingClicked.position, clickPointPos, t);
            float verticalOffset = Mathf.Sin(t * Mathf.PI) * heightOffset;
            transform.position = linearPos + Vector3.up * verticalOffset;
            transform.rotation = Quaternion.Slerp(stateBeingClicked.rotation, clickPointRot, t);
            if(t >= 1f)
            {
                state = 4;
                timer = 0f;
                isOnGround = true;
            }
        }
        else if(state == 4 && backToOriginal)
        {
            ReturnToFloatPoint();
        }
        else if(state == 5)
        {
            ResetBack();
        }
        
    }

    private void OnMouseDown()
    {
        if (NetworkManager.Singleton.LocalClientId == 0)
        {
            GameManager.Instance.ChangeChess1StateServerRpc();
        }

        if (NetworkManager.Singleton.LocalClientId == 1)
        {
            GameManager.Instance.ChangeChess2StateServerRpc();
        }


    }


    public void Move()
    {
        if(state == 2)
        {
            state = 3;
            timer = 0f;
        }
    }

    public void ReturnToFloatPoint()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer/originToBeingClickedDuration);
        transform.position = Vector3.Lerp(transform.position, stateBeingClicked.position, t);
        transform.rotation = Quaternion.Lerp(transform.rotation, stateBeingClicked.rotation, t);
        if(t >= 1f)
        {
            state = 5;
            timer = 0f;
        }

    }

    public void ResetBack()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer/originToBeingClickedDuration);
        transform.position = Vector3.Lerp(transform.position,originalPos, t);
        transform.rotation = Quaternion.Lerp(transform.rotation, originalRot, t);
        if(t >= 1f)
        {
            state = 0;
            timer = 0f;
            backToOriginal = false;
            
        }

    }


}
