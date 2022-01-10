using Assets.Scripts.Models;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Live2D
{
    internal class DefaultValueProvider : ILive2DValueProvider
    {
        private VSSModel _vssModel;

        private Dictionary<string, float> _velocityDict;

        private float _smoothTime = 0.3f;

        public int Priority { get; } = (int)Priorities.Default;

        public bool Enabled { get; set; } = true;

        public string[] Live2DIds { get; }

        bool ILive2DValueProvider.TrySetValues(string live2DId)
        {
            if (!_vssModel.Live2DParamDict.TryGetValue(live2DId, out var param)) return false;

            var velocity = _velocityDict[live2DId];
            var target = param.DefaultValue;
            var result = Mathf.SmoothDamp(param.Value, target, ref velocity, _smoothTime);
            _velocityDict[live2DId] = velocity;
            param.Value = result;

            return true;
        }

        public void EndLoop() { }

        public DefaultValueProvider(VSSModel model)
        {
            _vssModel = model;
            _velocityDict = model.Live2DParamDict.ToDictionary(x => x.Key, _ => 0.0f);
        }
    }
}
