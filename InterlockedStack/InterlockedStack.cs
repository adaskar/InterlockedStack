using Microsoft.VisualBasic;
using System.Diagnostics;
using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.ComponentModel;

namespace InterlockedStack
{
    public class InterlockedStack : IDisposable
    {
        #region HEAP

        [Flags()]
        private enum HEAP_FLAGS : uint
        {
            HEAP_GENERATE_EXCEPTIONS = 0x00000004,
            HEAP_NO_SERIALIZE = 0x00000001,
            HEAP_ZERO_MEMORY = 0x00000008,
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcessHeap();

        [DllImport("kernel32.dll")]
        private static extern IntPtr HeapAlloc(IntPtr hHeap, HEAP_FLAGS dwFlags, IntPtr dwBytes);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool HeapFree(IntPtr hHeap, HEAP_FLAGS dwFlags, IntPtr lpMem);

        #endregion

        #region ILOCKED

        [DllImport("kernel32.dll")]
        private static extern void InitializeSListHead(IntPtr ListHead);

        [DllImport("kernel32.dll")]
        private static extern IntPtr InterlockedPushEntrySList(IntPtr ListHead, IntPtr ListEntry);

        [DllImport("kernel32.dll")]
        private static extern IntPtr InterlockedPopEntrySList(IntPtr ListHead);

        #endregion

        #region STRUCT SIZE

        [StructLayout(LayoutKind.Sequential)]
        internal struct ListHead
        {
            private IntPtr p0;
            private IntPtr p1;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ListEntry
        {
            private IntPtr pNext;
            internal IntPtr pUser;
        }

        #endregion

        IntPtr _head;
        IntPtr _heap;

        private int _count;
        public int Count { get { return Interlocked.CompareExchange(ref _count, 0, 0); } }

        public InterlockedStack()
        {
            _heap = GetProcessHeap();
            if (_heap == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            _head = HeapAlloc(_heap, HEAP_FLAGS.HEAP_ZERO_MEMORY, new IntPtr(Marshal.SizeOf<ListHead>()));
            if (_head == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            InitializeSListHead(_head);
        }

        public IntPtr Push(IntPtr ptr)
        {
            IntPtr p;
            ListEntry le;

            p = HeapAlloc(_heap, HEAP_FLAGS.HEAP_ZERO_MEMORY, new IntPtr(Marshal.SizeOf<ListEntry>()));
            if (p == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }
            Marshal.WriteIntPtr(IntPtr.Add(p, IntPtr.Size), ptr);

            Interlocked.Increment(ref _count);
            return InterlockedPushEntrySList(_head, p);
        }

        public IntPtr Pop()
        {
            IntPtr p;
            IntPtr pUser;

            p = InterlockedPopEntrySList(_head);
            if (p == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }
            Interlocked.Decrement(ref _count);

            pUser = Marshal.PtrToStructure<ListEntry>(p).pUser;
            HeapFree(_heap, 0, p);

            return pUser;
        }

        public void Dispose()
        {
            if (_head != IntPtr.Zero)
            {
                HeapFree(_heap, 0, _head);
                _head = IntPtr.Zero;
            }
        }
    }
}