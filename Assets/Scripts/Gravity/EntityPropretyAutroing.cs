using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
public class EntityPropretyAutroing : MonoBehaviour
{
    public float mass;
    public float radius;
    public float height;
    public float3 size;

    public class Baker : Baker<EntityPropretyAutroing>
    {
        public override void Bake(EntityPropretyAutroing authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EntityProprety { mass = authoring.mass, radius = authoring.radius, height = authoring.height , size = authoring.size});
        }
    }
}

public partial struct EntityProprety : IComponentData
{
    public float mass;
    public float radius;
    public float height;
    public float3 size;
}