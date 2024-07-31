using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InnerMediaPlayer.Models;
using UnityEngine;

namespace InnerMediaPlayer.Tools
{
    internal class CancellationTokenSource
    {
        private System.Threading.CancellationTokenSource _token;

        internal bool IsCancellationRequested
        {
            get
            {
                bool isCancellationRequested = _token.IsCancellationRequested;
                if (isCancellationRequested)
                {
                    _token.Dispose();
                    _token = new System.Threading.CancellationTokenSource();
                }

                return isCancellationRequested;
            }
        }

        internal CancellationTokenSource()
        {
            _token = new System.Threading.CancellationTokenSource();
        }

        internal void Cancel() => _token.Cancel();
    }

    /// <summary>
    /// 同时只有一个任务在运行的队列，再次加入任务会取消前面正在运行的任务
    /// </summary>
    internal class TaskQueue
    {
        private readonly Queue<Func<CancellationTokenSource, IProgress<TaskStatus>, Task>> _taskQueue;
        protected TaskProgress process;
        protected CancellationTokenSource cancelTokenSource;
        protected bool waitForCancel;

        protected internal TaskStatus Status => process.Status;

        internal TaskQueue()
        {
            _taskQueue = new Queue<Func<CancellationTokenSource, IProgress<TaskStatus>, Task>>(3);
            cancelTokenSource = new CancellationTokenSource();
            process = new TaskProgress();
        }

        internal void AddTask(Func<CancellationTokenSource, IProgress<TaskStatus>, Task> taskFunc)
        {
            if (_taskQueue.Count == 0 && !waitForCancel)
                _taskQueue.Enqueue(taskFunc);
            Run();
        }

        private async void Run()
        {
            while (_taskQueue.Count > 0 && !waitForCancel)
            {
                Func<CancellationTokenSource, IProgress<TaskStatus>, Task> newTaskFunc = _taskQueue.Dequeue();
                if (process != null && process.Status == TaskStatus.Running)
                {
                    Stop();
                    while (process.Status == TaskStatus.Running)
                    {
                        waitForCancel = true;
                        await Task.Yield();
                    }
                }

                waitForCancel = false;
                try
                {
                    await newTaskFunc(cancelTokenSource, process);
                }
                catch (MissingReferenceException)
                {
                    return;
                }
            }
        }

        protected internal void Stop()
        {
            if (cancelTokenSource == null)
                throw new NullReferenceException("取消令牌为空");
#if UNITY_DEBUG
            Debug.Log("停止任务运行");
#endif
            cancelTokenSource.Cancel();
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
        private readonly Queue<Func<TParam1, CancellationTokenSource, IProgress<TaskStatus>, Task>> _taskQueue;

        internal TaskQueue()
        {
            _taskQueue = new Queue<Func<TParam1, CancellationTokenSource, IProgress<TaskStatus>, Task>>(3);
            cancelTokenSource = new CancellationTokenSource();
        }

        internal void AddTask(TParam1 param1, Func<TParam1, CancellationTokenSource, IProgress<TaskStatus>, Task> taskFunc)
        {
            if (_taskQueue.Count == 0 && !waitForCancel)
                _taskQueue.Enqueue(taskFunc);
            Run(param1);
        }

        private async void Run(TParam1 param1)
        {
            while (_taskQueue.Count > 0 && !waitForCancel)
            {
                Func<TParam1, CancellationTokenSource, IProgress<TaskStatus>, Task> newTaskFunc = _taskQueue.Dequeue();
                if (process != null && process.Status == TaskStatus.Running)
                {
                    Stop();
                    while (process.Status == TaskStatus.Running)
                    {
                        waitForCancel = true;
                        await Task.Yield();
                    }
                }

                waitForCancel = false;
                try
                {
                    await newTaskFunc(param1, cancelTokenSource, process);
                }
                catch (MissingReferenceException)
                {
                    return;
                }
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
        private readonly Queue<Func<TParam1, TParam2, CancellationTokenSource, IProgress<TaskStatus>, Task>> _taskQueue;

        internal TaskQueue()
        {
            _taskQueue = new Queue<Func<TParam1, TParam2, CancellationTokenSource, IProgress<TaskStatus>, Task>>(3);
            cancelTokenSource = new CancellationTokenSource();
        }

        internal void AddTask(TParam1 param1, TParam2 param2, Func<TParam1, TParam2, CancellationTokenSource, IProgress<TaskStatus>, Task> taskFunc)
        {
            if (_taskQueue.Count == 0 && !waitForCancel) 
                _taskQueue.Enqueue(taskFunc);
            Run(param1, param2);
        }

        private async void Run(TParam1 param1, TParam2 param2)
        {
            while (_taskQueue.Count > 0 && !waitForCancel)
            {
                Func<TParam1, TParam2, CancellationTokenSource, IProgress<TaskStatus>, Task> newTaskFunc = _taskQueue.Dequeue();
                if (process.Status == TaskStatus.Running)
                {
                    Stop();
                    while (process.Status == TaskStatus.Running)
                    {
                        waitForCancel = true;
                        await Task.Yield();
                    }
                }

                waitForCancel = false;
                try
                {
                    await newTaskFunc(param1, param2, cancelTokenSource, process);
                }
                catch (MissingReferenceException)
                {
                    return;
                }
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
        private readonly Queue<Func<TParam1, TParam2, TParam3, CancellationTokenSource, IProgress<TaskStatus>, Task>> _taskQueue;

        internal TaskQueue()
        {
            _taskQueue = new Queue<Func<TParam1, TParam2, TParam3, CancellationTokenSource, IProgress<TaskStatus>, Task>>(3);
            cancelTokenSource = new CancellationTokenSource();
        }

        internal void AddTask(TParam1 param1, TParam2 param2, TParam3 param3,
            Func<TParam1, TParam2, TParam3, CancellationTokenSource, IProgress<TaskStatus>, Task> taskFunc)
        {
            if (_taskQueue.Count == 0 && !waitForCancel)
                _taskQueue.Enqueue(taskFunc);
            Run(param1, param2, param3);
        }

        private async void Run(TParam1 param1, TParam2 param2, TParam3 param3)
        {
            while (_taskQueue.Count > 0 && !waitForCancel)
            {
                Func<TParam1, TParam2, TParam3, CancellationTokenSource, IProgress<TaskStatus>, Task> newTaskFunc = _taskQueue.Dequeue();
                if (process.Status == TaskStatus.Running)
                {
                    Stop();
                    while (process.Status == TaskStatus.Running)
                    {
                        waitForCancel = true;
                        await Task.Yield();
                    }
                }

                waitForCancel = false;
                try
                {
                    await newTaskFunc(param1, param2, param3, cancelTokenSource, process);
                }
                catch (MissingReferenceException)
                {
                    return;
                }
            }
        }

        internal new void Binder<T>(T t) where T : ITaskQueueBinding<TParam1, TParam2, TParam3>
        {
            t.CallBack?.Invoke();
            AddTask(t.Param1, t.Param2, t.Param3, t.Func);
        }
    }
}
