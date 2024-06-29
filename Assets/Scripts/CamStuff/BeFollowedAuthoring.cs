using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BeFollowedAuthoring : MonoBehaviour
{
    public Vector3 offset;
    public class Baker : Baker<BeFollowedAuthoring>
    {
        public override void Bake(BeFollowedAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new BeFollowed() {offset = authoring.offset });
        }
    }
}
public struct BeFollowed : IComponentData
{
    public float3 offset;
}

