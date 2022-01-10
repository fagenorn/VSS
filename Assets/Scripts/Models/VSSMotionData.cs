using Live2D.Cubism.Framework.Json;
using Live2D.Cubism.Framework.MotionFade;
using UnityEngine;

namespace Assets.Scripts.Models
{
    public class VSSMotionData
    {
        public string Name { get; set; }

        public AnimationClip Clip { get; set; }

        public CubismFadeMotionData CubismFadeMotionData { get; set; }

        public CubismMotion3Json CubismMotion3Json { get; set; }
    }
}
