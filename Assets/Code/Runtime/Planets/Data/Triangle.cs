using System.Runtime.InteropServices;

namespace PLE.Prototype.Runtime.Code.Runtime.Planets.Data
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Triangle
    {
        public short Index0;
        public short Index1;
        public short Index2;
    }
}