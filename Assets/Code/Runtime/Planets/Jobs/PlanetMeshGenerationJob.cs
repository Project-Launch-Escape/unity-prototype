using PLE.Prototype.Runtime.Code.Runtime.Planets.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace PLE.Prototype.Runtime.Code.Runtime.Planets.Jobs
{
    [BurstCompile]
    public struct PlanetMeshGenerationJob : IJob
    {
        [WriteOnly]
        public Mesh.MeshData MeshData;
        
        public unsafe void Execute()
        {
            // Generate mesh
            var vertices = GenerateVertices();
            var triangles = GenerateTriangles();
            
            // Configure mesh data
            var vertexAttributeDescriptor = CreateVertexAttributeDescriptor();
            MeshData.SetVertexBufferParams(vertices.Length, vertexAttributeDescriptor);
            MeshData.SetIndexBufferParams(triangles.Length * 3, IndexFormat.UInt16);
            
            // Apply vertices
            var vertexBuffer = MeshData.GetVertexData<Vertex>();
            UnsafeUtility.MemCpy(vertexBuffer.GetUnsafePtr(), vertices.GetUnsafeReadOnlyPtr(), (long) vertices.Length * UnsafeUtility.SizeOf<Vertex>());
            
            // Apply Indices
            var indexBuffer = MeshData.GetIndexData<short>();
            UnsafeUtility.MemCpy(indexBuffer.GetUnsafePtr(), triangles.GetUnsafeReadOnlyPtr(), (long) triangles.Length * UnsafeUtility.SizeOf<Triangle>());
            
            // Configure sub mesh
            var subMesh = new SubMeshDescriptor(0, triangles.Length * 3)
            {
                topology = MeshTopology.Triangles,
                vertexCount = vertices.Length
            };
            MeshData.subMeshCount = 1;
            MeshData.SetSubMesh(0, subMesh);
        }

        private NativeList<Vertex> GenerateVertices()
        {
            var vertices = new NativeList<Vertex>(Allocator.Temp);
            
            vertices.Add(new Vertex
            {
                Position = new float3(-1, 0, -1),
                Normal = new float3(0, 1, 0),
                UV = new float2(0, 0)
            });
            vertices.Add(new Vertex
            {
                Position = new float3(-1, 0, 1),
                Normal = new float3(0, 1, 0),
                UV = new float2(0, 1)
            });
            vertices.Add(new Vertex
            {
                Position = new float3(1, 0, 1),
                Normal = new float3(0, 1, 0),
                UV = new float2(1, 1)
            });
            vertices.Add(new Vertex
            {
                Position = new float3(1, 0, -1),
                Normal = new float3(0, 1, 0),
                UV = new float2(1, 0)
            });

            return vertices;
        }

        private NativeList<Triangle> GenerateTriangles()
        {
            var triangles = new NativeList<Triangle>(Allocator.Temp);
            
            AppendQuad(triangles, 0, 1, 2, 3);

            return triangles;
        }
        
        private void AppendTriangle(NativeList<Triangle> triangles, short a, short b, short c)
        {
            triangles.Add(new Triangle
            {
                Index0 = a,
                Index1 = b,
                Index2 = c
            });
        }
        
        private void AppendQuad(NativeList<Triangle> triangles, short a, short b, short c, short d)
        {
            AppendTriangle(triangles, a, b, c);
            AppendTriangle(triangles, c, d, a);
        }
        
        private NativeArray<VertexAttributeDescriptor> CreateVertexAttributeDescriptor()
        {
            return new NativeArray<VertexAttributeDescriptor>(4, Allocator.Temp)
            {
                // ReSharper disable RedundantArgumentDefaultValue
                [0] = new (VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                [1] = new (VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
                [2] = new (VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4),
                [3] = new (VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
            };
        }
    }
}