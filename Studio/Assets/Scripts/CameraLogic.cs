using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CameraLogic : MonoBehaviour
{
    public Camera mainCam;
    public Transform player1Cam;
    public Transform player2Cam;

    public float smoothSpeed = 5f;

    void Update()
    {
        if (NetworkManager.Singleton.LocalClientId == 0)
        {
            mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, player1Cam.position, smoothSpeed * Time.deltaTime);
            mainCam.transform.rotation = Quaternion.Lerp(mainCam.transform.rotation, player1Cam.rotation, smoothSpeed * Time.deltaTime);
        }
        else if (NetworkManager.Singleton.LocalClientId == 1)
        {
            mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, player2Cam.position, smoothSpeed * Time.deltaTime);
            mainCam.transform.rotation = Quaternion.Lerp(mainCam.transform.rotation, player2Cam.rotation, smoothSpeed * Time.deltaTime);
        }
    }
}
