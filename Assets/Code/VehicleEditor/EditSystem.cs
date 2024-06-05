using Unity.Entities;
using Unity.Physics;

partial class EditSystem : SystemBase {

    const int RAYCAST_LENGTH = 1000;

    // vehicle is made up of parts
    // raycast against parts to place new parts or destroy existing ones

    // parts have PartTags to denote root objects and PlaceTags + PhysicsColliders to denote adding points
    // parts are collected in LinkedEntityGroups - if one is destroyed all are destroyed

    InputSystem_Actions.VehicleEditorActions input;

    protected override void OnCreate() {
        input = new InputSystem_Actions().VehicleEditor;
        input.Enable();
    }

    protected override void OnUpdate() {
        var physics = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

        if (input.Place.WasPressedThisFrame()) {
            // todo - position/rotation
        }
        else if (input.Delete.WasPressedThisFrame() && physics.CastRay(CameraRaycast(), out RaycastHit closestHit)) {
            RemovePart(closestHit.Entity); // expect it to be grouped
        }
    }

    void PlacePart(Entity e) {
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
            Filter = new CollisionFilter { BelongsTo = (1 << 6) },
        };
    }

    protected override void OnDestroy() {
        input.Disable();
    }
}
