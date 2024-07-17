using System;
using System.Threading.Tasks;

namespace InnerMediaPlayer.Models
{
    internal class TaskProgress : IProgress<TaskStatus>
    {
        internal TaskStatus Status { get; private set; }

        void IProgress<TaskStatus>.Report(TaskStatus value)
        {
            Status = value;
        }
    }
}
