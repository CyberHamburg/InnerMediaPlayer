using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace InnerMediaPlayer.Tools
{
    internal class CoroutineQueue : MonoBehaviour
    {
        private Queue<Func<CancellationToken, IEnumerator>> _taskQueue;

        protected CancellationTokenSource cancelTokenSource;
        protected Coroutine currentCoroutine;
        protected bool isRunningRunMethod;

        internal void Run(Func<CancellationToken, IEnumerator> method)
        {
            _taskQueue ??= new Queue<Func<CancellationToken, IEnumerator>>();
            _taskQueue.Enqueue(method);

            while (_taskQueue.Count > 0 && !isRunningRunMethod)
            {
                isRunningRunMethod = true;
                Func<CancellationToken, IEnumerator> newTaskFunc = _taskQueue.Dequeue();

                using (cancelTokenSource)
                {
                    if (currentCoroutine != null)
                    {
                        if (cancelTokenSource != null && !cancelTokenSource.IsCancellationRequested)
                        {
                            cancelTokenSource.Cancel();
                        }
                    }

                    cancelTokenSource = new CancellationTokenSource();
                    currentCoroutine = StartCoroutine(newTaskFunc(cancelTokenSource.Token));
                }
                isRunningRunMethod = false;
            }
        }
    }
}
