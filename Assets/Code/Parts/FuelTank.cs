using System;
using Unity.Entities;

[Serializable]
public struct FuelTank : IComponentData {
    public int Capacity;
}
