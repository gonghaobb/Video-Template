using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

public class IconButtonManager : MonoBehaviour
{
    private Button button;

    private void Start()
    {
        InitButton();
        button.onClick.AddListener(() => {
            Debug.Log(gameObject.name + "click");
        });
    }

    #region//button ¶¯»­
    private TMP_Text Footnote;
    private Image icon;
    private float animaTime = 0.1f;
    private void InitButton()
    {
        button = GetComponentInChildren<Button>();
        icon = button.GetComponentInChildren<Image>();
        Footnote = GetComponentInChildren<TMP_Text>(false);

       EventTrigger eventTrigger =  gameObject.AddComponent<EventTrigger>();

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
        Footnote.gameObject.SetActive(a.Equals(0.6f) ? false : true);
    }
    #endregion
}
