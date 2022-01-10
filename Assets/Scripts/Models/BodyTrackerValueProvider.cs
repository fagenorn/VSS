using Assets.Scripts.BodyParameters;
using Assets.Scripts.Models;
using Newtonsoft.Json;

namespace Assets.Scripts.Live2D
{
    using J = JsonPropertyAttribute;

    public partial class BodyTrackerValueProvider : BaseData
    {
        private string live2DParameterId;

        private BodyParam bodyParameterType;

        [J("_name")] public string Name { get; set; }

        [J("_inputMin")] public float InputMin { get; set; }

        [J("_inputMax")] public float InputMax { get; set; }

        [J("_outputMin")] public float OutputMin { get; set; }

        [J("_outputMax")] public float OutputMax { get; set; }

        [J("_sensitivity")] public float Sensitivity { get; set; } = 1f;

        [J("_smoothTime")] public float SmoothTime { get; set; } = .1f;

        [J("_live2DParameterId")]
        public string Live2DParameterId
        {
            get => live2DParameterId;
            set
            {
                live2DParameterId = value;
                _live2DParameter = null;
            }
        }

        [J("_bodyParameterType")]
        public BodyParam BodyParameterType
        {
            get => bodyParameterType;
            set
            {
                bodyParameterType = value;
                _bodyParameter = null;
            }
        }
    }
}
