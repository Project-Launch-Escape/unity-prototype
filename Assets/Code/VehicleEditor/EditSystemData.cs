using System;
using Unity.Entities;

[Serializable]
public struct EditSystemData : IComponentData {
    public int SelectedPart;
    public int AvailablePartsCount;

    public void IncrementSelection() {
        SelectedPart = (SelectedPart + 1) % AvailablePartsCount;
    }
}
