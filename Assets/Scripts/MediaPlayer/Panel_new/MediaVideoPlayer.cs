using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using RenderHeads.Media.AVProVideo;
using UnityEngine.InputSystem;

public class MediaVideoPlayer : MonoBehaviour
{
    [Header("XR Components")]
    [SerializeField] XRRayInteractor _xRRayInteractor;//控制器
    [SerializeField] MediaPlayer _mediaPlayer = null;//播放器

    [Header("UI Components")]
    [SerializeField] private Slider _timeSlider;
    [SerializeField] private GameObject _progressBar;
    [SerializeField] private RectTransform _fill_value = null;
    [SerializeField] EventTrigger _previewEvent = null;
    [SerializeField] private Image _thumbImage = null;
    [SerializeField] private TMP_Text _previewTimelineText, _textTimeDuration, _textTimeAll;

    private RectTransform _progressBar_rect;
    /****** 触发条件 ******/
    private bool _isHoveringOverTimeline;//是否在进度条上 
    private bool _isTriggleInput;//是否按下triggle

    void Start()
    {
        Init();
        CreateTimelineDragEvents();
        CreatePreviewEvents();
    }
    private void Update()
    {
        SetProgressBar();
        UpdateTimeLine();
    }
    private void Init()
    {
        _progressBar_rect = _progressBar.GetComponent<RectTransform>();
        //_previewPanel = _progressBar.GetComponentInChildren<Mask>(false).rectTransform;
        //_previewImages = _previewImageParent.GetComponentsInChildren<Image>();
    }
    /// <summary>
    /// slider触发事件
    /// </summary>
    private void CreateTimelineDragEvents()
    {
        EventTrigger trigger = _timeSlider.gameObject.GetComponent<EventTrigger>();
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

    /// <summary>
    /// 进入 timeSlider
    /// </summary>
    private void OnTimelineBeginHover(PointerEventData eventData)
    {
        if (eventData.pointerCurrentRaycast.gameObject != null)
            _isHoveringOverTimeline = true;
    }

    /// <summary>
    /// 离开 timeSlider
    /// </summary>
    private void OnTimelineEndHover(PointerEventData eventData)
    {
        _isHoveringOverTimeline = false;
    }


    /// <summary>
    /// Hover 进度条
    /// </summary>
    public void SetProgressBar()
    {
        //_progressBar 显示隐藏
        if (!_progressBar.activeSelf.Equals(_isHoveringOverTimeline))
            _progressBar.SetActive(_isHoveringOverTimeline);
        //_progressBar 偏移量
        float progressX = _progressBar_rect.anchoredPosition.x;

        if (progressX < -272 || progressX > 272)
            _progressBar.SetActive(false);

        //_progressBar Hover
        if (_isHoveringOverTimeline)
        {
            if (_isTriggleInput && _isPreviewHover) return;

            _progressBar_rect.anchoredPosition = new Vector2(GetHitInfo(), 0);
            TimeRange timelineRange = GetTimelineRange();
            //更新进度
            if (_isTriggleInput) GetThumbImage(_timeSlider.value * timelineRange.duration, timelineRange.duration);
            else
            {
                float hoverX = (progressX + 272) / 544;
                GetThumbImage(hoverX * timelineRange.duration, timelineRange.duration);
            }
        }
    }

    private TimeRange GetTimelineRange()
    {
        if (_mediaPlayer.Info != null)
        {
            return Helper.GetTimelineRange(_mediaPlayer.Info.GetDuration(), _mediaPlayer.Control.GetSeekableTimes());
        }
        return new TimeRange();
    }


    #region 工具模块
    /// <summary>
    /// 获取射线在 timeSlider 的位置
    /// </summary>
    /// <returns></returns>
    private float GetHitInfo()
    {
        _xRRayInteractor.TryGetHitInfo(out Vector3 position, out Vector3 normal, out int positionInLine, out bool isValidTarget);
        return (position.x * 832) + 32f;
    }

    /// <summary>
    /// 获取预览图 
    /// </summary>
    /// <param name="accuracy">hover 进度</param>
    /// <param name="duration">video 时长 </param>
    private void GetThumbImage(double accuracy, double duration)
    {
        _previewTimelineText.text = Helper.GetTimeString(accuracy, false) + "/" + Helper.GetTimeString(duration, false);
        _thumbImage.sprite = GetVideoThumbSprite.GetSprites((accuracy).ToString("f0"));
    }

    private void UpdateTimeLine()
    {
        if (_mediaPlayer.Info != null)
        {
            TimeRange timelineRange = GetTimelineRange();
            // 更新播控进度条
            if (_timeSlider && !_isHoveringOverTimeline)
            {
                double t = 0.0;
                if (timelineRange.duration > 0.0)
                {
                    t = ((_mediaPlayer.Control.GetCurrentTime() - timelineRange.startTime) / timelineRange.duration);
                }
                _fill_value.sizeDelta = new Vector2(Mathf.Clamp01((float)t) * 544, 46);
            }
            // 更新播放进度text
            if (_textTimeDuration)
            {
                double playbackDuration = _mediaPlayer.Control.GetCurrentTime() - timelineRange.startTime;
                string t1 = Helper.GetTimeString(playbackDuration, false);
                string d1 = Helper.GetTimeString(timelineRange.duration - playbackDuration, false);
                _textTimeDuration.text = t1;
                _textTimeAll.text = d1;
            }
        }
    }

    #endregion

    #region Action
    [SerializeField] InputAction _triggleActionDown, _triggleActionUp;
    private void Awake()
    {
        _triggleActionDown.performed += ctx => { OnTriggleDown(ctx); };
        _triggleActionUp.performed += ctx => { OnTriggleUp(ctx); };
    }

    private void OnTriggleDown(InputAction.CallbackContext ctx)
    {
        _isTriggleInput = true;
    }
    private void OnTriggleUp(InputAction.CallbackContext ctx)
    {
        _isTriggleInput = false;

        if (ctx.performed)
        {
            if (!_isHoveringOverTimeline)
            {
                _timeSlider.value = 0;
                return;
            }
            //视频进度条修改
            _fill_value.DOSizeDelta(new Vector2(_timeSlider.value * 544f, 46), 0.1f);
            //视频进度修改
            if (_mediaPlayer && _mediaPlayer.Control != null)
            {
                TimeRange timelineRange = GetTimelineRange();
                double time = timelineRange.startTime + (_timeSlider.value * timelineRange.duration);
                _mediaPlayer.Control.Seek(time);
                _isHoveringOverTimeline = true;
            }
            //triggle进度条归零
            _timeSlider.value = 0;
        }
    }
    public void OnEnable()
    {
        _triggleActionDown.Enable();
        _triggleActionUp.Enable();
    }
    public void OnDisable()
    {
        _triggleActionDown.Disable();
        _triggleActionUp.Enable();
    }
    #endregion

    #region previewEvent 
    public Image[] _previewImages;
    private bool _isPreviewHover = false;
    private int thumbProgress;
    private void CreatePreviewEvents()
    {
        if (_previewEvent != null)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry();

            entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerEnter;
            entry.callback.AddListener((data) => { OnPreviewEventBeginHover((PointerEventData)data); });
            _previewEvent.triggers.Add(entry);
            entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerExit;
            entry.callback.AddListener((data) => { OnPreviewEventEndHover((PointerEventData)data); });
            _previewEvent.triggers.Add(entry);
        }
    }

    private void OnPreviewEventBeginHover(PointerEventData data)
    {
        _isPreviewHover = true;
    }

    private void OnPreviewEventEndHover(PointerEventData data)
    {
        _isPreviewHover = false;
    }
    #endregion
}
