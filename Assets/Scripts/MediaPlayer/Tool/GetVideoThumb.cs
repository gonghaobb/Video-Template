using RenderHeads.Media.AVProVideo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using UnityEngine.UI;

public class GetVideoThumb : MonoBehaviour
{
    [SerializeField] private int often;
    [SerializeField] private int index;
    private void Start()
    {
        StartCoroutine(GetCover("https://lf3-static.bytednsdoc.com/obj/eden-cn/pbeh7upsvhpeps/demo/pico/video/movie/001.mp4"));
    }
    public IEnumerator GetCover(string videoFullName)//��ȡ��Ƶ��÷���
    {
        GameObject temp = new GameObject("temp");
        MediaPlayer mediaPlayer = temp.AddComponent<MediaPlayer>();
        mediaPlayer.OpenMedia(MediaPathType.AbsolutePathOrURL, videoFullName, false);
        //�ȴ�һ����Ϊ������Ƶ��ɼ��أ������Ƶ���󣬿����ӳ�ʱ��
        yield return new WaitForSeconds(1);
        //��ȡ1֡����ͼ��
        TimeRange timelineRange = GetTimelineRange(mediaPlayer);
        often = (int)timelineRange.duration;
        Texture2D texture2D = new Texture2D(1920, 1080);
        mediaPlayer.ExtractFrame(texture2D, index);
        File.WriteAllBytes("d:/VideoThumb/Cover" + index + ".png", texture2D.EncodeToPNG());

        //���ٲ�����
        // GameObject.Destroy(temp);
    }

    public IEnumerator GetPNG(string videoFullName,int index)
    {
        GameObject temp = new GameObject("temp");
        MediaPlayer mediaPlayer = temp.AddComponent<MediaPlayer>();
        mediaPlayer.OpenMedia(MediaPathType.AbsolutePathOrURL, videoFullName, false);

        yield return new WaitForSeconds(1f);
        Texture2D texture2D = new Texture2D(1920, 1080);
        mediaPlayer.ExtractFrame(texture2D, index);
        File.WriteAllBytes("d:/VideoThumb/Cover" + index + ".png", texture2D.EncodeToPNG());

        //���ٲ�����
        GameObject.Destroy(temp);
    }

    private TimeRange GetTimelineRange(MediaPlayer mediaPlayer)
    {
        if (mediaPlayer.Info != null)
        {
            return Helper.GetTimelineRange(mediaPlayer.Info.GetDuration(), mediaPlayer.Control.GetSeekableTimes());
        }
        return new TimeRange();
    }
}
