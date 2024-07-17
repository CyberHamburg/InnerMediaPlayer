using System;
using System.Threading.Tasks;

namespace InnerMediaPlayer.Models.Signal
{
    internal struct LyricInterruptDisplaySignal : ITaskQueueBinding
    {
        public Func<Tools.CancellationTokenSource, IProgress<TaskStatus>, Task> Func { get; set; }
        public Action CallBack { get; set; }
    }
}
