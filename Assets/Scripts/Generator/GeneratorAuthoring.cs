using Unity.Entities;
using UnityEngine;

public class GeneratorAuthoring : MonoBehaviour
{
    private class Baker : Baker<GeneratorAuthoring>
    {

        public override void Bake(GeneratorAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Generator());
        }
    }
}

public struct Generator : IComponentData {




}