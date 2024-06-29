using Unity.Entities;
using UnityEngine;
public class ControlAuthoring : MonoBehaviour
{
    //Baker
    private class Baker : Baker<ControlAuthoring>
    {
        public override void Bake(ControlAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Control() { });
        }
    }
}


public struct Control : IComponentData { }