using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class BarController : MonoBehaviour
{
    #region
    private RectTransform navBar;
    public CanvasGroup canvasGroup;//
    public GameObject minTextGroup;//mini态
    private TMP_Text miniBarName;//mini态 文字
    private int tempModel = 1;//当前模式 1-6 首页、直播、全景、抖音、搜索、更多

    [SerializeField]
    private Vector2 mainBar = new Vector2(513, 84) , miniBar = new Vector2(84, 52);
    private float LEAVE_TIMER = 3f, METER_TIMER;//计时时间 当前累加时间
    private bool isMiniBar;//是否是mini状态
    #endregion

    private void Awake()
    {
        navBar = GetComponent<RectTransform>();
        miniBarName = minTextGroup.GetComponentInChildren<TMP_Text>(false);
        EventTrigger trigger = minTextGroup.GetComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        entry.callback.AddListener((data) => { OnPointerEnterDelegate((PointerEventData)data); });
        trigger.triggers.Add(entry);
    }
    private void Start()
    {
        Init();
    }

    private void OnPointerEnterDelegate(PointerEventData data)
    {
        StartCoroutine(SetMainBar());
    }

    private void Init()
    {
        canvasGroup.GetComponentInChildren<Toggle>().isOn = true;
    }
    private void Update()
    {

        if (tempModel.Equals(2) || tempModel.Equals(3) || tempModel.Equals(4))
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                if (!METER_TIMER.Equals(0)) METER_TIMER = 0;
                return;
            }

            if (!isMiniBar)
            {
                METER_TIMER += Time.deltaTime;
                if (METER_TIMER >= LEAVE_TIMER)
                {
                    SetMiniBar();
                    SetMiniBarValue();
                    METER_TIMER = 0f;
                }
            }
        }
    }
    private void ParseData(string str)
    {
        if (str.StartsWith("show_"))
        {
            string end = str.Substring(str.Length - 1, 1);
            tempModel =int.Parse(end);
        }
    }

    private void  SetMiniBar()
    {
        canvasGroup.DOFade(0f, 0.1f).OnComplete(()=> {
            minTextGroup.gameObject.SetActive(true);

            navBar.DOSizeDelta(miniBar, 0.4f);
            isMiniBar = true;
        });
    }

    IEnumerator SetMainBar()
    {
        if (isMiniBar)
        {
            minTextGroup.SetActive(false);
            yield return new WaitForSeconds(0.1f);
            navBar.DOSizeDelta(mainBar, 0.4f);
            canvasGroup.DOFade(1f, 0.4f).OnComplete(() =>
            {
                isMiniBar = false;
            }); 
        }
    }
    private void SetMiniBarValue()
    {
        miniBarName.text = GetValue(tempModel);
    }
    private string GetValue(int index)
    {
        switch (index)
        {
            case 2:
                return "直播";
            case 3:
                return "全景";
            case 4:
                return "抖音";
            default:
                return string.Empty;
        }
    }
    private void OnEnable()
    {
        MessageController.ObserverEvent += this.ParseData;
    }
    private void OnDisable()
    {
        MessageController.ObserverEvent -= this.ParseData;
    }
}
