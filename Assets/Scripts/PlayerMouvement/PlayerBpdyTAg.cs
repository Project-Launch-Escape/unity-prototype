using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
public class PlayerBpdyTAg : MonoBehaviour
{
    // Baker
    private class Baker : Baker<PlayerBpdyTAg>
    {
        public override void Bake(PlayerBpdyTAg authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlayerBodyTag());
        }
    }
}
public struct PlayerBodyTag : IComponentData { public float x;public float3 mouvement; }