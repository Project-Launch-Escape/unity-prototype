using System;
using Unity.Entities;

/// <summary>
/// Indicates where parts can snap to. 
/// Should be placed on an immediate child object of the part, with it's up facing outward from the part.
/// </summary>
[Serializable]
public struct SnapPoint : IComponentData {
    public PartPlacementFlags belongsTo;
    public PartPlacementFlags connectsWith;
}
