using System;
using Unity.Entities;

/// <summary>
/// State for EditSystem.
/// </summary>
[Serializable]
public struct EditSystemData : IComponentData {
    public int SelectedPart;
    public int AvailablePartsCount;
}
