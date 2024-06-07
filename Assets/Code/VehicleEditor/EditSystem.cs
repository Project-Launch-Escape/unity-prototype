using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

partial class EditSystem : SystemBase {

    const int RAYCAST_LENGTH = 1000;

    // vehicle is made up of parts
    // raycast against parts to place new parts or destroy existing ones

    InputSystem_Actions.VehicleEditorActions input;
    bool didSetup = false;

    protected override void OnCreate() {
        input = new InputSystem_Actions().VehicleEditor;
        input.Enable();
    }

    protected override void OnUpdate() {

        if (!didSetup) {
            // todo - move to PlacePart()
            // setup parent relationships for prefab
            var parents = new EntityQueryBuilder(Allocator.Temp)
            .WithAny<PartTag>()
            .Build(this)
            .ToEntityArray(Allocator.Temp);
            foreach (var entity in parents) {
                UnityEngine.Debug.Log(entity);
                foreach (var child in EntityManager.GetBuffer<LinkedEntityGroup>(entity, true)) {
                    UnityEngine.Debug.Log(child); // todo - batch
                    EntityManager.AddComponentData(child.Value, new Parent { Value = entity });
                }
            }
            didSetup = true;
        }

        var physics = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

        if (input.Place.WasPressedThisFrame()) {
            // todo - position/rotation
        }
        else if (input.Delete.WasPressedThisFrame()
            && physics.CastRay(CameraRaycast(), out RaycastHit closestHit)) {
            if (SystemAPI.HasComponent<Parent>(closestHit.Entity)) {
                RemovePart(SystemAPI.GetComponent<Parent>(closestHit.Entity).Value);
            }
        }
    }

    void PlacePart(float3 position, quaternion rotation) {
        // todo
    }

    void RemovePart(Entity e) {
        EntityManager.DestroyEntity(e);
    }

    RaycastInput CameraRaycast() {
        UnityEngine.Ray ray = UnityEngine.Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
        return new RaycastInput {
            Start = ray.origin,
            End = ray.origin + (ray.direction * RAYCAST_LENGTH),
            Filter = CollisionFilter.Default, // vehicle part layer
        };
    }

    protected override void OnDestroy() {
        input.Disable();
    }
}
