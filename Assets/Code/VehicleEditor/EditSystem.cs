using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Debug = UnityEngine.Debug;

/// <summary>
/// Primary system for vehicle editor - placing, deleting, etc
/// 
/// Controls:
/// - 1 -> Summon ghost (repeat press to cycle part)
/// - Esc -> Cancel ghost
/// - Left click -> Place part
/// - X -> Delete part tree
/// </summary>
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

        RaycastInput raycast = GetCameraRaycast(out float3 raycastDir);
        bool raycastHit = GetPartFrom(raycast, out RaycastHit closestHit, out Entity raycastPart);

        if (SystemAPI.TryGetSingletonEntity<PlacementGhost>(out Entity placementGhost)) {
            // move ghost
            LocalTransform placeTransform;
            if (raycastHit) {
                placeTransform = new LocalTransform {
                    Position = closestHit.Position,
                    Rotation = quaternion.LookRotationSafe(closestHit.SurfaceNormal, new(0, 1, 0)),
                    Scale = 1
                };
                var offsetDirection = placeTransform.InverseTransformDirection(closestHit.SurfaceNormal);
                placeTransform.Position += placeTransform.TransformDirection(GetPlacementOffset(offsetDirection));
            }
            else {
                placeTransform = new LocalTransform {
                    Position = raycast.Start + raycastDir * UnityEngine.Camera.main.transform.localPosition.magnitude, // todo - should be coplanar with origin
                    Rotation = quaternion.identity,
                    Scale = 1
                };
            }
            actionQueue.SetComponent(placementGhost, placeTransform);

            if (input.Place.WasPressedThisFrame()) {
                PlacePart(ref actionQueue, SystemAPI.GetSingletonBuffer<PartsBuffer>()[state.SelectedPart].Value, placeTransform, raycastHit ? raycastPart : Entity.Null);
            }
        }

        if (input.Delete.WasPressedThisFrame() && raycastHit) {
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
        if (SystemAPI.HasComponent<Parent>(closestHit.Entity)) {
            part = SystemAPI.GetComponent<Parent>(closestHit.Entity).Value;
            if (!SystemAPI.HasComponent<PartDefinition>(part)) {
                Debug.LogError("Part has nested children!");
                return false;
            }
            return true;
        }
        part = default;
        return false;
    }

    void PlacePart(ref EntityCommandBuffer actionQueue, Entity part, LocalTransform placeTransform, Entity parentPart) {
        var newPart = EntityManager.Instantiate(part);

        // todo - place one part at a time + modifier to place more at a time

        if (EntityManager.Exists(parentPart)) {
            actionQueue.AppendToBuffer(parentPart, new PartChildBuffer { Value = newPart });
        }
        actionQueue.AddBuffer<PartChildBuffer>(newPart);

        actionQueue.SetComponent(newPart, placeTransform);
    }

    void DeletePartTree(ref EntityCommandBuffer actionQueue, Entity rootPart) {
        var deletedParts = new NativeList<int>(Allocator.Temp);
        var deleteFrontier = new NativeQueue<Entity>(Allocator.Temp);
        deleteFrontier.Enqueue(rootPart);

        while (!deleteFrontier.IsEmpty()) {
            var next = deleteFrontier.Dequeue();
            if (!EntityManager.Exists(next)) {
                continue;
            }
            // if there are cycles in the part tree this will loop forever and lock up the game, so we need to check if a part appears more than once and skip it
            if (deletedParts.Contains(next.Index)) {
                Debug.LogWarning($"{next} was referenced in the part tree more than once");
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
        if (SystemAPI.TryGetSingletonEntity<PlacementGhost>(out Entity placementGhost)) {
            actionQueue.DestroyEntity(placementGhost);
            return true;
        }
        return false;
    }

    void SetupPlacementGhost(ref EntityCommandBuffer actionQueue) {
        var state = SystemAPI.GetSingletonRW<EditSystemData>();
        var parts = SystemAPI.GetSingletonBuffer<PartsBuffer>(true);

        if (DestroyPlacementGhost(ref actionQueue)) {
            // increment part - no ui at the moment
            state.ValueRW.SelectedPart = (state.ValueRO.SelectedPart + 1) % state.ValueRO.AvailablePartsCount;
        }

        var ghost = EntityManager.Instantiate(parts[state.ValueRO.SelectedPart].Value);

        // reset ghost position for bounds calcs
        var ghostTransform = SystemAPI.GetComponentRW<LocalTransform>(ghost);
        ghostTransform.ValueRW.Position = float3.zero;
        ghostTransform.ValueRW.Rotation = quaternion.identity;
        ghostTransform.ValueRW.Scale = 1;

        // setup bounds to calculate how far to offset the prefab so it doesn't clip into whatever it's placed against
        NativeList<Aabb> boundsList = new NativeList<Aabb>(Allocator.Temp);
        foreach (var childWrapper in EntityManager.GetBuffer<LinkedEntityGroup>(ghost, true)) {
            var child = childWrapper.Value;
            if (SystemAPI.HasComponent<PhysicsCollider>(child)) {
                Aabb childBounds = SystemAPI.GetComponent<PhysicsCollider>(child).Value.Value.CalculateAabb();
                var childTransform = SystemAPI.GetComponent<LocalTransform>(child);

                // child position not automatically accounted for when calculating bounds
                // todo - if there are nested children this will break...
                childBounds.Min += childTransform.Position;
                childBounds.Max += childTransform.Position;

                boundsList.Add(childBounds);
                actionQueue.RemoveComponent<PhysicsCollider>(child); // don't interfere with the camera raycast!!!
            }
        }
        Aabb bounds = new Aabb();
        foreach (var abbb in boundsList) {
            bounds.Include(abbb);
        }

        actionQueue.AddComponent(ghost, new PlacementGhost { bounds = bounds });
    }

    float3 GetPlacementOffset(float3 dir) {
        Aabb bounds = SystemAPI.GetSingleton<PlacementGhost>().bounds;
        float x = dir.x < 0 ? bounds.Max.x : -bounds.Min.x;
        float y = dir.y < 0 ? bounds.Max.y : -bounds.Min.y;
        float z = dir.z < 0 ? bounds.Max.z : -bounds.Min.z;
        float3 res = new(x * dir.x, y * dir.y, z * dir.z);
        return res;
    }

    RaycastInput GetCameraRaycast(out float3 direction) {
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
