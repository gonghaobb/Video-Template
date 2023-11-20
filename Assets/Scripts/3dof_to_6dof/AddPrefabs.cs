using RenderHeads.Media.AVProVideo;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AddPrefabs : MonoBehaviour
{
    [SerializeField] InputAction _ActionDown_a, _ActionDown_b;
    [SerializeField] GameObject _InRoomPrefab;
    [SerializeField] GameObject _TabPanelObject;

    private GameObject tempInRoomPrefab;
    private void Awake()
    {
        _ActionDown_a.performed += ctx => { OnResPrefab(ctx); };
        _ActionDown_b.performed += ctx => { OnTabStart(ctx); };
    }

    private void Start()
    {
        tempInRoomPrefab = Instantiate(_InRoomPrefab, transform);
    }
    private void OnTabStart(InputAction.CallbackContext ctx)
    {
        if (_TabPanelObject != null) { }
        _TabPanelObject.SetActive(!_TabPanelObject.activeSelf);
    }

    private void OnResPrefab(InputAction.CallbackContext ctx)
    {
        if (tempInRoomPrefab!=null)
        {
            DestroyImmediate(tempInRoomPrefab);
        }
        tempInRoomPrefab = Instantiate(_InRoomPrefab, transform);
    }

    public void OnEnable()
    {
        _ActionDown_a.Enable();
        _ActionDown_b.Enable();
    }

    public void OnDisable()
    {
        _ActionDown_a.Disable();
        _ActionDown_b.Disable();
    }
}
