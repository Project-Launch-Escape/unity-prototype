using System;
using PLE.Prototype.Runtime.Code.Runtime.Planets.Jobs;
using Unity.Jobs;
using UnityEngine;
using Unity.Entities;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;

namespace PLE.Prototype.Runtime.Code.Runtime.Planets
{
    // This class is only for testing, it isn't ECS and also not a proper unit test
    public class PlanetMeshGenerationTest : MonoBehaviour
    {
        [SerializeField]
        private Shader debugShader;
        
        private Mesh mesh;
        private GameObject previewGameObject;
        private Material debugMaterial;
        [Range(0,15)]
        public int MaxIteration;

        public Transform cameraPosition;
        public Entity Entity;
        private void Update()
        {
            if (mesh)
                Destroy(mesh);

            if (previewGameObject)
                Destroy(previewGameObject);

            if (debugMaterial)
                Destroy(debugMaterial);

            mesh = new Mesh();
            var meshDataArray = Mesh.AllocateWritableMeshData(mesh);

            EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var job = new PlanetMeshGenerationJob
            {
                MeshData = meshDataArray[0],
                MaxIteration = MaxIteration,
                campos=cameraPosition.position
        };
            job.Run();
        
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
            mesh.RecalculateBounds();

            debugMaterial = new Material(debugShader);            
        
            previewGameObject = new GameObject(nameof(PlanetMeshGenerationTest));
            previewGameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            previewGameObject.AddComponent<MeshRenderer>().sharedMaterial = debugMaterial;
        }

        private void OnDestroy()
        {
            if(mesh)
                Destroy(mesh);
            
            if(previewGameObject)
                Destroy(previewGameObject);
            
            if(debugMaterial)
                Destroy(debugMaterial);
        }
    }
}