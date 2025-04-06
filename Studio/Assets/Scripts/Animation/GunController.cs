using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : MonoBehaviour
{
    private Animator gunAnimator;  // 该枪的 Animator

    void Start()
    {
        // 获取枪的 Animator 组件
        gunAnimator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 调用此方法来触发枪的开火动画
    public void FireGun()
    {
        if (gunAnimator != null)
        {
            gunAnimator.SetTrigger("Grab");  // 设置触发器，开始GrabGun动画
        }
        else
        {
            Debug.LogError("GunAnimator not found on " + gameObject.name);
        }
    }
    
}
