using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Cinemachine;

public class CameraLogic : MonoBehaviour
{
    public Camera mainCam;
    public CinemachineVirtualCamera player1Cam;  // 玩家1的虚拟摄像机
    public CinemachineVirtualCamera player2Cam;  // 玩家2的虚拟摄像机

    public float smoothSpeed = 5f;

    void Start()
    {
        // 确保所有必要的引用都已设置
        if (mainCam == null)
        {
            mainCam = Camera.main;
            Debug.LogWarning("Main Camera was not assigned, attempting to find it automatically.");
        }

        // 确保 CinemachineVirtualCamera 已设置
        if (player1Cam == null || player2Cam == null)
        {
            Debug.LogError("Virtual Cameras for players not assigned!");
            return;
        }

        // 在开始时禁用两个虚拟摄像机
        player1Cam.gameObject.SetActive(false);
        player2Cam.gameObject.SetActive(false);
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

        // 切换虚拟摄像机
        if (NetworkManager.Singleton.LocalClientId == 0)
        {
            SwitchToPlayer1Camera();
        }
        else if (NetworkManager.Singleton.LocalClientId == 1)
        {
            SwitchToPlayer2Camera();
        }
    }

    // 切换到玩家1的虚拟摄像机
    private void SwitchToPlayer1Camera()
    {
        player1Cam.gameObject.SetActive(true);  // 激活玩家1的虚拟摄像机
        player2Cam.gameObject.SetActive(false); // 禁用玩家2的虚拟摄像机
    }

    // 切换到玩家2的虚拟摄像机
    private void SwitchToPlayer2Camera()
    {
        player1Cam.gameObject.SetActive(false); // 禁用玩家1的虚拟摄像机
        player2Cam.gameObject.SetActive(true);  // 激活玩家2的虚拟摄像机
    }
}
