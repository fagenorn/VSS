using Cysharp.Threading.Tasks.Linq;
using Cysharp.Threading.Tasks.Triggers;
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScaleOnClickAnimation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum AnimationType
    {
        Once,
        PulseAndRotate
    }

    [System.Serializable]
    public class ScaleObjInfo
    {
        [SerializeField] public GameObject GameObject;

        [SerializeField] public Vector3 ScaleBy;

        [SerializeField] public Vector3 RotateAmount;

        [SerializeField] public float Duration;

        [SerializeField] public AnimationType Type;

        public Transform Transform { get; set; }

        public Selectable Selectable { get; set; }

        public bool Started { get; set; }
    }

    [SerializeField] private ScaleObjInfo[] _gameObjects;

    private List<Sequence> _sequences = new List<Sequence>();

    void Start()
    {
        foreach (var item in _gameObjects)
        {
            item.Transform = item.GameObject?.transform;
            item.Selectable = item.Transform.gameObject.GetComponent<Selectable>();
        }

        gameObject.GetAsyncDisableTrigger()
                         .Subscribe(gameObject =>StopAnimation());
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        foreach (var item in _gameObjects)
        {
            if (item.Transform == null) continue;
            if (item.Started) continue;
            if (item.Selectable != null && !item.Selectable.interactable) continue;

            Sequence sequence = item.Type switch
            {
                AnimationType.Once => DOTween.Sequence()
                                                               .Append(item.Transform.DOBlendableScaleBy(item.ScaleBy, item.Duration))
                                                               .SetAutoKill(false),
                AnimationType.PulseAndRotate => DOTween.Sequence()
                                                                              .Append(item.Transform.DOBlendableLocalRotateBy(-item.RotateAmount, item.Duration, RotateMode.FastBeyond360))
                                                                              .Join(item.Transform.DOBlendableScaleBy(item.ScaleBy, item.Duration))
                                                                              .SetAutoKill(false),
                _ => throw new NotImplementedException(),
            };

            _sequences.Add(sequence);
            sequence.Play();
            item.Started = true;
        }
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        StopAnimation();
    }

    private void StopAnimation()
    {
        foreach (var sequence in _sequences)
        {
            sequence.Rewind();
            sequence.Kill();
        }

        _sequences.Clear();

        foreach (var item in _gameObjects)
        {
            item.Started = false;
        }
    }
}
