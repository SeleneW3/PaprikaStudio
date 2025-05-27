using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelNode : MonoBehaviour
{
        /*
    public LevelManager.Level levelType;  // 替换原来的 nodeIndex
    public LevelNode[] nextNodes;         // 完成后可以进入的下一个关卡节点
    public bool isActive = false;         // 当前节点是否可选

    private void Start()
    {
        UpdateNodeVisibility();
    }


    private void OnMouseDown()
    {
        if (!isActive) return;  // 如果节点不可选，直接返回

        // 先设置当前关卡信息
        if (GameManager.Instance.levelManager != null)
        {
            GameManager.Instance.levelManager.SelectLevel(levelType);
        }
        // 然后加载统一的游戏场景
        GameManager.Instance.LoadScene("Game");
    }

    public void UpdateNodeVisibility()
    {
        // 根据游戏进度和已选择的路径来决定是否显示和激活此节点
        if (GameManager.Instance.levelManager != null)
        {
            isActive = GameManager.Instance.levelManager.IsLevelAvailable(levelType);
            
            // 更新节点的视觉显示
            // 这里可以添加一些视觉效果，比如改变颜色或透明度
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = isActive ? 1f : 0.5f;
                spriteRenderer.color = color;
            }
        }
    }*/
}

