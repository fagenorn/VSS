using Assets.Scripts.BodyParameters;
using Assets.Scripts.Common;
using Assets.Scripts.Models;
using Cysharp.Threading.Tasks.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Live2D
{
    public class Live2DParameterMapper : MonoBehaviour
    {
        [SerializeField] private BodyTracker _bodyTracker;

        private bool _isPaused = false;

        private VSSModel _model;

        private List<ILive2DValueProvider> _providers = new List<ILive2DValueProvider>();

        private void Start()
        {
            GlobalStore.Instance.CurrentVSSModel.WithoutCurrent().Subscribe(SetModel);
        }

        private void SetModel(VSSModel model)
        {
            _isPaused = true;
            _providers.Clear();
            _model = model;

            if (model.VSSModelData.TrackerParameters == null || model.VSSModelData.TrackerParameters.Count == 0)
            {
                model.VSSModelData.TrackerParameters = new ObservableCollection<BodyTrackerValueProvider>(ValueProviderFactory.DefaultBodyTracker(_bodyTracker, model));
                model.VSSModelData.AnimationParameters = new ObservableCollection<Live2DAnimationProvider>(ValueProviderFactory.DefaultAnimation(model));
            }

            model.VSSModelData.TrackerParameters.CollectionChanged += TrackerParametersChanged;
            model.VSSModelData.AnimationParameters.CollectionChanged += AnimationParametersChanged;

            foreach (var item in model.VSSModelData.TrackerParameters)
            {
                item.Initialize(_bodyTracker, model);
                _providers.Add(item);
            }

            foreach (var item in model.VSSModelData.AnimationParameters)
            {
                item.Initialize(model);
                _providers.Add(item);
            }

            _providers.Add(new DefaultValueProvider(model));
            _isPaused = false;
        }

        private void TrackerParametersChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (BodyTrackerValueProvider item in e.NewItems)
                    {
                        item.Initialize(_bodyTracker, _model);
                        _providers.Add(item);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    _providers.RemoveAll(x => e.OldItems.Contains(x));
                    break;
                case NotifyCollectionChangedAction.Reset:
                    _providers.RemoveAll(x => x is BodyTrackerValueProvider);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void AnimationParametersChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Live2DAnimationProvider item in e.NewItems)
                    {
                        item.Initialize(_model);
                        _providers.Add(item);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    _providers.RemoveAll(x => e.OldItems.Contains(x));
                    break;
                case NotifyCollectionChangedAction.Reset:
                    _providers.RemoveAll(x => x is Live2DAnimationProvider);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void LateUpdate()
        {
            if (_isPaused) return;
            if (_model == null) return;

            var keys = _model?.Live2DParamDict?.Keys;
            var providers = _providers?.Where(x => x.Enabled).OrderBy(x => x.Priority).ToList();

            if (keys == null) return;
            if (providers == null) return;

            foreach (var parameter in keys)
            {
                foreach (var provider in providers)
                {
                    if (provider.TrySetValues(parameter)) break;
                }
            }

            foreach (var provider in providers)
            {
                provider.EndLoop();
            }
        }
    }
}
