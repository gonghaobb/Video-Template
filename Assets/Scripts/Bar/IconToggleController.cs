using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;
using TMPro;

public class IconToggleController : MonoBehaviour
{
    private Toggle toggle;
    private Image checkmark;//��ѡ���
    public Image icon;//toggle����
    private EventTrigger eventTrigger;//�¼�������
    private float animaTime = 0.1f;//����ʱ��
    /// <summary>
    /// ѡ����ɫ hover��ɫ �˳���ɫ
    /// </summary>
    private Color selectedColor = new Color(1f, 1f, 1f, 0.8f), hoverColor = new Color(1f, 1f, 1f, 0.3f), exitColor = new Color(1f, 1f, 1f, 0.0f);
    private bool isOn;//��ǰ�Ƿ�Ϊѡ��״̬
    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        eventTrigger = GetComponent<EventTrigger>();
        checkmark = GetComponentInChildren<Image>();
        icon = GetComponentsInChildren<Image>()[1];
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
        icon.DOColor(arg0.Equals(true) ? Color.black : Color.white, animaTime);
        MessageController.GetInstance().SetMessage(arg0.Equals(true) ? "show_" + name : "hide_" + name);
        isOn = arg0;
    }
}
