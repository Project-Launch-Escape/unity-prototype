using System;
using Unity.Entities;

/// <summary>
/// Defines static information for a part.
/// </summary>
[Serializable]
public struct PartDefinition : IComponentData {
    public PartTypeEnum type;
    public PartPlacementFlags canPlaceOn;
}
