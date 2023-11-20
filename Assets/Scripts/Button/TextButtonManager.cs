using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

public class TextButtonManager : MonoBehaviour
{
    private void Start()
    {
        InitButton();
    }

    #region//button ¶¯»­
    private Button button;
    private TMP_Text value;
    private Image icon;
    private float animaTime = 0.1f;
    private void InitButton()
    {
        button = GetComponentInChildren<Button>();
        icon = button.GetComponentInChildren<Image>();
        value = button.GetComponentInChildren<TMP_Text>();

       EventTrigger eventTrigger =  button.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry Normal = new EventTrigger.Entry();
        Normal.eventID = EventTriggerType.PointerExit;
        Normal.callback.AddListener((eventData) => { OnNormal(); });
        eventTrigger.triggers.Add(Normal);

        EventTrigger.Entry Highlighted = new EventTrigger.Entry();
        Highlighted.eventID = EventTriggerType.PointerEnter;
        Highlighted.callback.AddListener((eventData) => { OnHighlighted(); });
        eventTrigger.triggers.Add(Highlighted);

        SetButtonState();
    }
    private void OnNormal()
    {
        SetButtonState();
    }
    private void OnHighlighted()
    {
        SetButtonState(0.8f);
    }

    private void SetButtonState(float a = 0.6f)
    {
        icon.DOFade(a, animaTime);
        value.DOFade(a, animaTime);
    }
    #endregion
}
