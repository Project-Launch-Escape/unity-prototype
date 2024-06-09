using System;
using Unity.Entities;

[Serializable]
public struct EditSystemData : IComponentData {
    public int SelectedPart;
    public int AvailablePartsCount;
}
