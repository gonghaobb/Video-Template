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
    [SerializeField] XRRayInteractor _xRRayInteractor;//������
    [SerializeField] MediaPlayer _mediaPlayer = null;//������

    [Header("UI Components")]
    [SerializeField] private Slider _timeSlider;
    [SerializeField] private GameObject _progressBar;
    [SerializeField] private RectTransform _fill_value = null;
    [SerializeField] EventTrigger _previewEvent = null;
    [SerializeField] private Image _thumbImage = null;
    [SerializeField] private TMP_Text _previewTimelineText, _textTimeDuration, _textTimeAll;

    private RectTransform _progressBar_rect;
    /****** �������� ******/
    private bool _isHoveringOverTimeline;//�Ƿ��ڽ������� 
    private bool _isTriggleInput;//�Ƿ���triggle

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
    /// slider�����¼�
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
    /// ���� timeSlider
    /// </summary>
    private void OnTimelineBeginHover(PointerEventData eventData)
    {
        if (eventData.pointerCurrentRaycast.gameObject != null)
            _isHoveringOverTimeline = true;
    }

    /// <summary>
    /// �뿪 timeSlider
    /// </summary>
    private void OnTimelineEndHover(PointerEventData eventData)
    {
        _isHoveringOverTimeline = false;
    }


    /// <summary>
    /// Hover ������
    /// </summary>
    public void SetProgressBar()
    {
        //_progressBar ��ʾ����
        if (!_progressBar.activeSelf.Equals(_isHoveringOverTimeline))
            _progressBar.SetActive(_isHoveringOverTimeline);
        //_progressBar ƫ����
        float progressX = _progressBar_rect.anchoredPosition.x;

        if (progressX < -272 || progressX > 272)
            _progressBar.SetActive(false);

        //_progressBar Hover
        if (_isHoveringOverTimeline)
        {
            if (_isTriggleInput && _isPreviewHover) return;

            _progressBar_rect.anchoredPosition = new Vector2(GetHitInfo(), 0);
            TimeRange timelineRange = GetTimelineRange();
            //���½���
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


    #region ����ģ��
    /// <summary>
    /// ��ȡ������ timeSlider ��λ��
    /// </summary>
    /// <returns></returns>
    private float GetHitInfo()
    {
        _xRRayInteractor.TryGetHitInfo(out Vector3 position, out Vector3 normal, out int positionInLine, out bool isValidTarget);
        return (position.x * 832) + 32f;
    }

    /// <summary>
    /// ��ȡԤ��ͼ 
    /// </summary>
    /// <param name="accuracy">hover ����</param>
    /// <param name="duration">video ʱ�� </param>
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
            // ���²��ؽ�����
            if (_timeSlider && !_isHoveringOverTimeline)
            {
                double t = 0.0;
                if (timelineRange.duration > 0.0)
                {
                    t = ((_mediaPlayer.Control.GetCurrentTime() - timelineRange.startTime) / timelineRange.duration);
                }
                _fill_value.sizeDelta = new Vector2(Mathf.Clamp01((float)t) * 544, 46);
            }
            // ���²��Ž���text
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
            //��Ƶ�������޸�
            _fill_value.DOSizeDelta(new Vector2(_timeSlider.value * 544f, 46), 0.1f);
            //��Ƶ�����޸�
            if (_mediaPlayer && _mediaPlayer.Control != null)
            {
                TimeRange timelineRange = GetTimelineRange();
                double time = timelineRange.startTime + (_timeSlider.value * timelineRange.duration);
                _mediaPlayer.Control.Seek(time);
                _isHoveringOverTimeline = true;
            }
            //triggle����������
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
