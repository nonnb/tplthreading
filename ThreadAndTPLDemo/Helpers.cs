using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ThreadAndTPLDemo
{
    public static class Helpers
    {
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> items)
        {
            return new HashSet<T>(items);
        }

        public static void AssignThreadToProcessor(int affinityMask)
        {
            Thread.BeginThreadAffinity();
#pragma warning disable 618 // Yes, I know what I'm doing ...
            var osThreadId = AppDomain.GetCurrentThreadId();
#pragma warning restore 618
            var thisProcessThread = Process.GetCurrentProcess()
                .Threads
                .Cast<ProcessThread>()
                .Single(t => t.Id == osThreadId);
            thisProcessThread.IdealProcessor = 0;
            thisProcessThread.ProcessorAffinity = (IntPtr)affinityMask;
            thisProcessThread.PriorityBoostEnabled = true;
        }

    }
}
