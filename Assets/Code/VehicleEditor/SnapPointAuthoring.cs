using Unity.Entities;
using UnityEngine;

/// <inheritdoc cref="SnapPoint"/>>
public class SnapPointAuthoring : MonoBehaviour {

    public SnapPoint data;

    public class Baker : Baker<SnapPointAuthoring> {
        public override void Bake(SnapPointAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, authoring.data);
        }
    }
}