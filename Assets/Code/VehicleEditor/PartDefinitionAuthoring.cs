using Unity.Entities;
using UnityEngine;

/// <inheritdoc cref="PartDefinition"/>
public class PartDefinitionAuthoring : MonoBehaviour {

    public PartDefinition data;

    public class Baker : Baker<PartDefinitionAuthoring> {
        public override void Bake(PartDefinitionAuthoring authoring)
            => AddComponent(GetEntity(TransformUsageFlags.Dynamic), authoring.data);
    }
}