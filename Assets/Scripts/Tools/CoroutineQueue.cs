using InnerMediaPlayer.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace InnerMediaPlayer.Tools
{
    internal class CoroutineQueue : MonoBehaviour
    {
        private Queue<Func<CancellationTokenSource, IProgress<TaskStatus>, IEnumerator>> _taskQueue;

        protected CancellationTokenSource cancelTokenSource;
        protected TaskProgress progress;
        protected Coroutine currentCoroutine;
        protected bool waitForCancel;

        private void Awake()
        {
            cancelTokenSource = new CancellationTokenSource();
            progress = new TaskProgress();
            _taskQueue = new Queue<Func<CancellationTokenSource, IProgress<TaskStatus>, IEnumerator>>();
        }

        internal async void Run(Func<CancellationTokenSource, IProgress<TaskStatus>, IEnumerator> method)
        {
            if (_taskQueue.Count == 0 && !waitForCancel)
                _taskQueue.Enqueue(method);

            while (_taskQueue.Count > 0 && !waitForCancel)
            {
                Func<CancellationTokenSource, IProgress<TaskStatus>, IEnumerator> newTaskFunc = _taskQueue.Dequeue();
                if (progress.Status == TaskStatus.Running)
                {
                    Stop();
                    while (progress.Status == TaskStatus.Running)
                    {
                        waitForCancel = true;
                        await Task.Yield();
                    }
                }

                waitForCancel = false;
                currentCoroutine = StartCoroutine(newTaskFunc(cancelTokenSource, progress));
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
    }
}
