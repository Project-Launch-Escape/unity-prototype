using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public partial struct GeneratorSystem : ISystem
{
    public void OnCreate(ref SystemState state) { state.RequireForUpdate<Generator>(); }

    public void OnUpdate(ref SystemState state) {
        state.Enabled = false;
        foreach (RefRW<Generator> onRailBodyComponent in SystemAPI.Query<RefRW<Generator>>())
        {


        }





    }

}