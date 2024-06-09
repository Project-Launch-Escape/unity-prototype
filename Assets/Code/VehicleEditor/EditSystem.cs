using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Debug = UnityEngine.Debug;

partial class EditSystem : SystemBase {

    // vehicle is made up of parts

    // place using click/drop with modifier key to add more of the same
    // raycast against parts to destroy part trees with modifier to delete only that part

    // parts must have a flat child hierarchy and 1x scale

    // in future this should be extracted to an input service
    InputSystem_Actions.VehicleEditorActions input;

    protected override void OnCreate() {
        input = new InputSystem_Actions().VehicleEditor;
        input.Enable();
    }

    protected override void OnUpdate() {
        var actionQueue = new EntityCommandBuffer(Allocator.Temp);
        var state = SystemAPI.GetSingleton<EditSystemData>();

        RaycastInput raycast = CameraRaycast(out float3 raycastDir);
        bool raycastHit = GetPartFrom(raycast, out RaycastHit closestHit, out Entity raycastPart);

        if (SystemAPI.TryGetSingletonEntity<PlacementGhostTag>(out Entity placementGhost)) {
            // move ghost
            LocalTransform placeTransform;
            if (raycastHit) {
                placeTransform = new LocalTransform {
                    Position = closestHit.Position,
                    Rotation = quaternion.LookRotationSafe(closestHit.SurfaceNormal, new(0, 1, 0)),
                    Scale = 1
                };
            }
            else {
                placeTransform = new LocalTransform {
                    Position = raycast.Start + raycastDir * UnityEngine.Camera.main.transform.localPosition.magnitude,
                    Rotation = quaternion.identity,
                    Scale = 1
                };
            }
            actionQueue.SetComponent(placementGhost, placeTransform);

            if (input.Place.WasPressedThisFrame()) {
                PlacePart(ref actionQueue, SystemAPI.GetSingletonBuffer<PartsBuffer>()[state.SelectedPart].Value, placeTransform, raycastHit ? raycastPart : Entity.Null);
            }
        }
        else if (input.Delete.WasPressedThisFrame() && raycastHit) {
            // todo - handling for single part deletion
            DeletePartTree(ref actionQueue, raycastPart);
        }

        if (input.Summon.WasPressedThisFrame()) {
            SetupPlacementGhost(ref actionQueue);
        }
        else if (input.Cancel.WasPressedThisFrame()) {
            DestroyPlacementGhost(ref actionQueue);
        }

        actionQueue.Playback(EntityManager);
    }

    void DeletePartTree(ref EntityCommandBuffer actionQueue, Entity rootPart) {
        var deletedParts = new NativeList<int>(Allocator.Temp);
        var deleteFrontier = new NativeQueue<Entity>(Allocator.Temp);
        deleteFrontier.Enqueue(rootPart);

        while (!deleteFrontier.IsEmpty()) {
            var next = deleteFrontier.Dequeue();
            if (deletedParts.Contains(next.Index)) {
                continue;
            }
            foreach (var child in EntityManager.GetBuffer<PartChildBuffer>(next)) {
                deleteFrontier.Enqueue(child.Value);
            }
            actionQueue.DestroyEntity(next);
            deletedParts.Add(next.Index);
        }
    }

    /// <returns>true if ghost existed</returns>
    bool DestroyPlacementGhost(ref EntityCommandBuffer actionQueue) {
        if (SystemAPI.TryGetSingletonEntity<PlacementGhostTag>(out Entity placementGhost)) {
            actionQueue.DestroyEntity(placementGhost);
            return true;
        }
        return false;
    }

    void SetupPlacementGhost(ref EntityCommandBuffer actionQueue) {
        var state = SystemAPI.GetSingletonRW<EditSystemData>();
        var parts = SystemAPI.GetSingletonBuffer<PartsBuffer>();

        if (DestroyPlacementGhost(ref actionQueue)) {
            state.ValueRW.IncrementSelection();
        }

        var ghost = EntityManager.Instantiate(parts[state.ValueRO.SelectedPart].Value);
        actionQueue.AddComponent<PlacementGhostTag>(ghost);
        if (EntityManager.HasComponent<LinkedEntityGroup>(ghost)) {
            foreach (var child in EntityManager.GetBuffer<LinkedEntityGroup>(ghost, true)) {
                actionQueue.RemoveComponent<PhysicsCollider>(child.Value); // don't interfere with my raycast!!!
            }
        }
    }

    /// <returns>true if part found</returns>
    bool GetPartFrom(RaycastInput raycast, out RaycastHit closestHit, out Entity part) {
        if (!SystemAPI.GetSingleton<PhysicsWorldSingleton>().CastRay(raycast, out closestHit)) {
            part = default;
            return false;
        }

        if (SystemAPI.HasComponent<PartDefinition>(closestHit.Entity)) {
            part = closestHit.Entity;
            return true;
        }
        if (SystemAPI.HasComponent<RelatedToPart>(closestHit.Entity)) {
            part = SystemAPI.GetComponent<RelatedToPart>(closestHit.Entity).Root;
            return true;
        }
        Debug.LogWarning($"raycast hit entity {closestHit.Entity} that wasn't a part");
        part = default;
        return false;
    }

    void PlacePart(ref EntityCommandBuffer actionQueue, Entity part, LocalTransform placeTransform, Entity parentPart) {
        var newPart = EntityManager.Instantiate(part);

        // todo - place one part at a time + modifier to place more at a time

        if (parentPart != Entity.Null) {
            actionQueue.AppendToBuffer(parentPart, new PartChildBuffer { Value = newPart });
        }
        actionQueue.AddBuffer<PartChildBuffer>(newPart);

        actionQueue.SetComponent(newPart, placeTransform);
        if (EntityManager.HasComponent<LinkedEntityGroup>(newPart)) {
            // prefabs don't have transform parent/child relationships set up, which we need for deleting things
            foreach (var child in EntityManager.GetBuffer<LinkedEntityGroup>(newPart, true)) {
                if (child.Value == newPart) {
                    continue;
                }
                actionQueue.AddComponent(child.Value, new RelatedToPart { Root = newPart });
            }
        }
    }

    RaycastInput CameraRaycast(out float3 direction) {
        UnityEngine.Ray ray = UnityEngine.Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
        direction = ray.direction;
        return new RaycastInput {
            Start = ray.origin,
            End = ray.origin + (ray.direction * 100),
            Filter = new CollisionFilter { BelongsTo = (1 << 6), CollidesWith = int.MaxValue }, // vehicle part layer
        };
    }

    protected override void OnDestroy() {
        input.Disable();
    }
}
