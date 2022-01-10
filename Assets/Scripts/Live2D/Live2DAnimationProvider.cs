using Assets.Scripts.Animations;
using Assets.Scripts.Models;
using Cysharp.Threading.Tasks.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Live2D
{
    public partial class Live2DAnimationProvider : ILive2DValueProvider, IDisposable
    {
        private IDisposable _hotkeyDisposable;

        private float _smoothTime = 0.1f;

        private bool _playing;

        private bool _playingBackwards;

        private VSSModel _vssModel;

        private VSSMotionData _vssMotionData;

        private Dictionary<string, float> _velocityDict;

        private Dictionary<string, AnimationCurve> _curvesDict;

        private float _animationTimer;

        private float _previousTime;

        void ILive2DValueProvider.EndLoop()
        {
            if (_vssMotionData == null) return;
            if (!_playing) return;

            _previousTime = _animationTimer;
            _animationTimer += _playingBackwards ? -Time.deltaTime : Time.deltaTime;

            if (_animationTimer <= 0 || _animationTimer >= _vssMotionData.Clip.length)
            {
                switch (AnimationType)
                {
                    case AnimationType.OneShot:
                        _playing = false;
                        _animationTimer = 0;
                        _playingBackwards = false;
                        break;
                    case AnimationType.Looping:
                        _playing = true;
                        _animationTimer = 0;
                        _playingBackwards = false;
                        break;
                    case AnimationType.Hold:
                        _playing = true;
                        _animationTimer = _vssMotionData.Clip.length;
                        _playingBackwards = false;
                        break;
                    case AnimationType.PingPong:
                        _playing = true;
                        _animationTimer = _playingBackwards ? 0 : _vssMotionData.Clip.length;
                        _playingBackwards = !_playingBackwards;
                        break;
                }
            }
        }

        bool ILive2DValueProvider.TrySetValues(string live2DId)
        {
            if (!_playing) return false;
            if (_curvesDict == null) return false;

            if (!_curvesDict.TryGetValue(live2DId, out var curve)) return false;

            var oldTarget = curve.Evaluate(_previousTime);
            var target = curve.Evaluate(_animationTimer);
            var live2DParam = _vssModel.Live2DParamDict[live2DId];

            if (Mathf.Abs(live2DParam.Value - oldTarget) > 0.0001f)
            {
                var velocity = _velocityDict[live2DId];
                target = Mathf.SmoothDamp(live2DParam.Value, target, ref velocity, _smoothTime);
                _velocityDict[live2DId] = velocity;
            }

            live2DParam.Value = target;

            return true;
        }

        public void Initialize(VSSModel model)
        {
            _vssModel = model;

            AnimationUpdated();
            HotkeyUpdated();
        }

        private void AnimationUpdated()
        {
            if (_vssModel == null) return;
            if (MotionId == null) return;
            if (!_vssModel.VSSMotionDataDict.ContainsKey(MotionId)) return;

            _vssMotionData = _vssModel.VSSMotionDataDict[MotionId];
            _velocityDict = _vssMotionData.CubismFadeMotionData.ParameterIds.ToDictionary(x => x, _ => 0.0f);
            _curvesDict = _vssMotionData.CubismFadeMotionData.ParameterCurves
                                                                    .Select((input, index) => new { input, index })
                                                                    .ToDictionary(x => _vssMotionData.CubismFadeMotionData.ParameterIds[x.index], x => x.input);
        }

        private void HotkeyUpdated()
        {
            _hotkeyDisposable?.Dispose();
            _hotkeyDisposable = HotkeyManager.Instance.HotKey.Where(x => HasHotkey && x == KeyCombo).Subscribe(x =>
            {
                _playing = !_playing;
                _animationTimer = 0;
                _playingBackwards = false;
            });
        }

        private void TypeUpdated()
        {
            _playing = !HasHotkey;
            _animationTimer = 0;
            _playingBackwards = false;
        }

        public void Dispose()
        {
            _hotkeyDisposable?.Dispose();
        }
    }
}
