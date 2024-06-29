using Unity.Entities;
using UnityEngine;

public class VehicleControlAuthoring : MonoBehaviour
{
    //Baker
    private class Baker : Baker<VehicleControlAuthoring>
    {
        public override void Bake(VehicleControlAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new VehicleControl(){throttle = 0f});
        }
    }
}
public struct VehicleControl : IComponentData {public float throttle;}