using Unity.Entities;
using UnityEngine;

public class PartTagAuthoring : MonoBehaviour {

    public class Baker : Baker<PartTagAuthoring> {
        public override void Bake(PartTagAuthoring authoring)
            => AddComponent(GetEntity(TransformUsageFlags.Dynamic), typeof(PartTag));
    }
}