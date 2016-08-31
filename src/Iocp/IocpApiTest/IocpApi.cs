using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace IocpApiTest
{
    public class IocpApi
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern SafeFileHandle CreateIoCompletionPort(IntPtr fileHandle, IntPtr existingCompletionPort,
            IntPtr completionKey, uint numberOfConcurrentThreads);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GetQueuedCompletionStatus(SafeFileHandle completionPort,
            out uint lpNumberOfBytesTransferred, out IntPtr lpCompletionKey, out IntPtr lpOverlapped,
            uint dwMilliseconds);

        [DllImport("Kernel32", CharSet = CharSet.Auto)]
        private static extern bool PostQueuedCompletionStatus(SafeFileHandle completionPort,
            uint dwNumberOfBytesTransferred, IntPtr dwCompletionKey, IntPtr lpOverlapped);

        public static unsafe void TestIocpApi()
        {
            var completionPort = CreateIoCompletionPort(new IntPtr(-1), IntPtr.Zero, IntPtr.Zero, 1);
            if (completionPort.IsInvalid)
                Console.WriteLine("CreateIoCompletionPort 出错:{0}", Marshal.GetLastWin32Error());

            var thread = new Thread(ThreadProc);
            thread.Start(completionPort);

            var ioData = new IOData();
            var gch = GCHandle.Alloc(ioData);
            ioData.Data = "hi,我是sujunxuan,你是谁?";
            Console.WriteLine("{0}-主线程发送数据", Thread.CurrentThread.GetHashCode());

            PostQueuedCompletionStatus(completionPort, (uint)sizeof(IntPtr), IntPtr.Zero, (IntPtr)gch);
            var ioData2 = new IOData();
            var gch2 = GCHandle.Alloc(ioData2);
            ioData2.Data = "关闭工作线程吧";
            Console.WriteLine("{0}-主线程发送输出", Thread.CurrentThread.GetHashCode());
            PostQueuedCompletionStatus(completionPort, 4, IntPtr.Zero, (IntPtr) gch2);
            Console.WriteLine("主线程执行完毕");
            Console.ReadKey();

        }

        private static void ThreadProc(object completionPortId)
        {
            var completionPort = (SafeFileHandle)completionPortId;

            while (true)
            {
                uint bytesTransferred;
                IntPtr perHandleData;
                IntPtr lpOverlapped;
                Console.WriteLine("{0}-工作线程准备接受数据", Thread.CurrentThread.GetHashCode());
                GetQueuedCompletionStatus(completionPort, out bytesTransferred, out perHandleData, out lpOverlapped, 0xffffffff);
                if (bytesTransferred <= 0)
                    continue;

                var gch = GCHandle.FromIntPtr(lpOverlapped);
                var data = (IOData)gch.Target;
                Console.WriteLine("{0}-工作线程收到数据：{1}", Thread.CurrentThread.GetHashCode(), data.Data);

                gch.Free();
                if (data.Data != "关闭工作线程吧") continue;
                Console.WriteLine("收到退出指令，正在退出");
                completionPort.Dispose();
                break;
            }
        }
    }
}
