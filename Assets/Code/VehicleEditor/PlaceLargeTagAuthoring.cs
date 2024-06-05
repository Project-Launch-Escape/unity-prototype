using Unity.Entities;
using UnityEngine;

public class PlaceLargeTagAuthoring : MonoBehaviour {
    public class Baker : Baker<PlaceLargeTagAuthoring> {
        public override void Bake(PlaceLargeTagAuthoring authoring)
            => AddComponent(GetEntity(TransformUsageFlags.Dynamic), typeof(PlaceLargeTag));
    }
}