using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ThreadAndTPLDemo
{
    [TestFixture]
    public class ThreadSafety
    {
        // This is just a hack to stop Test runners from 'buffering' console output, so that we can see Console output progress in realtime.
        static ThreadSafety()
        {
            Console.SetOut(TestContext.Progress);
        }

        // Here we have a number of threads (as determined by default by TPL) each adding to a common, shared variable. No barrier means that the threads interfere with the results of other concurrent threads 
        [Test]
        public void _01ARaceCondition()
        {
            const int numLoops = 100000;
            var sum = 0;
            Parallel.For(0, numLoops, 
                i => sum++);
            Assert.AreEqual(numLoops, sum);
        }

        // We solve the race condition by providing a barrier (sequential lock, in this case).
        [Test]
        public void _01B_FixRaceCondition()
        {
            const int numLoops = 1000000;
            var sum = 0;
            // Obviously this is a naive example. But even in divide and conquer approaches, contention will exist, and synchronisation is almost always needed
            Parallel.For(0, numLoops, 
                i => Interlocked.Increment(ref sum));
            Assert.AreEqual(numLoops, sum);
        }

        [Test]
        public void _02A_NonThreadSafeCollections()
        {
            const int numLoops = 100000;
            var strings = new List<string>();
            // With List, will get all kinds of random exceptions / behaviour. IndexOutOfRange, Destination array was not long enough etc
            Parallel.For(0, numLoops, i => strings.Add(i.ToString()));
            Assert.AreEqual(numLoops, strings.Count);
        }

        [Test]
        public void _02B_ConcurrentCollections()
        {
            const int numLoops = 1000000;
            // The collections in System.Collections.Concurrent have built in Thread Safety
            var strings = new ConcurrentBag<string>();
            Parallel.For(0, numLoops, i => strings.Add(i.ToString()));
            Assert.AreEqual(numLoops, strings.Count);
        }


        // NB - x86 Release Build
        // Tearing happens because the processor cannot 'atomically' update a value which is larger than the register word
        [Test]
        public void _10_Tearing_UnsafeNonAtomicOperation()
        {
            const long numIterations = 100000000L;
            // Guid is 16 bytes / 128 bits
            var guid1 = new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff");
            var guid2 = new Guid("00000000-0000-0000-0000-000000000000");
            var theGuid = guid1;
            var writerThread1 = new Thread(() => DoLoopedAction(numIterations, () => { theGuid = guid1; }));
            var writerThread2 = new Thread(() => DoLoopedAction(numIterations, () => { theGuid = guid2; }));
            bool atomic = true;
            var readerThread = new Thread(
                () => NonAtomicReaderAsserter(() =>
                {
                    var readValue = theGuid;
                    atomic = readValue == guid1 || readValue == guid2;
                    Assert.True(atomic, $"I read {readValue}");
                }))
            {
                IsBackground = true
            };
            writerThread1.Start();
            writerThread2.Start();
            readerThread.Start();

            writerThread1.Join();
            writerThread2.Join();
            Assert.True(atomic);

            // So what exactly does a 64 bit processor mean?
            // Which primitives and structs are atomic ?
            // (decimal, DateTime, ...)
            // Reference types are however atomic (64 bit `pointers` / selectors)
            // We're still on the wrong track -> Mutability is the enemy
        }


        private static void DoLoopedAction(long numIterations, Action someMutation)
        {
            for (var loop = 0L; loop < numIterations; loop++)
            {
                someMutation();
            }
        }

        private static void NonAtomicReaderAsserter(Action someAssertion)
        {
            try
            {
                while (true)
                {
                    someAssertion();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Oops - NonAtomic - {ex.Message}");
                throw;
            }
        }

    }
}
