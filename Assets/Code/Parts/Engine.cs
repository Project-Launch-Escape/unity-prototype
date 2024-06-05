using System;
using Unity.Entities;

[Serializable]
public struct Engine : IComponentData {
    public int Thrust;
    public int SpecificImpulse;
}
