using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterlockedStack
{
    public static class Program
    {
        private static int _n;

        public static int Main(string[] args)
        {
            InterlockedStack stack = new InterlockedStack();

            for (int i = 0; i < 10; ++i)
            {
                ThreadPool.QueueUserWorkItem((o) =>
                {
                    for (long i = Interlocked.Increment(ref _n); i < 100; i = Interlocked.Increment(ref _n))
                    {
                        stack.Push(new IntPtr(i));
                    }
                });
            }

            SpinWait.SpinUntil(() =>
            {
                Thread.Sleep(1);
                Debug.WriteLine($"stack.Count: {stack.Count}");
                return Interlocked.CompareExchange(ref _n, 0, 0) >= 100;
            });

            for (int i = 0; i < 10; ++i)
            {
                ThreadPool.QueueUserWorkItem((o) =>
                {
                    for (IntPtr p = stack.Pop(); p != IntPtr.Zero; p = stack.Pop())
                    {
                        Console.WriteLine(p);
                    }
                });
            }

            Console.ReadLine();
            return 0;
        }
    }
}
