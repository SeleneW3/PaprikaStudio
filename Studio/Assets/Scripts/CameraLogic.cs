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

    void Start()
    {
        // 确保所有必要的引用都已设置
        if (mainCam == null)
        {
            mainCam = Camera.main;
            Debug.LogWarning("Main Camera was not assigned, attempting to find it automatically.");
        }
    }

    void Update()
    {
        // 检查 NetworkManager 是否已初始化
        if (NetworkManager.Singleton == null) return;

        // 检查必要的组件是否都存在
        if (mainCam == null || player1Cam == null || player2Cam == null)
        {
            Debug.LogError("Camera references not set in CameraLogic!");
            return;
        }

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
