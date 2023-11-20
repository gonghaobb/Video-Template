using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HeadsetMenu : MonoBehaviour
{
    [SerializeField]
    private Image image;
    [SerializeField]
    private Sprite[] sprites;
    public InputAction NextAction =new InputAction("",InputActionType.Button);
    private int tempIndex;
    private void Awake()
    {
        NextAction.performed += ctx => { OnSwitchShow(ctx); };
    }

    private void OnSwitchShow(InputAction.CallbackContext ctx)
    {
        tempIndex = tempIndex < sprites.Length - 1 ? tempIndex + 1 : 0;
        image.sprite = sprites[tempIndex];
        image.SetNativeSize();
    }

    public void OnEnable()
    {
        NextAction.Enable();
    }
    public void OnDisable()
    {
        NextAction.Disable();
    }
}
