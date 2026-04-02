using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace Gaffer.UI;

/// <summary>Runs queued actions on Unity's main thread for IL2CPP-safe UI updates.</summary>
public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly ConcurrentQueue<Action> Queue = new();

    public MainThreadDispatcher(IntPtr pointer) : base(pointer)
    {
    }

    /// <summary>Schedules work to run on the Unity main thread during the next Update tick.</summary>
    public static void Enqueue(Action action)
    {
        Queue.Enqueue(action);
    }

    private void Update()
    {
        while (Queue.TryDequeue(out Action? action))
        {
            action();
        }
    }
}
