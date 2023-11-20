using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShowNameCtrl : MonoBehaviour
{
    private void Start()
    {
        EventTrigger trigger = GetComponent<EventTrigger>();
        if (trigger != null)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerEnter;
            entry.callback.AddListener((data) => { OnProgressBeginHover((PointerEventData)data); });
            trigger.triggers.Add(entry);
        }
    }

    public void OnProgressBeginHover(PointerEventData data)
    {
        MessageController.GetInstance().SetMessage("shifting_"+name);
    }
}
