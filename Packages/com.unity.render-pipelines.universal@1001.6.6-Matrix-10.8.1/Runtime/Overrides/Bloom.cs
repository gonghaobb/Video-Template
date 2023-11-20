using System;

namespace UnityEngine.Rendering.Universal
{
    // PicoVideo;Bloom;ZhouShaoyang;Begin
    public enum BloomIterationMode
    {
        Fixed = 0,
        Skip = 1
    }    
    public enum BloomMaskMode
    {
        Off = 0,
        DepthStencil_Forward = 1,
        DepthStencil_Reverse = 2,
        Alpha_Forward = 3,
        Alpha_Reverse = 4
    }
    // PicoVideo;Bloom;ZhouShaoyang;End
    [Serializable, VolumeComponentMenu("Post-processing/Bloom")]
    public sealed class Bloom : VolumeComponent, IPostProcessComponent
    {
        //PicoVideo;CustomBloom;ZhouShaoyang;Begin
        [Tooltip("勾选的话为优化后的Bloom，否则为默认Bloom")]
        public BoolParameter isCustomBloom = new BoolParameter(false);
        [Tooltip("Fixed表示直接指定迭代次数，Skip表示依据默认计算得到的迭代次数的基础上减去多少次数")]
        public BloomIterationModeParameter iterationMode = new BloomIterationModeParameter(BloomIterationMode.Fixed);
        [Tooltip("直接指定的迭代次数")]
        public IntParameter fixedIterations = new ClampedIntParameter(4, 1, 8);
        [Tooltip("Mask方式，Stencil走蒙版mask，alpha走颜色a通道做mask，Forward和Reserve表示正反两种Mask")]
        public BloomMaskModeParameter maskMode = new BloomMaskModeParameter(BloomMaskMode.Off);
        //PicoVideo;CustomBloom;ZhouShaoyang;End
        [Tooltip("Filters out pixels under this level of brightness. Value is in gamma-space.")]
        public MinFloatParameter threshold = new MinFloatParameter(0.9f, 0f);

        [Tooltip("Strength of the bloom filter.")]
        public MinFloatParameter intensity = new MinFloatParameter(0f, 0f);

        [Tooltip("Changes the extent of veiling effects.")]
        public ClampedFloatParameter scatter = new ClampedFloatParameter(0.7f, 0f, 1f);

        [Tooltip("Clamps pixels to control the bloom amount.")]
        public MinFloatParameter clamp = new MinFloatParameter(65472f, 0f);

        [Tooltip("Global tint of the bloom filter.")]
        public ColorParameter tint = new ColorParameter(Color.white, false, false, true);

        [Tooltip("Use bicubic sampling instead of bilinear sampling for the upsampling passes. This is slightly more expensive but helps getting smoother visuals.")]
        public BoolParameter highQualityFiltering = new BoolParameter(false);

        [Tooltip("The number of final iterations to skip in the effect processing sequence.")]
        public ClampedIntParameter skipIterations = new ClampedIntParameter(1, 0, 16);

        [Tooltip("Dirtiness texture to add smudges or dust to the bloom effect.")]
        public TextureParameter dirtTexture = new TextureParameter(null);

        [Tooltip("Amount of dirtiness.")]
        public MinFloatParameter dirtIntensity = new MinFloatParameter(0f, 0f);

        public bool IsActive()=>intensity.value > 0f;

        public bool IsTileCompatible() => false;
    }

    // PicoVideo;Bloom;ZhouShaoyang;Begin
    [Serializable]
    public sealed class BloomIterationModeParameter : VolumeParameter<BloomIterationMode>
    {
        public BloomIterationModeParameter(BloomIterationMode value, bool overrideState = false) : base(value, overrideState)
        {
        }
    }
    [Serializable]
    public sealed class BloomMaskModeParameter : VolumeParameter<BloomMaskMode>
    {
        public BloomMaskModeParameter(BloomMaskMode value, bool overrideState = false) : base(value, overrideState)
        {
        }
    }
    // PicoVideo;Bloom;ZhouShaoyang;End
}
