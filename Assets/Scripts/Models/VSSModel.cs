using Assets.Scripts.BodyParameters;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.Motion;
using Live2D.Cubism.Framework.MotionFade;
using System.Collections.Generic;

namespace Assets.Scripts.Models
{
    public class VSSModel
    {
        public VSSModelData VSSModelData { get; set; }

        public BodyTracker BodyTracker { get; set; }

        public Dictionary<string, VSSMotionData> VSSMotionDataDict { get; set; }

        public CubismModel Live2DModel { get; set; }

        public Dictionary<string, CubismParameter> Live2DParamDict { get; set; }

        public Dictionary<string, CubismPart> Live2DPartDict { get; set; }

        public CubismMotionController CubismMotionController { get; set; }

        public CubismFadeController CubismFadeController { get; set; }
    }
}
