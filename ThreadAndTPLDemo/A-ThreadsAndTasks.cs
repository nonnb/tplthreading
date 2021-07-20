using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ThreadAndTPLDemo
{
    [TestFixture]
    public class ThreadsAndTasks
    {
        static ThreadsAndTasks()
        {
            Console.SetOut(TestContext.Progress);
        }

        private const int NumCores = 8;

        [Test]
        public void _01NoThreadCoreAffinity()
        {
            // This is a single threaded calculation. The thread will be arbitrarily assigned to available cores and context switched many times per second
            DoInfiniteCalculation();
        }

        // NB - Change the project to net4.8
        [Test]
        public void _02ThreadAffinityNetFw()
        {
            // Depending on the O/S (e.g. Windows), we can sometimes influence on which core the thread is to be run on
            // The affinity mask is a flags enum (1 = Core0, 2 = Core1, 4 = Core2, 8 = Core3, 10 = Core4, 20 = Core5, 40 = Core6, 80=Core7)
            Helpers.AssignThreadToProcessor(0x02);
            DoInfiniteCalculation();
        }

        [Test]
        public void _03UncontendedParallelism()
        {
            // Deliberately language specific features like LINQ
            var myThreads = new List<Thread>();
            for(var i = 0; i < NumCores; i++)
            {
                var capture = i;
                var thread = new Thread(() => DoUncontendedCpuBoundWork(capture));
                myThreads.Add(thread);
                thread.Start();
            }

            // How many threads do we have right now?
            foreach (var thread in myThreads)
            {
                // This main thread won't actually do any 'real' work - Are we there yet? No -> Context switch
                thread.Join();
            }
            Console.WriteLine($"Main thread: {Thread.CurrentThread.ManagedThreadId}");

            // All work starts ~simultaneously, and each finishes on the same thread it started on
        }

        [Test]
        public void _04ContendedWork_Blocking()
        {
            var myThreads = new List<Thread>();
            for (var i = 0; i < NumCores; i++)
            {
                var capture = i;
                var thread = new Thread(() => DoContendedWork(capture));
                myThreads.Add(thread);
                thread.Start();
            }

            // How many threads do we have right now?
            foreach (var thread in myThreads)
            {
                thread.Join();
            }
            Console.WriteLine($"Main thread: {Thread.CurrentThread.ManagedThreadId}");

            // Benefits over single threaded DoContendedWork?
            // lock syntactic sugar is safer than Monitor.Enter (implicit try finally, check for reference type)
        }

        [Test]
        public void _10Tasks()
        {
            var task1 = new Task(() => DoUncontendedCpuBoundWork(1));
            task1.Start();
            // Or, quicker
            var task2 = Task.Run(() => DoUncontendedCpuBoundWork(2));

            Task.WaitAll(task1, task2);
        }


        [Test]
        public void _11TPL_Plinq()
        {
            var items = Enumerable.Range(0, 100)
                .AsParallel()
                .Select(i => DoUncontendedCpuBoundWork(i))
                .ToList();
        }


        [Test]
        public void _12TPL_ParallelFor()
        {
            long grandTotal = 0;
            var nums = CreateRandomIntegers(1000000);

            Parallel.For(
                0,                          // From, inclusive
                nums.Length,                // To, Exclusive
                new ParallelOptions { MaxDegreeOfParallelism = 8 },

                () => 0,                    // 'Initialization' per partition

                // Tight loop
                (j, loop, subtotal) =>      // method invoked by the loop on each iteration
                    subtotal + nums[j],          // value to be passed to next iteration subtotal
                
                // The final value of subtotal is passed to the localFinally function parameter
                subtotal =>
                {
                    Console.WriteLine($"Task Id {Task.CurrentId}");
                    Interlocked.Add(ref grandTotal, subtotal);
                });

            Console.WriteLine($"Grand Total : {grandTotal}");
        }


        private int[] CreateRandomIntegers(int size)
        {
            var rnd = new Random();
            return Enumerable.Range(0, size)
                .Select(_ => rnd.Next(5000))
                .ToArray();
        }


        private static void DoInfiniteCalculation()
        {
            for (var denom = 1; denom < int.MaxValue; denom++)
            {
                var total = 0d;
                for (var num = 0; num < denom; num++)
                {
                    total += (double)num / denom;
                }

                var average = total / denom;
            }
        }

        // No contention for any resource, no mutation
        private static int DoUncontendedCpuBoundWork(int i)
        {
            Console.WriteLine($"Work Item {i} started on Thread {Thread.CurrentThread.ManagedThreadId} at {DateTime.UtcNow:HH:m:s.fff}");
            Thread.Sleep(5000);
            Console.WriteLine($"Work Item {i} finished on Thread {Thread.CurrentThread.ManagedThreadId} at {DateTime.UtcNow:HH:m:s.fff}");
            return i;
        }

        // Locks must be held over a reference type
        private readonly object _myLock = new object();

        // private int _myLock = 0;
        // private const int _myLock = 0;
        private void DoContendedWork(int i)
        {
            Console.WriteLine("Work Item {0} on Thread {1} waiting for lock", i, Thread.CurrentThread.ManagedThreadId);

            // Work done inside the lock isn't thread safe - e.g. there is contention for a shared / common resource
            lock (_myLock)
            {
                Console.WriteLine("Work Item {0} got the lock!", i);
                Thread.Sleep(2000);
            }

            Console.WriteLine("Work Item {0} finished on Thread {1}",
                i, Thread.CurrentThread.ManagedThreadId);
        }
    }
}
