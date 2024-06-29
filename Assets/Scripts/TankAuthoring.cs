using System.Drawing;
using Unity.Entities;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class TankAuthoring : MonoBehaviour
{
    //Baker
    private class Baker : Baker<TankAuthoring>
    {
        public override void Bake(TankAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new TankComponent() { size = GetComponent<MeshRenderer>().bounds.size, fuel = GetComponent<MeshRenderer>().bounds.size.x * GetComponent<MeshRenderer>().bounds.size.y * GetComponent<MeshRenderer>().bounds.size.z });
        }
    }
}

public struct TankComponent : IComponentData {
    public float3 size;
    public float fuel;}