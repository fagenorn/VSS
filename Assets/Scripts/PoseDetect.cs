using Mediapipe;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoseDetect : MonoBehaviour
{
    private Queue<string> _appendQueue = new Queue<string>();
    private object _lock = new object();

    private void Update()
    {
        lock (_lock)
        {
            if (_appendQueue.Count == 0) return;
            Debug.Log(_appendQueue.Dequeue());
        }
    }

    public void Proccess(Detection detection)
    {
        lock (_lock)
        {
            _appendQueue.Enqueue(detection.ToString());
        }
    }
}
