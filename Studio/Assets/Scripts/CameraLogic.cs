using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Cinemachine;


public class CameraLogic : NetworkManager
{
    public Camera mainCam;
    public GameObject player1GunState;
    public GameObject player1BalanceState;
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

        if (NetworkManager.Singleton.LocalClientId == 0)
        {
            Invoke("SwitchToPlayer1Camera", 0.5f);
        }

        if (NetworkManager.Singleton.LocalClientId == 1)
        {
            Invoke("SwitchToPlayer2Camera", 0.5f);
        }


    }



    // 切换到玩家1的虚拟摄像机控制器
    private void SwitchToPlayer1Camera()
    {
        player1GunState.gameObject.SetActive(true);
        player1BalanceState.SetActive(false);

        player2CameraController_Gun.gameObject.SetActive(false);
        player2CameraController_Balance.gameObject.SetActive(false);
    }

    // 切换到玩家2的虚拟摄像机控制器
    private void SwitchToPlayer2Camera()
    {
        player2CameraController_Gun.gameObject.SetActive(true);
        player2CameraController_Balance.gameObject.SetActive(false);

        player1GunState.gameObject.SetActive(false);
        player1BalanceState.SetActive(false);
    }
}