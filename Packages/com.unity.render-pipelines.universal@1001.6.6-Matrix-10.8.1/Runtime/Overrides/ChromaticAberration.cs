using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenu("Post-processing/Chromatic Aberration")]
    public sealed class ChromaticAberration : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Amount of tangential distortion.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        //PicoVideo;ChromaticAberration;ZhouShaoyang;Begin
        [Tooltip("Screen UVOffset")] 
        public Vector2Parameter uvOffset = new Vector2Parameter(Vector2.zero);
        //PicoVideo;ChromaticAberration;ZhouShaoyang;End
        public bool IsActive() => intensity.value > 0f;

        public bool IsTileCompatible() => false;
    }
}
