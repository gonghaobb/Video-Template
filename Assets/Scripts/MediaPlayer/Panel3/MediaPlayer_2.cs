using RenderHeads.Media.AVProVideo;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using UnityEngine.InputSystem;
using DG.Tweening;

public class MediaPlayer_2 : MonoBehaviour
{
    /**常数**/
    [HideInInspector] private const float _sliderSeizeDeltaX = 544, _sliderSeizeDeltaY = 46;
    [HideInInspector] private Slider _sliderTime;

    [Header("UI Components")]
    [SerializeField] XRRayInteractor xRRayInteractor;//控制器
    [SerializeField] MediaPlayer _mediaPlayer = null;//播放器
    [SerializeField] RectTransform _fill_value = null;
    [SerializeField] TMP_Text _textTimeDuration, _textTimeAll;
    [SerializeField] GameObject _progressBar = null;
    [HideInInspector] RectTransform _previewPanel = null;
    [SerializeField] EventTrigger _previewEvent = null;
    [SerializeField] GameObject _previewImageParent = null;
    [SerializeField] TMP_Text _previewTimelineText = null;
    [SerializeField] Image _thumbImage = null;
    [HideInInspector] RectTransform _progressBar_rect;
    /** **/
    private bool _isHoveringOverTimeline = false;
   [HideInInspector] public bool _isTriggleInput = false;
    private void Start()
    {
        Init();
        CreateTimelineDragEvents();
        CreatePreviewEvents();
    }
    private void Update()
    {
        SetProgressBar();
        UpdateTimeLine();
        SetPreviewPanel();
    }
    private void Init()
    {
        _sliderTime = GetComponent<Slider>();
        _progressBar_rect = _progressBar.GetComponent<RectTransform>();
        _previewPanel = _progressBar.GetComponentInChildren<Mask>(false).rectTransform;
        _previewImages = _previewImageParent.GetComponentsInChildren<Image>();
        motion = GetComponent<GetMotion>();
    }
    /// <summary>
    /// slider触发事件
    /// </summary>
    private void CreateTimelineDragEvents()
    {
        EventTrigger trigger = _sliderTime.gameObject.GetComponent<EventTrigger>();
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
    private void OnTimelineBeginHover(PointerEventData eventData)
    {
        if (eventData.pointerCurrentRaycast.gameObject != null)
            _isHoveringOverTimeline = true;
    }
    private void OnTimelineEndHover(PointerEventData eventData)
    {
        _isHoveringOverTimeline = false;
    }

    private float GetHitInfo()
    {
        xRRayInteractor.TryGetHitInfo(out Vector3 position, out Vector3 normal, out int positionInLine, out bool isValidTarget);
        return (position.x * 832)/* + 32f*/;
    }

    /// <summary>
    /// Hover 进度条
    /// </summary>
    public void SetProgressBar()
    {
        //_progressBar 显示隐藏
        if (_isHoveringOverTimeline)
        {
            _progressBar.SetActive(_isHoveringOverTimeline);
        }
        if (!_isHoveringOverTimeline && !_isPreviewHover)
        {
            _progressBar.SetActive(false);
        }
        //_progressBar 偏移量
        float progressX = _progressBar_rect.anchoredPosition.x;
        if (progressX < -272 || progressX > 272)
        {
            _progressBar.SetActive(false);
        }

        //_progressBar Hover
        if (_isHoveringOverTimeline)
        {
            if (_isTriggleInput && _isStopMotion) return;

            _progressBar_rect.anchoredPosition = new Vector2(GetHitInfo(), -300);
            TimeRange timelineRange = GetTimelineRange();
            //更新进度
            if (_isTriggleInput) GetThumbImage(_sliderTime.value * timelineRange.duration, timelineRange.duration);
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
            if (_sliderTime && !_isHoveringOverTimeline)
            {
                double t = 0.0;
                if (timelineRange.duration > 0.0)
                {
                    t = ((_mediaPlayer.Control.GetCurrentTime() - timelineRange.startTime) / timelineRange.duration);
                }
                _fill_value.sizeDelta = new Vector2(Mathf.Clamp01((float)t) * _sliderSeizeDeltaX, _sliderSeizeDeltaY);
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
                _sliderTime.value = 0;
                return;
            }
            //视频进度条修改
            _fill_value.DOSizeDelta(new Vector2(_sliderTime.value * 544f, 46), 0.1f);
            //视频进度修改
            if (_mediaPlayer && _mediaPlayer.Control != null)
            {
                TimeRange timelineRange = GetTimelineRange();
                double time = timelineRange.startTime + (_sliderTime.value * timelineRange.duration);
                _mediaPlayer.Control.Seek(time);
                _isHoveringOverTimeline = true;
            }
            //triggle进度条归零
            _sliderTime.value = 0;
        }
    }
    public void OnEnable()
    {
        _triggleActionDown.Enable();
        _triggleActionUp.Enable();
        MessageController.ObserverEvent += this.ParseData;
    }
    public void OnDisable()
    {
        _triggleActionDown.Disable();
        _triggleActionUp.Enable();
        MessageController.ObserverEvent -= this.ParseData;
    }
    #endregion

    #region previewEvent 
    public Image[] _previewImages;
    private bool _isStopMotion = false;
    private bool _isPreviewScaleMax = false;
    private bool _isPreviewHover = false;
    private int thumbProgress;
    private GetMotion motion;

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

    private bool SetPreviewPanelScale(Vector2 sca)
    {
        bool isMax = sca.Equals(new Vector2(2212, 129)) ? true : false;
        Vector2 pos = isMax ? new Vector2(0, -120) : Vector2.zero;
        _isStopMotion = isMax;
        _previewPanel.DOAnchorPos(pos, 0.1f);
        _previewPanel.DOSizeDelta(sca, 0.1f);
        _sliderTime.interactable = !isMax;
        return isMax;
    }

    private void SetPreviewPanel()
    {
        if (_isTriggleInput)
        {
            if (_isHoveringOverTimeline)
            {
                if (motion.GetBackTrack)
                {
                    SetPraviewScale(true);
                }
            }
        }
        else
        {
            SetPraviewScale(false);
        }
    }

    private void SetPraviewScale(bool isMax)
    {
        if (isMax)
        {
            if (_isPreviewScaleMax) return;

            _isPreviewScaleMax = SetPreviewPanelScale(new Vector2(2212f, 129f));
            float hoverX = (_progressBar_rect.anchoredPosition.x + 272) / 544;
            //向前取六个缩略图
            int midIndex = (int)Math.Round(147 * hoverX) - 6;
            for (int i = 0; i < _previewImages.Length; i++)
            {
                int index = i;
                _previewImages[index].sprite = GetVideoThumbSprite.GetSprites((midIndex + index).ToString());
            }
            string tempTime = _previewTimelineText.text.Split('/')[0];
            thumbProgress = int.Parse(tempTime.Split(':')[0]) * 60 + int.Parse(tempTime.Split(':')[1]);
        }
        else
        {
            if (_isPreviewScaleMax)
            {
                _isPreviewScaleMax = SetPreviewPanelScale(new Vector2(172f, 129f));
            }
        }
    }

    private void ParseData(string str)
    {
        if (str.StartsWith("shifting_"))
        {
            string end = str.Split('_')[1];
            int temp = Convert.ToInt32(end);
            _previewTimelineText.text = Helper.GetTimeString(thumbProgress + temp, false) + "/" + Helper.GetTimeString(147, false);
        }
    }
    #endregion
}
