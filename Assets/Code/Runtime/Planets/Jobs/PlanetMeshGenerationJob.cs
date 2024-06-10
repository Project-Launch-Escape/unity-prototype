using JetBrains.Annotations;
using PLE.Prototype.Runtime.Code.Runtime.Planets.Data;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Entities.EntitiesJournaling;
using static UnityEditor.MaterialProperty;
using static UnityEngine.UIElements.UxmlAttributeDescription;

namespace PLE.Prototype.Runtime.Code.Runtime.Planets.Jobs
{
    [BurstCompile]
    public struct PlanetMeshGenerationJob : IJob
    {
        [WriteOnly]
        public Mesh.MeshData MeshData;

        public float cube;
        public int MaxIteration;
        public int Radius;
        public Vector3 campos;
        public Vector3 selfposition;

        
        public bool doBissect(float distance,int iteration)
        {
            // I could use a better structure like a dictionnary defined in a start 
            if ((iteration <= 5 ||  // Do 5 iteration for everything
                (distance < 6 && iteration <= 6) || // Do 6 iteration for distance < 6
                (distance < 5 && iteration <= 7) || // Do 7 iteration for distance < 5
                (distance < 4 && iteration <= 8) || 
                (distance < 3 && iteration <= 9) ||
                (distance < 1.50 && iteration <= 10) || 
                (distance < 1.00 && iteration <= 11) || 
                (distance < 0.75 && iteration <= 12) || 
                (distance < 0.50 && iteration <= 13) || 
                (distance < 0.25 && iteration <= 14)))
            { return true; }else { return false; }
        }

