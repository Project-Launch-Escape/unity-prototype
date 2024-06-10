using System.Runtime.InteropServices;
using Unity.Mathematics;
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