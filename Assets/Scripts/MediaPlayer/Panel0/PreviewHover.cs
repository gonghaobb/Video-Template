using System;
using RenderHeads.Media.AVProVideo;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class PreviewHover : MonoBehaviour
{
    [SerializeField] RectTransform PreviewParent;
    [SerializeField] RectTransform _bgImage, _preview;
    [SerializeField] Slider _sliderTime = null;//播放进度条
    [SerializeField] TMP_Text _Text;
    

    [Header("Action")]
    [SerializeField] InputAction _triggleActionDown, _triggleActionUp;

    public bool _isHoveringPreview;//是否hover
    public bool _isTriggleOver;// 是否按着trigger
    private Vector2 tempPos;
    [SerializeField] private string tempTime;

    private void Awake()
    {
        _triggleActionDown.performed += ctx => { OnTriggleDown(ctx); };
        _triggleActionUp.performed += ctx => { OnTriggleUp(ctx); };
    }

    public void OnEnable()
    {
        _triggleActionDown.Enable();
        _triggleActionUp.Enable();
        MessageController.ObserverEvent += this.ParseData;
    }

    private void ParseData(string str)
    {
        if (str.StartsWith("shifting_"))
        {
            string end = str.Split('_')[1];
            int temp = Convert.ToInt32(end);

            int sec = Helper.GetSecondString(tempTime.Split('/')[0]) + temp;
            Debug.Log(tempTime.Split('/')[0]);
            string strTime = Helper.GetTimeString(sec) + "/" + tempTime.Split('/')[1];
            _Text.text = strTime;
        }
    }

    public void OnDisable()
    {
        _triggleActionDown.Disable();
        _triggleActionUp.Enable();
        MessageController.ObserverEvent -= this.ParseData;
    }

    private void Start()
    {
        CreateProgressHoverEvents();
    }
    private void Update()
    {
        if (_isTriggleOver && _isHoveringPreview)
        {
            _sliderTime.interactable = false;
            PreviewParent.anchoredPosition = tempPos;
            _preview.DOSizeDelta(new Vector2(2212f, 129f), 0.1f);
            tempTime = _Text.text;
        }
        else
        {
            _sliderTime.interactable = true;
            _preview.DOSizeDelta(new Vector2(172f, 129f), 0.1f);
        }
    }

    private void CreateProgressHoverEvents()
    {
        EventTrigger trigger = _bgImage.GetComponent<EventTrigger>();
        if (trigger != null)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry();

            entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerEnter;
            entry.callback.AddListener((data) => { OnProgressBeginHover((PointerEventData)data); });
            trigger.triggers.Add(entry);

            entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerExit;
            entry.callback.AddListener((data) => { OnProgressEndHover((PointerEventData)data); });
            trigger.triggers.Add(entry);
        }
    }

    private void OnProgressBeginHover(PointerEventData data)
    {
        tempPos = GetComponent<RectTransform>().anchoredPosition;
        _isHoveringPreview = true;
    }
    private void OnProgressEndHover(PointerEventData data)
    {
        _isHoveringPreview = false;
    }
    private void OnTriggleDown(InputAction.CallbackContext ctx)
    {
        _isTriggleOver = true;
    }
    private void OnTriggleUp(InputAction.CallbackContext ctx)
    {
        _isTriggleOver = false;
        _isHoveringPreview = true?false:false;
        gameObject.SetActive(false);
    }


}
