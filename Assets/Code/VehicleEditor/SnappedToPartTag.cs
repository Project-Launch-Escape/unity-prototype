using Unity.Entities;

/// <summary>
/// Tag component that indicates the placement ghost is snapped to this part (so it can be queried). 
/// Should only ever be one.
/// </summary>
public struct SnappedToPartTag : IComponentData {
}
