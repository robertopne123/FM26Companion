using System;
using System.Collections.Concurrent;

namespace Gaffer
{
    /// <summary>
    /// Thread-safe queue for marshalling work back onto Unity's main thread.
    /// Enqueue from any thread; Flush() is called each frame from GafferBehaviour.Update().
    /// </summary>
    internal static class MainThreadDispatcher
    {
        // IL2CPP PATTERN: async/await continuations in .NET 6 run on ThreadPool threads.
        // Unity's API (Text.text, GameObject methods, etc.) is not thread-safe — all
        // calls must happen on the main thread. ConcurrentQueue gives lock-free enqueue
        // from any thread, safe dequeue on the main thread in Update().
        private static readonly ConcurrentQueue<Action> _queue = new();

        /// <summary>Enqueues an action to be executed on the main thread next frame.</summary>
        public static void Enqueue(Action action) => _queue.Enqueue(action);

        /// <summary>
        /// Drains the queue and executes all pending actions.
        /// Must only be called from the main thread (inside MonoBehaviour.Update).
        /// </summary>
        internal static void Flush()
        {
            while (_queue.TryDequeue(out var action))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"[MainThreadDispatcher] Action threw: {ex.Message}");
                }
            }
        }
    }
}
