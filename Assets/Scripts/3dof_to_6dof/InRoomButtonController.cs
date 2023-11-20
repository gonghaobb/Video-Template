using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class InRoomButtonController : MonoBehaviour
{
    [SerializeField] private Image _inRoom;//Ä¬ÈÏ×´Ì¬
    [SerializeField] private Image _waitInRoom;//500ms ºóÄ¬ÈÏ×´Ì¬
    [SerializeField] private Image _hoverInRoom;//
    [SerializeField] private GameObject _animaInRoom;
    [SerializeField] private TMP_Text _text;

    private bool isHover = false,isTimeOver;

   private void OnEnable()
    {
        StartCoroutine(WaitButtonScale());
        CreateTimelineDragEvents();
    }


    IEnumerator WaitButtonScale()
    {
        yield return new WaitForSeconds(5f);
        if (!isHover) _animaInRoom.SetActive(true);
        _waitInRoom.DOFade(1f,0.2f);
        _text.DOColor(new Color(0,0,0,0.8f),0.2f);
        isTimeOver = true;
    }
    private void CreateTimelineDragEvents()
    {
        EventTrigger trigger = _inRoom.gameObject.GetComponent<EventTrigger>();
        if (trigger != null)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry();

            entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerEnter;
            entry.callback.AddListener((data) => { OnTimelineBeginHover((PointerEventData)data); });
            trigger.triggers.Add(entry);

            entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerExit;
            entry.callback.AddListener((data) => { OnTimelineEndHover((PointerEventData)data); });
            trigger.triggers.Add(entry);
        }
    }
    private void OnTimelineBeginHover(PointerEventData data)
    {
        isHover = true;
        _hoverInRoom.DOFade(1, 0.2f);
        StartCoroutine(SetAnimaInRoom(false));
    }
    private void OnTimelineEndHover(PointerEventData data)
    {
        isHover = false;
        _hoverInRoom.DOFade(0, 0.2f);
        if (isTimeOver)
        {
            StartCoroutine(SetAnimaInRoom(true));
        }
    }

    IEnumerator SetAnimaInRoom(bool isOpen)
    {
        float wait = isOpen ? 1f : 0f;
        yield return new WaitForSeconds(wait);
        if (isOpen.Equals(!isHover))
        {
            _animaInRoom.SetActive(isOpen);
        }
    }
}
