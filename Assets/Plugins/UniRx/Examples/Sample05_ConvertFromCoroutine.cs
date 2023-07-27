using System;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace UniRx.Examples
{
    public class Sample05_ConvertFromCoroutine
    {
        // public method
        public static IObservable<string> GetWWW(string url)
        {
            // convert coroutine to IObservable
            return Observable.FromCoroutine<string>((observer, cancellationToken) => GetWWWCore(url, observer, cancellationToken));
        }

        // IEnumerator with callback
        static IEnumerator GetWWWCore(string url, IObserver<string> observer, CancellationToken cancellationToken)
        {
#pragma warning disable CS0618 // “WWW”已过时:“Use UnityWebRequest, a fully featured replacement which is more efficient and has additional features”
            var www = new UnityEngine.WWW(url);
#pragma warning restore CS0618 // “WWW”已过时:“Use UnityWebRequest, a fully featured replacement which is more efficient and has additional features”
            while (!www.isDone && !cancellationToken.IsCancellationRequested)
            {
                yield return null;
            }

            if (cancellationToken.IsCancellationRequested) yield break;

            if (www.error != null)
            {
                observer.OnError(new Exception(www.error));
            }
            else
            {
                observer.OnNext(www.text);
                observer.OnCompleted();
            }
        }
    }
}