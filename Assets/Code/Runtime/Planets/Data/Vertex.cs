using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace PLE.Prototype.Runtime.Code.Runtime.Planets.Data
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public float3 Position;
        public float3 Normal;
        public float4 Tangent;
        public float2 UV;
    }
}