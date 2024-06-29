using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;
/*
public partial class EntityPropretySystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<EntityProprety>(); // Run program only if this component exsist and used
    }

    protected override void OnUpdate()
    {
        this.Enabled = false;
        //foreach ((RefRW<EntityProprety> entityproprety,RefRO<RenderBounds> rendering) in SystemAPI.Query<RefRW<EntityProprety>, RefRO<RenderBounds>>())
        foreach (RefRW<EntityProprety> entityproprety in SystemAPI.Query<RefRW<EntityProprety>>())
        {
                entityproprety.ValueRW.radius = 3.14f;
        }
    }
}
*/
public partial struct EntityPropretySystem : ISystem
{
    
    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<EntityProprety>();
    }
    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;
        /*
        foreach (RefRW<EntityProprety> entityProprety in SystemAPI.Query<RefRW<EntityProprety>>())
        {
            
            entityProprety.ValueRW.radius = 10f;
            entityProprety.ValueRW.height = 12f;
        } 
    */
        foreach ((RefRW<EntityProprety> e, RefRO<RenderBounds> b) in SystemAPI.Query<RefRW<EntityProprety>, RefRO<RenderBounds>>())
        {
            //Debug.Log(b.ValueRO.Value.Extents);
            e.ValueRW.size = b.ValueRO.Value.Extents;
            e.ValueRW.radius = b.ValueRO.Value.Extents[0]; // [0] should == [1]
            e.ValueRW.height = b.ValueRO.Value.Extents[1];

        }


    }
}