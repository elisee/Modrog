using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace DeepSwarmCommon
{
    public class ThreadActionQueue
    {
        readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

        readonly int _processingThreadId = -1;

        public ThreadActionQueue() { }
        public ThreadActionQueue(int processingThreadId) { _processingThreadId = processingThreadId; }

        public void Run(Action action)
        {
            Debug.Assert(_processingThreadId == -1 || Thread.CurrentThread.ManagedThreadId != _processingThreadId);
            _queue.Enqueue(action);
        }

        public void ExecuteActions()
        {
            Debug.Assert(_processingThreadId == -1 || Thread.CurrentThread.ManagedThreadId == _processingThreadId);
            while (_queue.TryDequeue(out var action)) action();
        }
    }
}
