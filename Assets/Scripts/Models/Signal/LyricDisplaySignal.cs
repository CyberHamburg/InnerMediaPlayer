using System;
using System.Threading.Tasks;

namespace InnerMediaPlayer.Models.Signal
{
    internal struct LyricDisplaySignal : ITaskQueueBinding<int>
    {
        public int Param1 { get; set; }
        public Func<int, Tools.CancellationTokenSource, IProgress<TaskStatus>, Task> Func { get; set; }
        public Action CallBack { get; set; }
    }
}
