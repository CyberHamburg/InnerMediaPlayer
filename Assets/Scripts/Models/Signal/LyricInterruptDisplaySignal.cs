using System;
using System.Threading;
using System.Threading.Tasks;

namespace InnerMediaPlayer.Models.Signal
{
    internal struct LyricInterruptDisplaySignal : ITaskQueueBinding
    {
        public Func<CancellationToken, Task> Func { get; set; }
        public Action CallBack { get; set; }
    }
}
