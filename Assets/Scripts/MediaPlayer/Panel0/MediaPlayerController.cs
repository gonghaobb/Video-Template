using RenderHeads.Media.AVProVideo;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using DG.Tweening;

public class MediaPlayerController : MonoBehaviour
{
	/**����**/
	private const float _sliderSeizeDeltaX = 544, _sliderSeizeDeltaY = 46;

	[Header("UI Components")]
	[SerializeField]  XRRayInteractor xRRayInteractor;//������
	[SerializeField] MediaPlayer _mediaPlayer = null;//������
	[SerializeField] Slider _sliderTime = null;//���Ž�����
	[SerializeField] RectTransform _fill = null;//��Ƶ������

	[Header("UI Components (Optional)")]
	[SerializeField] Button _buttonPlayPause = null;
	[SerializeField] GameObject _progressBar = null;//hoverԤ��ģ��
	[SerializeField] TMP_Text _textTimeDuration = null, _textTimeAll = null;//����ʱ����ʣ��ʱ��

	[Header("Action")]
	[SerializeField] InputAction _triggleActionDown, _triggleActionUp;
	private bool _wasPlayingBeforeTimelineDrag;
	private bool _isHoveringOverTimeline;//�Ƿ�hover ������

    private void Awake()
    {
		_triggleActionDown.performed += ctx => { OnTriggleDown(ctx); };
		_triggleActionUp.performed += ctx => { OnTriggleUp(ctx); };
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

	void Start()
	{
		InitPlayerInfo();
		CreateTimelineDragEvents();
	}

    private void Update()
    {
		SetProgressBar();

		if (_mediaPlayer.Info != null)
		{
			TimeRange timelineRange = GetTimelineRange();
			// ���²��ؽ�����
			if (_sliderTime && !_isHoveringOverTimeline)
			{
				double t = 0.0;
				if (timelineRange.duration > 0.0)
				{
					t = ((_mediaPlayer.Control.GetCurrentTime() - timelineRange.startTime) / timelineRange.duration);
				}
			    _fill.sizeDelta =new Vector2(Mathf.Clamp01((float)t)* _sliderSeizeDeltaX, _sliderSeizeDeltaY);
			}
			// ���²��Ž���text
			if (_textTimeDuration)
			{
				double playbackDuration = _mediaPlayer.Control.GetCurrentTime() - timelineRange.startTime;
				string t1 = Helper.GetTimeString(playbackDuration, false);
				string d1 = Helper.GetTimeString(timelineRange.duration- playbackDuration, false);
				_textTimeDuration.text = t1;
				_textTimeAll.text = d1;
			}
		}
	}
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

	/// <summary>
	/// Hover ������
	/// </summary>
	public void SetProgressBar()
    {
        if (!_progressBar.activeSelf.Equals(_isHoveringOverTimeline))
        {
			_progressBar.SetActive(_isHoveringOverTimeline);
		}
        if (_isHoveringOverTimeline)
        {
			_progressBar.GetComponent<RectTransform>().anchoredPosition = new Vector2(GetHitInfo(), 0);
			TimeRange timelineRange = GetTimelineRange();
            if (_progressBar.GetComponentInChildren<Mask>().rectTransform.sizeDelta.x<200f)
			{
				TMP_Text _Text = _progressBar.GetComponentInChildren<TMP_Text>();
				_Text.text = Helper.GetTimeString(_sliderTime.value * timelineRange.duration, false) + "/" + Helper.GetTimeString(timelineRange.duration, false); 
			}
		}
	}

	private void OnTimeSliderBeginDrag()
	{
		if (_mediaPlayer && _mediaPlayer.Control != null)
		{
			_wasPlayingBeforeTimelineDrag = _mediaPlayer.Control.IsPlaying();
			if (_wasPlayingBeforeTimelineDrag)
			{
				//_mediaPlayer.Pause();
			}
			OnTimeSliderDrag();
		}
	}
	private void OnTimeSliderEndDrag()
	{
		if (_mediaPlayer && _mediaPlayer.Control != null)
		{
			if (_wasPlayingBeforeTimelineDrag)
			{
				//_mediaPlayer.Play();
				_wasPlayingBeforeTimelineDrag = false;
			}
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
	private void OnTimeSliderDrag()
	{
		if (_mediaPlayer && _mediaPlayer.Control != null)
		{

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
	private float GetHitInfo()
	{
        if (_isHoveringOverTimeline)
        {
			xRRayInteractor.TryGetHitInfo(out Vector3 position, out Vector3 normal, out int positionInLine, out bool isValidTarget);
			return (position.x * 832) + 32f;
        }
        else
        {
			return 0;
        }
	}

	/// <summary>
	/// ��ʼ��
	/// </summary>
	private void InitPlayerInfo()
    {
		if (_buttonPlayPause)
		{
			_buttonPlayPause.onClick.AddListener(TogglePlayPause);
		}
	}

	public void TogglePlayPause()
	{
		if (_mediaPlayer && _mediaPlayer.Control != null)
		{
			if (_mediaPlayer.Control.IsPlaying())
			{
				if (_mediaPlayer && _mediaPlayer.Control != null) _mediaPlayer.Pause();
			}
			else
			{
				if (_mediaPlayer && _mediaPlayer.Control != null) _mediaPlayer.Play();
			}
		}
	}

	/// <summary>
	/// Triggle
	/// </summary>
	/// <param name="ctx"></param>
	private void OnTriggleDown(InputAction.CallbackContext ctx)
	{
		if (ctx.performed)
        {
            if (_isHoveringOverTimeline)
            {
				//����Ԥ������
			}
        }
	}
	/// <summary>
	/// Trigglȩ��
	/// </summary>
	/// <param name="ctx"></param>
	private void OnTriggleUp(InputAction.CallbackContext ctx)
	{
		if (ctx.performed)
		{
			if (!_isHoveringOverTimeline)
			{
				_sliderTime.value = 0;
				return;
			}
			//��Ƶ�������޸�
			_fill.DOSizeDelta( new Vector2( _sliderTime.value * 544f,46),0.1f);
			//��Ƶ�����޸�
			if (_mediaPlayer && _mediaPlayer.Control != null)
			{
                TimeRange timelineRange = GetTimelineRange();
                double time = timelineRange.startTime + (_sliderTime.value * timelineRange.duration);
                _mediaPlayer.Control.Seek(time);
                _isHoveringOverTimeline = true;
            }
			//triggle����������
			_sliderTime.value = 0;
		}
	}
}
