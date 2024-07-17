using System;
using System.Threading.Tasks;

namespace InnerMediaPlayer.Models
{
    internal interface ITaskQueueBinding
    {
        public Func<Tools.CancellationTokenSource, IProgress<TaskStatus>, Task> Func { get; set; }
        public Action CallBack { get; set; }
    }

    internal interface ITaskQueueBinding<TParam>
    {
        public TParam Param1 { get; set; }
        public Func<TParam, Tools.CancellationTokenSource, IProgress<TaskStatus>, Task> Func { get; set; }
        public Action CallBack { get; set; }
    }

    internal interface ITaskQueueBinding<TParam, TParam1>
    {
        public TParam Param1 { get; set; }
        public TParam1 Param2 { get; set; }
        public Func<TParam, TParam1, Tools.CancellationTokenSource, IProgress<TaskStatus>, Task> Func { get; set; }
        public Action CallBack { get; set; }
    }

    internal interface ITaskQueueBinding<TParam, TParam1, TParam2>
    {
        public TParam Param1 { get; set; }
        public TParam1 Param2 { get; set; }
        public TParam2 Param3 { get; set; }
        public Func<TParam, TParam1, TParam2, Tools.CancellationTokenSource, IProgress<TaskStatus>, Task> Func { get; set; }
        public Action CallBack { get; set; }
    }
}
