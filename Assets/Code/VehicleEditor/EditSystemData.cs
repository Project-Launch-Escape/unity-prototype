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

    public int ID;// Each time parts are placed (dont matter how many, if it is just in 1 click) they all have the same ID
    public int sym;
    public int it;

}
