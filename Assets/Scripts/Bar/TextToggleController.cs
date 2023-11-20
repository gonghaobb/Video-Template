using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;
using TMPro;

public class TextToggleController : MonoBehaviour
{
    private Toggle toggle;
    private Image checkmark;//复选标记
    private TMP_Text toggleValue;//toggle名称
    private EventTrigger eventTrigger;//事件触发器
    private float animaTime = 0.1f;//持续时间
    /// <summary>
    /// 选中颜色 hover颜色 退出颜色
    /// </summary>
    private Color selectedColor = new Color(1f, 1f, 1f, 0.8f), hoverColor = new Color(1f, 1f, 1f, 0.3f), exitColor = new Color(1f, 1f, 1f, 0.0f);
    private bool isOn;//当前是否为选中状态
    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        eventTrigger = GetComponent<EventTrigger>();
        checkmark = GetComponentInChildren<Image>();
        toggleValue = GetComponentInChildren<TMP_Text>();
        toggle.onValueChanged.AddListener(OnSelected);
    }

    void Start()
    {
        EventTrigger.Entry onEnter = new EventTrigger.Entry();
        onEnter.eventID = EventTriggerType.PointerEnter;
        onEnter.callback.AddListener((eventData) => { OnEnter(); });
        eventTrigger.triggers.Add(onEnter);

        EventTrigger.Entry onExit = new EventTrigger.Entry();
        onExit.eventID = EventTriggerType.PointerExit;
        onExit.callback.AddListener((eventData) => { OnExit(); });
        eventTrigger.triggers.Add(onExit);
    }
    private void OnExit()
    {
        checkmark.DOColor(isOn.Equals(true) ? selectedColor : exitColor, animaTime);
    }

    private void OnEnter()
    {
        checkmark.DOColor(isOn.Equals(true) ? selectedColor : hoverColor, animaTime);
    }

    private void OnSelected(bool arg0)
    {
        checkmark.DOColor(arg0.Equals(true) ? selectedColor : exitColor, animaTime);
        toggleValue.DOColor(arg0.Equals(true) ? Color.black : Color.white, animaTime);
        MessageController.GetInstance().SetMessage(arg0.Equals(true) ? "show_" + name : "hide_" + name);
        isOn = arg0;
    }
}
