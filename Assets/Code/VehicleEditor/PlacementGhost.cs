using Unity.Entities;
using Unity.Physics;

public struct PlacementGhost : IComponentData {
    public Aabb bounds;
}
