using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Cinemachine;


public class CameraLogic : MonoBehaviour
{
    public Camera mainCam;
    public CinemachineStateDrivenCamera player1CameraController_Gun;  
    public CinemachineStateDrivenCamera player1CameraController_Balance;
    public CinemachineStateDrivenCamera player2CameraController_Gun;    
    public CinemachineStateDrivenCamera player2CameraController_Balance;

    void Start()
    {
        // 确保所有必要的引用都已设置
        if (mainCam == null)
        {
            mainCam = Camera.main;
            Debug.LogWarning("Main Camera was not assigned, attempting to find it automatically.");
        }

        if (player1CameraController_Gun == null || player1CameraController_Balance == null ||player2CameraController_Gun == null || player2CameraController_Balance == null)
        {
            Debug.LogError("State-Driven Camera references not set in CameraLogic!");
            return;
        }
    }

    void Update()
    {
        // 检查 NetworkManager 是否已初始化
        if (NetworkManager.Singleton == null) return;

        // 根据玩家的 LocalClientId 来决定使用哪个摄像机
        if (NetworkManager.Singleton.LocalClientId == 0)
        {
            // 玩家1使用自己的虚拟摄像机控制器
            SwitchToPlayer1Camera();
        }
        else if (NetworkManager.Singleton.LocalClientId == 1)
        {
            // 玩家2使用自己的虚拟摄像机控制器
            SwitchToPlayer2Camera();
        }
    }

    // 切换到玩家1的虚拟摄像机控制器
    private void SwitchToPlayer1Camera()
    {
        player1CameraController_Gun.gameObject.SetActive(true);
        player1CameraController_Balance.gameObject.SetActive(true);

        player2CameraController_Gun.gameObject.SetActive(false);
        player2CameraController_Balance.gameObject.SetActive(false);
    }

    // 切换到玩家2的虚拟摄像机控制器
    private void SwitchToPlayer2Camera()
    {
        player2CameraController_Gun.gameObject.SetActive(true);
        player2CameraController_Balance.gameObject.SetActive(true);

        player1CameraController_Gun.gameObject.SetActive(false);
        player1CameraController_Balance.gameObject.SetActive(false);
    }
}