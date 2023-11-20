using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DebugXRInputHelper : MonoBehaviour
{
    public List<bool> isOn;
    public List<InputAction> toggleActions;
    public List<UnityEvent<bool>> onToggleStateChanged;

    private void Awake()
    {
        for (var i = 0; i < onToggleStateChanged.Count; i++)
        {     var i1 = i;

            onToggleStateChanged[i1].Invoke(isOn[i1]);
            toggleActions[i1].performed += ctx =>
            {
                isOn[i1] = !isOn[i1];
                onToggleStateChanged[i1].Invoke(isOn[i1]);
            };
        }
    }

    private void OnEnable()
    {
        foreach (var action in toggleActions)
        {
            action.Enable();
        }
    }

    private void OnDisable()
    {
        foreach (var action in toggleActions)
        {
            action.Disable();
        }
    }
}
