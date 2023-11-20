using System;
using UnityEngine;
using UnityEngine.UI;

public class GetMotion : MonoBehaviour
{
    private MediaPlayer_2 mediaPlayer;
    private Slider slider;
    private float[] backTrackTime = new float[3];
    private float dis1, dis2;
    private bool isBackTrack = false;

    public bool GetBackTrack
    {
        get
        {
            return isBackTrack;
        }
    }

    
    private void Start()
    {
        slider = GetComponent<Slider>();
        mediaPlayer = GetComponent<MediaPlayer_2>();
        InvokeRepeating("SetListValue", 1, 1);
    }

    private void SetListValue()
    {
        backTrackTime[0] = backTrackTime[1];
        backTrackTime[1] = backTrackTime[2];
        backTrackTime[2] = slider.value;

        if (mediaPlayer._isTriggleInput)
        {
            dis1 = Math.Abs(backTrackTime[0] - backTrackTime[1]);
            dis2 = Math.Abs(backTrackTime[1] - backTrackTime[2]);
        }
        else
        {
            dis1 = 1; dis2 = 1f;
        }
    }

    private void Update()
    {

            if (dis1 < 0.1f && dis2 < 0.1f && slider.value >= 0.11f)
            {
                isBackTrack = true;
            }
            else
            {
                isBackTrack = false;
            }
        

    }

}
