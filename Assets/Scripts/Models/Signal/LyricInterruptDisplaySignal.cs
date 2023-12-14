using System;
using System.Threading;
using System.Threading.Tasks;

namespace InnerMediaPlayer.Models.Signal
{
    internal struct LyricInterruptDisplaySignal : ITaskQueueBinding<float>
    {
        public float Param1 { get; set; }
        public Func<float, CancellationToken, Task> Func { get; set; }
        public Action CallBack { get; set; }
    }
}
