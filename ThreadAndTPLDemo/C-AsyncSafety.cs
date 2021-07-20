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
                await Task.Delay(10000);
                Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
            });

            await Task.WhenAll(tasks);
        }
    }
}
