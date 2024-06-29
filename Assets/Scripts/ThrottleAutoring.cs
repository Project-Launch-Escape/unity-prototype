using Unity.Entities;
using UnityEngine;

public class ThrottleAutoring : MonoBehaviour
{
    public float max;
    public float min;
    public GameObject player;
    // BAKER
    private class Baker : Baker<ThrottleAutoring>
    {
        public override void Bake(ThrottleAutoring authoring)
        {
            Entity e = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(e, new Throttle
            {
                max = authoring.max,
                min = authoring.min,
                value = authoring.min,
                entity = GetEntity(authoring.player, TransformUsageFlags.Dynamic)
            });
        }
    }
}

public struct Throttle : IComponentData
{
    public float max;
    public float min;
    public float value;
    public Entity entity;
}