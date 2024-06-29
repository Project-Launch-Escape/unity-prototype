using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public partial class DoorOpenningSystem : SystemBase
{
    protected override void OnCreate(){RequireForUpdate<DoorOpenning>();}
    protected override void OnUpdate()
    {
        if (!Input.GetKey(KeyCode.E)){return;}
        this.Enabled = false;
        Entity entity = new Entity();
        
        foreach ((RefRO<DoorOpenning> doorComponent, RefRO<LocalToWorld> transform) in SystemAPI.Query<RefRO<DoorOpenning>, RefRO<LocalToWorld>>())
        {
            DoorOpenning doorOpenning = SystemAPI.GetSingleton<DoorOpenning>();
            Entity spawnedentity = EntityManager.Instantiate(doorOpenning.newDoorPrefab);

            EntityManager.SetComponentData(spawnedentity, new LocalTransform{Scale = 1f,Rotation = transform.ValueRO.Rotation,Position = transform.ValueRO.Position});
            entity = doorComponent.ValueRO.currentDoor;
        }
        EntityManager.DestroyEntity(entity);
    }
}