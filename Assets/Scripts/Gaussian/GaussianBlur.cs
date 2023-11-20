using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GaussianBlur : MonoBehaviour
{
    public Material GaussianBlurMat;
    public float blurSpread =2;
    public int iterations =10;
    public RenderTexture src, dest;
    private void Start()
    {
        GaussianBlurProcess(src, dest, 32, 32);

    }

    private void Update()
    {
        GaussianBlurProcess(src, dest, 500  , 500);
    }

    void GaussianBlurProcess(RenderTexture src, RenderTexture dest, int rtW, int rtH)
    {
        //完整的高斯模糊过程，先降采样，再高斯模糊
        RenderTexture buffer0 = RenderTexture.GetTemporary(rtW, rtH, 0);
        buffer0.filterMode = FilterMode.Trilinear;
        Graphics.Blit(src, buffer0);
        for (int i = 0; i < iterations; i++)
        {
            GaussianBlurMat.SetFloat("_BlurSize", blurSpread);
            RenderTexture buffer1 = RenderTexture.GetTemporary(rtW, rtH, 0);

            //Render the vertical pass
            Graphics.Blit(buffer0, buffer1, GaussianBlurMat, 0);

            RenderTexture.ReleaseTemporary(buffer0);
            buffer0 = buffer1;
            buffer1 = RenderTexture.GetTemporary(rtW, rtH, 0);

            //Render the horizontal pass
            Graphics.Blit(buffer0, buffer1, GaussianBlurMat, 1);

            RenderTexture.ReleaseTemporary(buffer0);
            buffer0 = buffer1;
         }
        Graphics.Blit(buffer0, dest);
        RenderTexture.ReleaseTemporary(buffer0);
    }
}
