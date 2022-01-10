using Assets.Scripts.BodyParameters;
using Assets.Scripts.Common;
using Assets.Scripts.Models;
using Live2D.Cubism.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace Assets.Scripts.Live2D
{
    public partial class BodyTrackerValueProvider : ILive2DValueProvider
    {
        private float _velocity = 0.0f;

        private BodyTracker _bodyTracker;

        private VSSModel _model;

        private CubismParameter _live2DParameter;

        private BodyParameterInstance _bodyParameter;

        [JsonIgnore] public string[] Live2DIds { get; private set; }

        [JsonIgnore]
        public BodyParameterInstance BodyParameter
        {
            get
            {
                if (_bodyParameter != null) return _bodyParameter;
                if (BodyParameterType == BodyParam.None) return null;

                _bodyParameter = _bodyTracker.GetBodyParameter(this.BodyParameterType);
                return _bodyParameter;
            }
        }

        [JsonIgnore]
        public CubismParameter Live2DParameter
        {
            get
            {
                if (_live2DParameter != null) return _live2DParameter;
                if (Live2DParameterId == null) return null;

                _live2DParameter = _model.Live2DParamDict[Live2DParameterId];
                return _live2DParameter;
            }
        }

        int ILive2DValueProvider.Priority => (int)Priorities.BodyTracker;

        bool ILive2DValueProvider.Enabled => BodyParameter?.IsTracking() ?? false;

        public void Initialize(BodyTracker bodyTracker, VSSModel model)
        {
            _bodyTracker = bodyTracker;
            _model = model;
        }

        void ILive2DValueProvider.EndLoop() { }

        bool ILive2DValueProvider.TrySetValues(string live2DId)
        {
            var live2DParam = Live2DParameter;
            var locallive2DId = Live2DParameterId;
            var bodyParam = BodyParameter;

            if (live2DParam == null) return false;
            if (bodyParam == null) return false;
            if (live2DId != locallive2DId) return false;


            var value = bodyParam.Value;

            var inputMin = Sensitivity > 0 ? InputMin * (Sensitivity) : InputMin;
            var inputMax = Sensitivity > 0 ? InputMax * (Sensitivity) : InputMax;
            var outputMin = Sensitivity < 0 ? OutputMin * (-Sensitivity) : OutputMin;
            var outputMax = Sensitivity < 0 ? OutputMax * (-Sensitivity) : OutputMax;

            var normalized = MathHelper.Normalize(value, inputMin, inputMax, outputMin, outputMax);
            var result = Mathf.SmoothDamp(live2DParam.Value, normalized, ref _velocity, SmoothTime);
            live2DParam.Value = result;

            return true;
        }
    }
}
