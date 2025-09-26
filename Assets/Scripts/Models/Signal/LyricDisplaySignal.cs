using System;
using System.Threading.Tasks;

namespace InnerMediaPlayer.Models.Signal
{
    internal struct LyricDisplaySignal : ITaskQueueBinding<long>
    {
        public long Param1 { get; set; }
        public Func<long, Tools.CancellationTokenSource, IProgress<TaskStatus>, Task> Func { get; set; }
        public Action CallBack { get; set; }
    }
}
