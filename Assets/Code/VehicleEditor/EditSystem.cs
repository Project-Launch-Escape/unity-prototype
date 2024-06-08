using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

partial class EditSystem : SystemBase {

    const int RAYCAST_LENGTH = 1000;

    // vehicle is made up of parts

    // place using drag/drop with modifier key to add more of the same
    // raycast against parts to destroy them

    // parts must have a flat child hierarchy and 1x scale

    InputSystem_Actions.VehicleEditorActions input;
    bool didSetup = false;

    protected override void OnCreate() {
        input = new InputSystem_Actions().VehicleEditor;
        input.Enable();
    }

    protected override void OnUpdate() {

        var entityActions = new EntityCommandBuffer(Allocator.Temp);

        if (!didSetup) { // entities don't exist when OnCreate is called
            // setup parent relationships for test parts
            var parents = new EntityQueryBuilder(Allocator.Temp)
                .WithAny<PartDefinition>()
                .Build(this)
                .ToEntityArray(Allocator.Temp);
            foreach (var entity in parents) {
                entityActions = SetPartRootRelationships(entityActions, entity);
            }
            didSetup = true;
        }

        var physics = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        bool raycastHit = physics.CastRay(CameraRaycast(), out RaycastHit closestHit);

        if (raycastHit) {


            if (input.Place.WasPressedThisFrame()) {
                Entity currentPart = SystemAPI.GetSingleton<EditSystemData>().CurrentPart;
                var newPart = entityActions.Instantiate(currentPart);
                entityActions = SetPartRootRelationships(entityActions, newPart);
            }
            else if (input.Delete.WasPressedThisFrame()) {
                if (SystemAPI.HasComponent<RelatedToPart>(closestHit.Entity)) {
                    entityActions.DestroyEntity(SystemAPI.GetComponent<RelatedToPart>(closestHit.Entity).Root);
                }
            }
        }

        entityActions.Playback(EntityManager);
    }

    // prefabs don't have transform parent/child relationships set up, which we need for deleting things
    private EntityCommandBuffer SetPartRootRelationships(EntityCommandBuffer entityActions, Entity currentPart) {
        if (EntityManager.HasComponent<LinkedEntityGroup>(currentPart)) {
            foreach (var child in EntityManager.GetBuffer<LinkedEntityGroup>(currentPart, true)) {
                if (child.Value == currentPart) {
                    continue;
                }
                entityActions.AddComponent(child.Value, new RelatedToPart { Root = currentPart });
            }
        }
        return entityActions;
    }

    RaycastInput CameraRaycast() {
        UnityEngine.Ray ray = UnityEngine.Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
        return new RaycastInput {
            Start = ray.origin,
            End = ray.origin + (ray.direction * RAYCAST_LENGTH),
            Filter = new CollisionFilter { BelongsTo = (1 << 6), CollidesWith = (1 << 6) }, // vehicle part layer
        };
    }

    protected override void OnDestroy() {
        input.Disable();
    }
}
