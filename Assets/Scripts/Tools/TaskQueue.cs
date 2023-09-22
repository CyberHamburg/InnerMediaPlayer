using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InnerMediaPlayer.Tools
{
    internal class TaskQueue
    {
        private readonly Queue<Func<CancellationToken, Task>> _taskQueue;
        protected Task currentTask;
        protected CancellationTokenSource cancelTokenSource;
        protected bool isRunning;

        protected TaskQueue()
        {
            _taskQueue = new Queue<Func<CancellationToken, Task>>(3);
        }

        internal void AddTask(Func<CancellationToken, Task> taskFunc)
        {
            _taskQueue.Enqueue(taskFunc);
            Run();
        }

        private async void Run()
        {
            while (_taskQueue.Count > 0 && !isRunning)
            {
                isRunning = true;
                Func<CancellationToken, Task> newTaskFunc = _taskQueue.Dequeue();
                if (currentTask != null && !currentTask.IsCompleted)
                {
                    if (cancelTokenSource != null && !cancelTokenSource.IsCancellationRequested)
                    {
                        cancelTokenSource.Cancel();
                    }
                }

                cancelTokenSource = new CancellationTokenSource();
                currentTask = newTaskFunc(cancelTokenSource.Token);
                await Task.Yield();
                isRunning = false;
            }
        }
    }

    internal class TaskQueue<TParam1> : TaskQueue
    {
        private readonly Queue<Func<TParam1, CancellationToken, Task>> _taskQueue;

        protected TaskQueue()
        {
            _taskQueue = new Queue<Func<TParam1, CancellationToken, Task>>(3);
        }

        internal void AddTask(TParam1 param1, Func<TParam1, CancellationToken, Task> taskFunc)
        {
            _taskQueue.Enqueue(taskFunc);
            Run(param1);
        }

        private async void Run(TParam1 param1)
        {
            while (_taskQueue.Count > 0 && !isRunning)
            {
                isRunning = true;
                Func<TParam1, CancellationToken, Task> newTaskFunc = _taskQueue.Dequeue();
                if (currentTask != null && !currentTask.IsCompleted)
                {
                    if (cancelTokenSource != null && !cancelTokenSource.IsCancellationRequested)
                    {
                        cancelTokenSource.Cancel();
                    }
                }

                cancelTokenSource = new CancellationTokenSource();
                currentTask = newTaskFunc(param1, cancelTokenSource.Token);
                await Task.Yield();
                isRunning = false;
            }
        }
    }

    internal class TaskQueue<TParam1, TParam2> : TaskQueue<TParam1>
    {
        private readonly Queue<Func<TParam1, TParam2, CancellationToken, Task>> _taskQueue;

        public TaskQueue()
        {
            _taskQueue = new Queue<Func<TParam1, TParam2, CancellationToken, Task>>(3);
        }

        internal void AddTask(TParam1 param1, TParam2 param2, Func<TParam1, TParam2, CancellationToken, Task> taskFunc)
        {
            _taskQueue.Enqueue(taskFunc);
            Run(param1, param2);
        }

        private async void Run(TParam1 param1, TParam2 param2)
        {
            while (_taskQueue.Count > 0 && !isRunning)
            {
                isRunning = true;
                Func<TParam1, TParam2, CancellationToken, Task> newTaskFunc = _taskQueue.Dequeue();
                if (currentTask != null && !currentTask.IsCompleted)
                {
                    if (cancelTokenSource != null && !cancelTokenSource.IsCancellationRequested)
                    {
                        cancelTokenSource.Cancel();
                    }
                }

                cancelTokenSource = new CancellationTokenSource();
                currentTask = newTaskFunc(param1, param2, cancelTokenSource.Token);
                await Task.Yield();
                isRunning = false;
            }
        }
    }

    internal class TaskQueue<TParam1, TParam2, TParam3> : TaskQueue<TParam1, TParam2>
    {
        private readonly Queue<Func<TParam1, TParam2, TParam3, CancellationToken, Task>> _taskQueue;

        public TaskQueue()
        {
            _taskQueue = new Queue<Func<TParam1, TParam2, TParam3, CancellationToken, Task>>(3);
        }

        internal void AddTask(TParam1 param1, TParam2 param2, TParam3 param3,
            Func<TParam1, TParam2, TParam3, CancellationToken, Task> taskFunc)
        {
            _taskQueue.Enqueue(taskFunc);
            Run(param1, param2, param3);
        }

        private async void Run(TParam1 param1, TParam2 param2, TParam3 param3)
        {
            while (_taskQueue.Count > 0 && !isRunning)
            {
                isRunning = true;
                Func<TParam1, TParam2, TParam3, CancellationToken, Task> newTaskFunc = _taskQueue.Dequeue();
                if (currentTask != null && !currentTask.IsCompleted)
                {
                    if (cancelTokenSource != null && !cancelTokenSource.IsCancellationRequested)
                    {
                        cancelTokenSource.Cancel();
                    }
                }

                cancelTokenSource = new CancellationTokenSource();
                currentTask = newTaskFunc(param1, param2, param3, cancelTokenSource.Token);
                await Task.Yield();
                isRunning = false;
            }
        }
    }
}
