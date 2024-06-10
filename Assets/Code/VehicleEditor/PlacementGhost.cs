using Unity.Entities;
using Unity.Physics;

/// <summary>
/// Stores state for the ghost that follows the cursor when you're trying to place a part.
/// </summary>
public struct PlacementGhost : IComponentData {
    public Aabb bounds;
}
