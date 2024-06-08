using System;
using Unity.Entities;

[Serializable]
public struct PartDefinition : IComponentData {
    public PartTypeEnum type;
    public PartPlacementFlags canPlaceOn;
}
