using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ThreadAndTPLDemo
{
    [TestFixture]
    public class AsyncSafety
    {
        static AsyncSafety()
        {
            Console.SetOut(TestContext.Progress);
        }

        [Test]
        public async Task BewareContinuations()
        {
            var items = Enumerable.Range(0, 1000).ToList();

            var tasks = items.Select(async item =>
            {
                // Code BEFORE the first await is executed, in serial, on the main (caller's) thread. We don't technically need to be thread safe here.

                await Task.Delay(10000);
                
                // However, once we're done awaiting, the 1000 async Tasks we've started all now need to be scheduled back on the thread pool.
                // The continuation code (Console.WriteLine in this case) will be run on a random thread.
                // This continuation MUST be thread safe.
                Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
            });

             // While we await, we release ALL our threads, including the caller's thread. Refer 'There is no thread', by Stephen Cleary
            await Task.WhenAll(tasks);
        }
    }
}
