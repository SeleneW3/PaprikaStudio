using UnityEngine;
using Cinemachine;

public class CameraManager : MonoBehaviour
{
    private static CameraManager instance;
    public static CameraManager Instance { get { return instance; } }

    [Header("Virtual Cameras")]
    [SerializeField] private CinemachineVirtualCamera mainCamera;
    [SerializeField] private CinemachineVirtualCamera gunCamera;
    [SerializeField] private CinemachineVirtualCamera balanceCamera;

    [Header("Priority Settings")]
    [SerializeField] private int activeWeight = 20;
    [SerializeField] private int inactiveWeight = 5;

    public enum CameraType
    {
        Main,
        Gun,
        Balance
    }

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    void Start()
    {
        // 初始化为主相机
        SwitchCamera(CameraType.Main);
    }

    public void SwitchCamera(CameraType type)
    {
        // 将所有相机设置为低优先级
        mainCamera.Priority = inactiveWeight;
        gunCamera.Priority = inactiveWeight;
        balanceCamera.Priority = inactiveWeight;

        // 激活选定的相机
        switch (type)
        {
            case CameraType.Main:
                mainCamera.Priority = activeWeight;
                break;
            case CameraType.Gun:
                gunCamera.Priority = activeWeight;
                break;
            case CameraType.Balance:
                balanceCamera.Priority = activeWeight;
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
