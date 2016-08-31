using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace IocpThreadPool
{
    public sealed class SafeIocpThreadPool
    {
        // Win32 Function Prototypes
        /// <summary> Win32Func: Create an IO Completion Port Thread Pool </summary>
        [DllImport("Kernel32", CharSet = CharSet.Auto)]
        private static extern SafeFileHandle CreateIoCompletionPort(IntPtr hFile, IntPtr hExistingCompletionPort,
            IntPtr puiCompletionKey, uint uiNumberOfConcurrentThreads);

        /// <summary> Win32Func: Closes an IO Completion Port Thread Pool </summary>
        [DllImport("Kernel32", CharSet = CharSet.Auto)]
        private static extern bool CloseHandle(SafeHandle hObject);

        /// <summary> Win32Func: Posts a context based event into an IO Completion Port Thread Pool </summary>
        [DllImport("Kernel32", CharSet = CharSet.Auto)]
        private static extern bool PostQueuedCompletionStatus(SafeFileHandle hCompletionPort, uint uiSizeOfArgument,
            IntPtr dwCompletionKey, IntPtr pOverlapped);

        /// <summary> Win32Func: Waits on a context based event from an IO Completion Port Thread Pool.
        ///           All threads in the pool wait in this Win32 Function </summary>
        [DllImport("Kernel32", CharSet = CharSet.Auto)]
        private static extern bool GetQueuedCompletionStatus(SafeFileHandle hCompletionPort,
            out uint pSizeOfArgument, out IntPtr dwCompletionKey, out IntPtr ppOverlapped, uint uiMilliseconds);

        // Constants
        /// <summary> SimTypeConst: This represents the Win32 Invalid Handle Value Macro </summary>
        private readonly IntPtr _invalidHandleValue = new IntPtr(-1);

        /// <summary> SimTypeConst: This represents the Win32 INFINITE Macro </summary>
        private readonly uint _inifinite = 0xffffffff;

        /// <summary> SimTypeConst: This tells the IOCP Function to shutdown </summary>
        private readonly IntPtr _shutdownIocpthread = new IntPtr(0x7fffffff);


        // Delegate Function Types
        /// <summary> DelType: This is the type of user function to be supplied for the thread pool </summary>
        public delegate void UserFunction(MyData obj);


        // Private Properties

        /// <summary> SimType: Contains the IO Completion Port Thread Pool handle for this instance </summary>
        private SafeFileHandle GetHandle { get; set; }

        /// <summary> SimType: The maximum number of threads that may be running at the same time </summary>
        private int GetMaxConcurrency { get; set; }

        /// <summary> SimType: The minimal number of threads the thread pool maintains </summary>
        private int GetMinThreadsInPool { get; set; }

        /// <summary> SimType: The maximum number of threads the thread pool maintains </summary>
        private int GetMaxThreadsInPool { get; set; }

        /// <summary> RefType: A serialization object to protect the class state </summary>
        private object GetCriticalSection { get; set; }

        /// <summary> DelType: A reference to a user specified function to be call by the thread pool </summary>
        private UserFunction GetUserFunction { get; set; }

        /// <summary> SimType: Flag to indicate if the class is disposing </summary>
        private bool IsDisposed { get; set; }

        // Public Properties
        private int _mICurThreadsInPool;
        /// <summary> SimType: The current number of threads in the thread pool </summary>
        public int GetCurThreadsInPool
        {
            get { return _mICurThreadsInPool; }
            set { _mICurThreadsInPool = value; }
        }

        /// <summary> SimType: Increment current number of threads in the thread pool </summary>
        private int IncCurThreadsInPool()
        {
            return Interlocked.Increment(ref _mICurThreadsInPool);
        }

        /// <summary> SimType: Decrement current number of threads in the thread pool </summary>
        private int DecCurThreadsInPool()
        {
            return Interlocked.Decrement(ref _mICurThreadsInPool);
        }

        private int _mIActThreadsInPool;

        /// <summary> SimType: The current number of active threads in the thread pool </summary>
        public int GetActThreadsInPool
        {
            get { return _mIActThreadsInPool; }
            set { _mIActThreadsInPool = value; }
        }

        /// <summary> SimType: Increment current number of active threads in the thread pool </summary>
        private int IncActThreadsInPool()
        {
            return Interlocked.Increment(ref _mIActThreadsInPool);
        }

        /// <summary> SimType: Decrement current number of active threads in the thread pool </summary>
        private int DecActThreadsInPool()
        {
            return Interlocked.Decrement(ref _mIActThreadsInPool);
        }

        private int _mICurWorkInPool;

        /// <summary> SimType: The current number of Work posted in the thread pool </summary>
        public int GetCurWorkInPool
        {
            get { return _mICurWorkInPool; }
            set { _mICurWorkInPool = value; }
        }

        /// <summary> SimType: Increment current number of Work posted in the thread pool </summary>
        private int IncCurWorkInPool()
        {
            return Interlocked.Increment(ref _mICurWorkInPool);
        }

        /// <summary> SimType: Decrement current number of Work posted in the thread pool </summary>
        private int DecCurWorkInPool()
        {
            return Interlocked.Decrement(ref _mICurWorkInPool);
        }



        // Constructor, Finalize, and Dispose 
        //***********************************************
        /// <summary> Constructor </summary>
        /// <param name = "iMaxConcurrency"> SimType: Max number of running threads allowed </param>
        /// <param name = "iMinThreadsInPool"> SimType: Min number of threads in the pool </param>
        /// <param name = "iMaxThreadsInPool"> SimType: Max number of threads in the pool </param>
        /// <param name = "pfnUserFunction"> DelType: Reference to a function to call to perform work </param>
        /// <exception cref = "Exception"> Unhandled Exception </exception>
        public SafeIocpThreadPool(int iMaxConcurrency, int iMinThreadsInPool, int iMaxThreadsInPool,
            UserFunction pfnUserFunction)
        {
            // Set initial class state
            GetMaxConcurrency = iMaxConcurrency;
            GetMinThreadsInPool = iMinThreadsInPool;
            GetMaxThreadsInPool = iMaxThreadsInPool;
            GetUserFunction = pfnUserFunction;
            // Init the thread counters
            GetCurThreadsInPool = 0;
            GetActThreadsInPool = 0;
            GetCurWorkInPool = 0;
            // Initialize the Monitor Object
            GetCriticalSection = new object();
            // Set the disposing flag to false
            IsDisposed = false;

            // Create an IO Completion Port for Thread Pool use
            GetHandle = CreateIoCompletionPort(_invalidHandleValue, IntPtr.Zero, IntPtr.Zero,
                (uint)GetMaxConcurrency);

            // Test to make sure the IO Completion Port was created
            if (GetHandle.IsInvalid)
                throw new Exception("Unable To Create IO Completion Port");
            // Allocate and start the Minimum number of threads specified
            var tsThread = new ThreadStart(IocpFunction);
            for (var iThread = 0; iThread < GetMinThreadsInPool; ++iThread)
            {
                // Create a thread and start it
                Thread thThread = new Thread(tsThread);
                thThread.Name = "IOCP " + thThread.GetHashCode();
                thThread.Start();
                // Increment the thread pool count
                IncCurThreadsInPool();
                Console.WriteLine(thThread.Name);
            }
        }

        //***********************************************
        /// <summary> Finalize called by the GC </summary>
        ~SafeIocpThreadPool()
        {
            if (!IsDisposed)
                Dispose();
        }

        //**********************************************
        /// <summary> Called when the object will be shutdown.  This
        ///           function will wait for all of the work to be completed
        ///           inside the queue before completing </summary>
        public void Dispose()
        {
            // Flag that we are disposing this object
            IsDisposed = true;
            // Get the current number of threads in the pool
            var iCurThreadsInPool = GetCurThreadsInPool;
            // Shutdown all thread in the pool
            for (var iThread = 0; iThread < iCurThreadsInPool; ++iThread)
                PostQueuedCompletionStatus(GetHandle, 4, _shutdownIocpthread, IntPtr.Zero);

            // Wait here until all the threads are gone
            while (GetCurThreadsInPool != 0) Thread.Sleep(100);

            // Close the IOCP Handle
            CloseHandle(GetHandle);

        }

        // Private Methods
        //*******************************************
        /// <summary> IOCP Worker Function that calls the specified user function </summary>
        private void IocpFunction()
        {
            while (true)
            {

                // Wait for an event
                uint uiNumberOfBytes;
                IntPtr dwCompletionKey;
                IntPtr lpOverlapped;
                GetQueuedCompletionStatus(GetHandle, out uiNumberOfBytes, out dwCompletionKey, out lpOverlapped, _inifinite);

                if (uiNumberOfBytes <= 0)
                {
                    continue;
                }

                // Decrement the number of events in queue
                DecCurWorkInPool();
                // Was this thread told to shutdown
                if (dwCompletionKey == _shutdownIocpthread)
                    break;
                // Increment the number of active threads
                IncActThreadsInPool();
                // Call the user function
                var gch = GCHandle.FromIntPtr(lpOverlapped);
                MyData obj = (MyData)gch.Target;
                GetUserFunction(obj);
                // Get a lock
                Monitor.Enter(GetCriticalSection);

                // If we have less than max threads currently in the pool
                if (GetCurThreadsInPool < GetMaxThreadsInPool)
                {
                    // Should we add a new thread to the pool
                    if (GetActThreadsInPool == GetCurThreadsInPool)
                    {
                        if (IsDisposed == false)
                        {
                            // Create a thread and start it
                            var tsThread = new ThreadStart(IocpFunction);
                            var thThread = new Thread(tsThread);
                            thThread.Name = "IOCP " + thThread.GetHashCode();
                            thThread.Start();
                            // Increment the thread pool count
                            IncCurThreadsInPool();
                        }
                    }
                }
                // Relase the lock
                Monitor.Exit(GetCriticalSection);
                // Increment the number of active threads
                DecActThreadsInPool();
            }
            // Decrement the thread pool count
            DecCurThreadsInPool();
        }

        // Public Methods
        //******************************************
        /// <summary> IOCP Worker Function that calls the specified user function </summary>
        /// <param name="obj"> SimType: A value to be passed with the event </param>
        /// <exception cref = "Exception"> Unhandled Exception </exception>
        public void PostEvent(MyData obj)
        {
            // Only add work if we are not disposing
            if (IsDisposed) return;
            // Post an event into the IOCP Thread Pool

            var gch = GCHandle.Alloc(obj);
            PostQueuedCompletionStatus(GetHandle, (uint)Marshal.SizeOf(gch), IntPtr.Zero, (IntPtr)gch);

            // Increment the number of item of work
            IncCurWorkInPool();
            // Get a lock
            Monitor.Enter(GetCriticalSection);
            // If we have less than max threads currently in the pool
            if (GetCurThreadsInPool < GetMaxThreadsInPool)
            {
                // Should we add a new thread to the pool
                if (GetActThreadsInPool == GetCurThreadsInPool)
                {
                    if (IsDisposed == false)
                    {
                        // Create a thread and start it
                        var tsThread = new ThreadStart(IocpFunction);
                        var thThread = new Thread(tsThread);
                        thThread.Name = "IOCP " + thThread.GetHashCode();
                        thThread.Start();
                        // Increment the thread pool count
                        IncCurThreadsInPool();
                    }
                }
            }
            // Release the lock
            Monitor.Exit(GetCriticalSection);
        }

        //*****************************************
        /// <summary> IOCP Worker Function that calls the specified user function </summary>
        /// <exception cref = "Exception"> Unhandled Exception </exception>
        public void PostEvent()
        {
            // Only add work if we are not disposing
            if (IsDisposed) return;
            // Post an event into the IOCP Thread Pool
            PostQueuedCompletionStatus(GetHandle, 0, IntPtr.Zero, IntPtr.Zero);

            // Increment the number of item of work
            IncCurWorkInPool();
            // Get a lock
            Monitor.Enter(GetCriticalSection);
            // If we have less than max threads currently in the pool
            if (GetCurThreadsInPool < GetMaxThreadsInPool)
            {
                // Should we add a new thread to the pool
                if (GetActThreadsInPool == GetCurThreadsInPool)
                {
                    if (IsDisposed == false)
                    {
                        // Create a thread and start it
                        var tsThread = new ThreadStart(IocpFunction);
                        var thThread = new Thread(tsThread);
                        thThread.Name = "IOCP " + thThread.GetHashCode();
                        thThread.Start();
                        // Increment the thread pool count
                        IncCurThreadsInPool();
                    }
                }
            }
            // Release the lock
            Monitor.Exit(GetCriticalSection);
        }
    }
}
