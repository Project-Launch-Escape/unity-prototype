using System;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// State for EditSystem.
/// </summary>
[Serializable]
public struct EditSystemData : IComponentData {

    public int SelectedPart;
    public int AvailablePartsCount;

    // this stands as a monument to my sins
    // i should have looked at how ksp did it
    public bool IsSnapping;
    public float3 SnapPosition;
    public quaternion SnapRotation;
    public bool WasAgainstPartBeforeSnapping;
    public float3 LastGhostPositionBeforeSnap;
}
