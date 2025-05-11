using UnityEngine;
using Unity.Netcode;
using Cinemachine;
using System.Collections;

public class MapCameraController : NetworkBehaviour
{
    [Header("Virtual Cameras")]
    public CinemachineVirtualCamera initialCamera;    // 初始视角的虚拟相机
    public CinemachineVirtualCamera targetCamera;     // 目标视角的虚拟相机
    
    [Header("Transition Settings")]
    public float switchDelay = 0.5f;                  // 切换延迟时间
    public float blendDuration = 0.5f;               // 切换过渡时间

    private void Start()
    {
        if (initialCamera == null || targetCamera == null)
        {
            Debug.LogError("Virtual Cameras not assigned to MapCameraController!");
            return;
        }

        // 设置初始优先级
        initialCamera.Priority = 20;
        targetCamera.Priority = 10;

        // 延迟调用切换方法
        Invoke("SwitchToTargetCamera", switchDelay);
    }

    private void SwitchToTargetCamera()
    {
        // 设置 CinemachineBrain 的混合时间
        var brain = Camera.main.GetComponent<CinemachineBrain>();
        if (brain != null)
        {
            brain.m_DefaultBlend.m_Time = blendDuration;
        }

        // 切换相机优先级
        initialCamera.Priority = 10;
        targetCamera.Priority = 20;
    }

    // 提供公共方法用于手动触发相机切换
    public void SwitchCamera()
    {
        SwitchToTargetCamera();
    }
}