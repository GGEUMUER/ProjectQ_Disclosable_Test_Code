using System;
using System.Collections.Concurrent;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly ConcurrentQueue<Action> actions = new();

    public static void Enqueue(Action action)
    {
        if (action != null)
            actions.Enqueue(action);
    }

    void Update()
    {
        while (actions.TryDequeue(out var action))
        {
            action?.Invoke();
        }
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
} // 반드시 씬에 Dispatcher 오브젝트로 존재해야 함