using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InnerMediaPlayer.Models;
using UnityEngine;

namespace InnerMediaPlayer.Tools
{
    /// <summary>
    /// 同时只有一个任务在运行的队列，再次加入任务会取消前面正在运行的任务
    /// </summary>
    internal class TaskQueue
    {
        private readonly Queue<Func<CancellationToken, Task>> _taskQueue;
        protected Task currentTask;
        protected CancellationTokenSource cancelTokenSource;
        protected bool isRunningRunMethod;

        internal TaskQueue()
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
            while (_taskQueue.Count > 0 && !isRunningRunMethod)
            {
                isRunningRunMethod = true;
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
                isRunningRunMethod = false;
            }
        }

        protected internal void Stop()
        {
            if (cancelTokenSource == null) 
                return;
#if UNITY_DEBUG
            Debug.Log("停止任务运行");
#endif
            cancelTokenSource.Cancel();
            cancelTokenSource = null;
        }

        internal void Binder<T>(T t) where T : ITaskQueueBinding
        {
            t.CallBack?.Invoke();
            AddTask(t.Func);
        }
    }

    /// <summary>
    /// 同时只有一个任务在运行的队列，再次加入任务会取消前面正在运行的任务
    /// </summary>
    internal class TaskQueue<TParam1> : TaskQueue
    {
        private readonly Queue<Func<TParam1, CancellationToken, Task>> _taskQueue;

        internal TaskQueue()
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
            while (_taskQueue.Count > 0 && !isRunningRunMethod)
            {
                isRunningRunMethod = true;
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
                isRunningRunMethod = false;
            }
        }

        internal new void Binder<T>(T t) where T : ITaskQueueBinding<TParam1>
        {
            t.CallBack?.Invoke();
            AddTask(t.Param1, t.Func);
        }
    }

    /// <summary>
    /// 同时只有一个任务在运行的队列，再次加入任务会取消前面正在运行的任务
    /// </summary>
    internal class TaskQueue<TParam1, TParam2> : TaskQueue<TParam1>
    {
        private readonly Queue<Func<TParam1, TParam2, CancellationToken, Task>> _taskQueue;

        internal TaskQueue()
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
            while (_taskQueue.Count > 0 && !isRunningRunMethod)
            {
                isRunningRunMethod = true;
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
                isRunningRunMethod = false;
            }
        }

        internal new void Binder<T>(T t) where T : ITaskQueueBinding<TParam1, TParam2>
        {
            t.CallBack?.Invoke();
            AddTask(t.Param1, t.Param2, t.Func);
        }
    }

    /// <summary>
    /// 同时只有一个任务在运行的队列，再次加入任务会取消前面正在运行的任务
    /// </summary>
    internal class TaskQueue<TParam1, TParam2, TParam3> : TaskQueue<TParam1, TParam2>
    {
        private readonly Queue<Func<TParam1, TParam2, TParam3, CancellationToken, Task>> _taskQueue;

        internal TaskQueue()
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
            while (_taskQueue.Count > 0 && !isRunningRunMethod)
            {
                isRunningRunMethod = true;
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
                isRunningRunMethod = false;
            }
        }

        internal new void Binder<T>(T t) where T : ITaskQueueBinding<TParam1, TParam2, TParam3>
        {
            t.CallBack?.Invoke();
            AddTask(t.Param1, t.Param2, t.Param3, t.Func);
        }
    }
}