        public unsafe void Execute()
        {
            selfposition = float3.zero;
            var vertices = new NativeList<Vertex>(Allocator.Temp);
            var triangles = new NativeList<Triangle>(Allocator.Temp);

            // TOP   FRONT  BACK    RIGHT   LEFT (Views)
            // 20    02     64      40      26  
            // 64    13     75      51      37
            vertices.Add(new Vertex { Position = math.normalize(new float3(+1,+1,+1)), Normal = math.normalize(new float3(+1, +1, +1)), UV = math.normalize(new float2(+1, +1)) });//0
            vertices.Add(new Vertex { Position = math.normalize(new float3(+1,-1,+1)), Normal = math.normalize(new float3(+1, -1, +1)), UV = math.normalize(new float2(+1, +1)) });//1

            vertices.Add(new Vertex { Position = math.normalize(new float3(-1,+1,+1)), Normal = math.normalize(new float3(-1, +1, +1)), UV = math.normalize(new float2(-1, +1)) });//2
            vertices.Add(new Vertex { Position = math.normalize(new float3(-1,-1,+1)), Normal = math.normalize(new float3(-1, -1, +1)), UV = math.normalize(new float2(-1, +1)) });//3

            vertices.Add(new Vertex { Position = math.normalize(new float3(+1,+1,-1)), Normal = math.normalize(new float3(+1, +1, -1)), UV = math.normalize(new float2(+1, -1)) });//4
            vertices.Add(new Vertex { Position = math.normalize(new float3(+1,-1,-1)), Normal = math.normalize(new float3(+1, -1, -1)), UV = math.normalize(new float2(+1, -1)) });//5

            vertices.Add(new Vertex { Position = math.normalize(new float3(-1,+1,-1)), Normal = math.normalize(new float3(-1, +1, -1)), UV = math.normalize(new float2(-1, -1)) });//6
            vertices.Add(new Vertex { Position = math.normalize(new float3(-1,-1,-1)), Normal = math.normalize(new float3(-1, -1, -1)), UV = math.normalize(new float2(-1, -1)) });//7

            Dictionary<float3, int> vertexDict = new Dictionary<float3, int>();
            int T =0;
            foreach( var thing in vertices){vertexDict[thing.Position] = T;T++;}

            // TOP
            triangles.Add(new Triangle { Index0 = (short)4, Index1 = (short)2, Index2 = (short)0});
            triangles.Add(new Triangle { Index0 = (short)2, Index1 = (short)4, Index2 = (short)6});
            // BOT
            triangles.Add(new Triangle { Index0 = (short)3, Index1 = (short)5, Index2 = (short)1});
            triangles.Add(new Triangle { Index0 = (short)5, Index1 = (short)3, Index2 = (short)7});
            // FRONT
            triangles.Add(new Triangle { Index0 = (short)3, Index1 = (short)0, Index2 = (short)2 });
            triangles.Add(new Triangle { Index0 = (short)0, Index1 = (short)3, Index2 = (short)1 });
            // BACK
            triangles.Add(new Triangle { Index0 = (short)5, Index1 = (short)6, Index2 = (short)4 });
            triangles.Add(new Triangle { Index0 = (short)6, Index1 = (short)5, Index2 = (short)7 });
            // RIGHT
            triangles.Add(new Triangle { Index0 = (short)1, Index1 = (short)4, Index2 = (short)0 });
            triangles.Add(new Triangle { Index0 = (short)4, Index1 = (short)1, Index2 = (short)5 });
            // Left
            triangles.Add(new Triangle { Index0 = (short)7, Index1 = (short)2, Index2 = (short)6 });
            triangles.Add(new Triangle { Index0 = (short)2, Index1 = (short)7, Index2 = (short)3 });



            for (int i=0; i < MaxIteration; i++) {  //iteration
                int l = triangles.Length;
                for (int index = 0; index < l; index++)
                {
                    float3 pa = vertices[triangles[index].Index0].Position;
                    float3 pb = vertices[triangles[index].Index1].Position;
                    float3 pc = vertices[triangles[index].Index2].Position;

                    float3 m = math.normalize((pa + pb + pc) / 3); // middle point / average
                    if (math.dot(campos, m) < -0.1) { if (i > 3) continue; }  // Dont divide back side

                    float d = (campos - new Vector3(m.x, m.y, m.z)).magnitude; // distance
                    float3 newp = math.normalize((float3)((pa + pb) / 2)); // Point that splits triangles     
                    
                    int p;
                    // If the triangel is already devided devide it (if there is a point in the middle of the hypotenus) if not then create the new point if not to far away
                    if (!vertexDict.TryGetValue(newp, out p))
                    {
                        if (!doBissect(distance: d, iteration: i)) {continue;} // If dont generate then dont
                        p = vertices.Length;
                        vertexDict[newp] = p; // not sure about this
                        vertices.Add(new Vertex { Position = newp, Normal = newp, UV = new float2(newp.x, newp.z) }) ;//6
                    }
                    // We need to destroy the old triangle and add 2 new triangles 
                    triangles.Add(new Triangle { Index0 = triangles[index].Index1, Index1 = triangles[index].Index2, Index2 = (short)p });
                    triangles[index] = new Triangle { Index1 = triangles[index].Index0, Index0 = triangles[index].Index2, Index2 = (short)p };
                }
            }

            // Configure mesh data
            var vertexAttributeDescriptor = CreateVertexAttributeDescriptor();
            MeshData.SetVertexBufferParams(vertices.Length, vertexAttributeDescriptor);
            MeshData.SetIndexBufferParams(triangles.Length * 3, IndexFormat.UInt16);

            // Apply vertices
            var vertexBuffer = MeshData.GetVertexData<Vertex>();
            UnsafeUtility.MemCpy(vertexBuffer.GetUnsafePtr(), vertices.GetUnsafeReadOnlyPtr(), (long)vertices.Length * UnsafeUtility.SizeOf<Vertex>());

            // Apply Indices
            var indexBuffer = MeshData.GetIndexData<short>();
            UnsafeUtility.MemCpy(indexBuffer.GetUnsafePtr(), triangles.GetUnsafeReadOnlyPtr(), (long)triangles.Length * UnsafeUtility.SizeOf<Triangle>());

            // Configure sub mesh
            var subMesh = new SubMeshDescriptor(0, triangles.Length * 3)
            {
                topology = MeshTopology.Triangles,
                vertexCount = vertices.Length
            };
            MeshData.subMeshCount = 1;
            MeshData.SetSubMesh(0, subMesh);
        }
        private NativeArray<VertexAttributeDescriptor> CreateVertexAttributeDescriptor()
        {
            return new NativeArray<VertexAttributeDescriptor>(4, Allocator.Temp)
            {
                // ReSharper disable RedundantArgumentDefaultValue
                [0] = new(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                [1] = new(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
                [2] = new(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4),
                [3] = new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
            };
        }
    }
}