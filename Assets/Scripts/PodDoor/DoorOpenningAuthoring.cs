using Unity.Entities;
using UnityEngine;


public class DoorOpenningAuthoring : MonoBehaviour
{
    public GameObject newDoorPrefab;
    public GameObject currentDoor;

    private class Baker : Baker<DoorOpenningAuthoring>
    {
        public override void Bake(DoorOpenningAuthoring authoring)
        {
            Entity e = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(e, new DoorOpenning
            {
                currentDoor = GetEntity(authoring.currentDoor, TransformUsageFlags.Dynamic),
                newDoorPrefab = GetEntity(authoring.newDoorPrefab, TransformUsageFlags.Dynamic)
            }) ;
        }
    }
}

// COMPONENT
public struct DoorOpenning : IComponentData
{
    public Entity currentDoor;
    public Entity newDoorPrefab;
}

