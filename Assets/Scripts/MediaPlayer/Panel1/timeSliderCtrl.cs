using RenderHeads.Media.AVProVideo;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using DG.Tweening;

public class timeSliderCtrl : MonoBehaviour
{
	/**常数**/
	[HideInInspector]private const float _sliderSeizeDeltaX = 544, _sliderSeizeDeltaY = 46;
	[SerializeField] private Slider _sliderTime;
	[SerializeField] private RectTransform previewEvent;

	[Header("UI Components")]
	[SerializeField] XRRayInteractor xRRayInteractor;//控制器
	[SerializeField] MediaPlayer _mediaPlayer = null;//播放器
	[SerializeField] GameObject _progressBar = null;//hover预览进度条
	[SerializeField] GameObject _previewPanel = null;//hover预览画面
	[SerializeField] RectTransform _fill = null;//视频进度条
	[SerializeField] TMP_Text _textTimeDuration = null, _textTimeAll = null;//播放时长，剩余时长
	/****/
	private TMP_Text _previewTimelineText;
	public Image _thumbImage;

	[Header("Input Action")]
	[SerializeField] InputAction _triggleActionDown, _triggleActionUp;
	private bool _wasPlayingBeforeTimelineDrag;
	private bool _isHoveringOverTimeline;//是否hover 进度条
	private bool _isTriggleInput;
	private bool _isHoveringPreview;//是否hover预览画面
	private void Awake()
	{
		_triggleActionDown.performed += ctx => { OnTriggleDown(ctx); };
		_triggleActionUp.performed += ctx => { OnTriggleUp(ctx); };
	}

	private void Start()
    {
		_previewTimelineText = _previewPanel.GetComponentInChildren<TMP_Text>(false);

		CreateTimelineDragEvents();
		CreatePreviewHoverEvents();
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
	private void Update()
    {
		SetProgressBar();
		UpdateTimeLine();
		SetPreviewPanelScale();
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
			entry.eventID = EventTriggerType.PointerDown;
			entry.callback.AddListener((data) => { OnTimeSliderBeginDrag(); });
			trigger.triggers.Add(entry);

			entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.Drag;
			entry.callback.AddListener((data) => { OnTimeSliderDrag(); });
			trigger.triggers.Add(entry);

			entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.PointerUp;
			entry.callback.AddListener((data) => { OnTimeSliderEndDrag(); });
			trigger.triggers.Add(entry);

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
		{
			_isHoveringOverTimeline = true;
		}
	}

	private void OnTimelineEndHover(PointerEventData eventData)
	{
		_isHoveringOverTimeline = false;
	}

	/// <summary>
	/// 按下
	/// </summary>
	private void OnTimeSliderBeginDrag()
	{
	}

	private void OnTimeSliderDrag()
    {
	}

    private void OnTimeSliderEndDrag()
    {
	}



    private float GetHitInfo()
	{
		xRRayInteractor.TryGetHitInfo(out Vector3 position, out Vector3 normal, out int positionInLine, out bool isValidTarget);
		return (position.x * 832) + 32f;
	}

	/// <summary>
	/// Hover 进度条
	/// </summary>
	public void SetProgressBar()
	{
		//_progressBar 显示隐藏
		if (!_progressBar.activeSelf.Equals(_isHoveringOverTimeline))
		{
			_progressBar.SetActive(_isHoveringOverTimeline);
		}
		//_progressBar 偏移量
		float progressX = _progressBar.GetComponent<RectTransform>().anchoredPosition.x;
		if (progressX < -272 || progressX > 272)
		{
			_progressBar.SetActive(false);
		}
		//previewPanel 显示隐藏
		if (!_previewPanel.activeSelf.Equals(_progressBar.activeSelf))
		{
			_previewPanel.SetActive(_progressBar.activeSelf);
		}

		//_progressBar Hover
		if (_isHoveringOverTimeline)
		{
			_progressBar.GetComponent<RectTransform>().anchoredPosition = new Vector2(GetHitInfo(), 0);
			TimeRange timelineRange = GetTimelineRange();
			if (_previewPanel.GetComponent<RectTransform>().sizeDelta.x < 200f)
			{
				//更新进度
				if (_isTriggleInput) GetThumbImage(_sliderTime.value * timelineRange.duration, timelineRange.duration);
				else
				{
					float hoverX = (progressX + 272) / 544;
					GetThumbImage(hoverX * timelineRange.duration, timelineRange.duration);
				}
				//预览面板跟随射线
				_previewPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(progressX, 0);
            }
            else
            {
				_previewPanel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
				//更新进度
				if (_isTriggleInput) GetThumbImage(_sliderTime.value * timelineRange.duration, timelineRange.duration);
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
			_fill.DOSizeDelta(new Vector2(_sliderTime.value * 544f, 46), 0.1f);
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

	private void OnTriggleDown(InputAction.CallbackContext ctx)
	{
		_isTriggleInput = true;
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
				_fill.sizeDelta = new Vector2(Mathf.Clamp01((float)t) * _sliderSeizeDeltaX, _sliderSeizeDeltaY);
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


	#region 
	[HideInInspector]public Vector2 tempPos;
	private void CreatePreviewHoverEvents()
	{
		EventTrigger trigger = previewEvent.GetComponent<EventTrigger>();
		if (trigger != null)
		{
			EventTrigger.Entry entry = new EventTrigger.Entry();

			entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.PointerEnter;
			entry.callback.AddListener((data) => { OnPreviewBeginHover((PointerEventData)data); });
			trigger.triggers.Add(entry);

			entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.PointerExit;
			entry.callback.AddListener((data) => { OnPreviewEndHover((PointerEventData)data); });
			trigger.triggers.Add(entry);
		}
	}

	private void OnPreviewBeginHover(PointerEventData data)
	{
		tempPos = _progressBar.GetComponent<RectTransform>().anchoredPosition;
		_isHoveringPreview = true;
	}
	private void OnPreviewEndHover(PointerEventData data)
	{
		_isHoveringPreview = false;
	}

	private void SetPreviewPanelScale()
    {
        if (_isTriggleInput && _isHoveringPreview)
        {
            _sliderTime.interactable = false;
			_progressBar.GetComponent<RectTransform>().anchoredPosition = tempPos;
			_previewPanel.GetComponent<RectTransform>().DOSizeDelta(new Vector2(2212f, 129f), 0.1f);
		}
        else
        {
			_previewPanel.GetComponent<RectTransform>().DOSizeDelta(new Vector2(172f, 129f), 0.1f);
			_sliderTime.interactable = true;
		}
	}
	#endregion

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
}
