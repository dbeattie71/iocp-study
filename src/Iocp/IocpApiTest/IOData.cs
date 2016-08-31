using System.Runtime.InteropServices;

namespace IocpApiTest
{
    [StructLayout(LayoutKind.Sequential)]
    public class IOData
    {
        public string Data { get; set; }
    }
}
