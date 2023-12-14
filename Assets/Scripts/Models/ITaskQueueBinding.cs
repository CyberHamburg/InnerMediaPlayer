using System;
using System.Threading;
using System.Threading.Tasks;

namespace InnerMediaPlayer.Models
{
    internal interface ITaskQueueBinding
    {
        public Func<CancellationToken, Task> Func { get; set; }
        public Action CallBack { get; set; }
    }

    internal interface ITaskQueueBinding<TParam>
    {
        public TParam Param1 { get; set; }
        public Func<TParam, CancellationToken, Task> Func { get; set; }
        public Action CallBack { get; set; }
    }

    internal interface ITaskQueueBinding<TParam, TParam1>
    {
        public TParam Param1 { get; set; }
        public TParam1 Param2 { get; set; }
        public Func<TParam, TParam1, CancellationToken, Task> Func { get; set; }
        public Action CallBack { get; set; }
    }

    internal interface ITaskQueueBinding<TParam, TParam1, TParam2>
    {
        public TParam Param1 { get; set; }
        public TParam1 Param2 { get; set; }
        public TParam2 Param3 { get; set; }
        public Func<TParam, TParam1, TParam2, CancellationToken, Task> Func { get; set; }
        public Action CallBack { get; set; }
    }
}
