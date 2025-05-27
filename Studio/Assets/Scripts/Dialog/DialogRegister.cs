using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogRegister : MonoBehaviour
{
    public enum Type
    {
        Panel,
        DialogText
    }

    public Type type;
    void Start()
    {
        if(type == Type.Panel)
        {
            DialogManager.Instance.dialogPanel = gameObject;
        }
        else if (type == Type.DialogText)
        {
            DialogManager.Instance.dialogText = GetComponent<TMPro.TextMeshProUGUI>();
        }
        else
        {
            Debug.LogError("DialogRegister: Invalid type specified.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
