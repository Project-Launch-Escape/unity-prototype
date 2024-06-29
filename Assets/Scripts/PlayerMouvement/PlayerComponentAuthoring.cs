using Unity.Entities;
using UnityEngine;

public class PlayerComponentAuthoring : MonoBehaviour
{
    public class Baker : Baker<PlayerComponentAuthoring>
    {
        public override void Bake(PlayerComponentAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new PlayerComponent());
        }
    }
}
public struct PlayerComponent : IComponentData
{
}
