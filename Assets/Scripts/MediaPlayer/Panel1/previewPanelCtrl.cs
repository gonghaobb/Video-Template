using DG.Tweening;
using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class previewPanelCtrl : MonoBehaviour
{
    public XRRayInteractor xRRayInteractor;
    public Slider timeSlider;
    private timeSliderCtrl timeSliderCtrl;

    private void Start()
    {
        timeSliderCtrl = timeSlider.GetComponent<timeSliderCtrl>();
    }
    void Update()
    {
        SetPreviewPanelScale();
    }
    private void SetPreviewPanelScale()
    {
        float progressX = GetComponent<RectTransform>().sizeDelta.x;

        if (progressX>200f)
        {
            xRRayInteractor.TryGetHitInfo(out Vector3 position, out Vector3 normal, out int positionInLine, out bool isValidTarget);
            // image.anchoredPosition = new Vector2(position.x * 832 + 32f, 0);

            Vector2 vector2 = GetHitInfo();
            timeSlider.value = vector2.x / 2212f+0.5f;
            timeSliderCtrl.tempPos =new Vector2(vector2.x / 2212f * 544,0);
        }
    }


    private Vector2 GetHitInfo()
    {
        xRRayInteractor.TryGetHitInfo(out Vector3 position, out Vector3 normal, out int positionInLine, out bool isValidTarget);
        return new Vector2(position.x * 832 + 32f, 0);
    }
}
