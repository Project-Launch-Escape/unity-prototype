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
    private const float SNAP_DISTANCE = .5f;

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

        RaycastInput raycast = GetCameraRaycast(out float3 clickDirection);
        bool hitPart = GetPartFrom(raycast, out RaycastHit hitInfo, out Entity parentPart);

        if (input.Summon.WasPressedThisFrame()) {
            SetupPlacementGhost(ref actionQueue);
        }
        else if (input.Cancel.WasPressedThisFrame()) {
            DestroyPlacementGhost(ref actionQueue);
        }
        else if (SystemAPI.TryGetSingletonEntity<PlacementGhost>(out Entity placementGhost)) { // move ghost
            LocalTransform placeTransform;

            // derive where ghost should be
            if (hitPart) { // against part surface
                placeTransform = new LocalTransform {
                    Position = hitInfo.Position,
                    Rotation = quaternion.LookRotationSafe(hitInfo.SurfaceNormal, new(0, 1, 0)),
                    Scale = 1
                };
                var offsetDirection = placeTransform.InverseTransformDirection(hitInfo.SurfaceNormal);
                placeTransform.Position += placeTransform.TransformDirection(GetPlacementOffset(offsetDirection));
            }
            else { // free placement
                placeTransform = new LocalTransform {
                    Position = raycast.Start + clickDirection * UnityEngine.Camera.main.transform.localPosition.magnitude, // todo - should be coplanar with origin
                    Rotation = quaternion.identity,
                    Scale = 1
                };
            }

            // apply snapping
            if (!state.IsSnapping && FindValidSnapPointInRange(out var snapTransform, out Entity part)) { // start snapping
                var stateRW = SystemAPI.GetSingletonRW<EditSystemData>();
                stateRW.ValueRW.IsSnapping = true;
                stateRW.ValueRW.LastGhostPositionBeforeSnap = placeTransform.Position;
                stateRW.ValueRW.WasAgainstPartBeforeSnapping = hitPart;

                placeTransform = new LocalTransform {
                    Position = snapTransform.Position,
                    Rotation = quaternion.LookRotationSafe(snapTransform.Up, new(0, 1, 0)),
                    Scale = 1
                };
                var offsetDirection = placeTransform.InverseTransformDirection(snapTransform.Up);
                placeTransform.Position += placeTransform.TransformDirection(GetPlacementOffset(offsetDirection));

                stateRW.ValueRW.SnapPosition = placeTransform.Position;
                stateRW.ValueRW.SnapRotation = placeTransform.Rotation;
                actionQueue.AddComponent<SnappedToPartTag>(part);
            }
            else if (state.IsSnapping) {
                var snappedPart = SystemAPI.GetSingletonEntity<SnappedToPartTag>();
                bool cursorMovedAway = math.length(placeTransform.Position - state.LastGhostPositionBeforeSnap) < SNAP_DISTANCE;
                if (state.WasAgainstPartBeforeSnapping == hitPart && cursorMovedAway) { // continue snapping
                    placeTransform = new LocalTransform {
                        Position = state.SnapPosition,
                        Rotation = state.SnapRotation,
                        Scale = 1
                    };
                    parentPart = snappedPart;
                }
                else { // finish snapping
                    var stateRW = SystemAPI.GetSingletonRW<EditSystemData>();
                    stateRW.ValueRW.IsSnapping = false;
                    actionQueue.RemoveComponent<SnappedToPartTag>(snappedPart);
                }
            }

            actionQueue.SetComponent(placementGhost, placeTransform);

            if (input.Place.WasPressedThisFrame()) {
                PlacePart(ref actionQueue, SystemAPI.GetSingletonBuffer<PartsBuffer>()[state.SelectedPart].Value, placeTransform, hitPart ? parentPart : Entity.Null);
            }
        }

        if (input.Delete.WasPressedThisFrame() && hitPart) {
            // todo - handling for single part deletion
            DeletePartTree(ref actionQueue, parentPart);
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
            if (SystemAPI.HasComponent<SnapPoint>(child)) {
                actionQueue.AddComponent<SnapPointGhostTag>(child);
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

    /// <returns>true if snap point found + LocalToWorld of same</returns>
    bool FindValidSnapPointInRange(out LocalToWorld snapTransform, out Entity part) {
        var ghostSnapChunks = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<SnapPoint, SnapPointGhostTag>()
            .Build(this)
            .ToArchetypeChunkArray(Allocator.Temp);
        var otherSnapChunks = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<SnapPoint>()
            .WithAbsent<SnapPointGhostTag>()
            .Build(this)
            .ToArchetypeChunkArray(Allocator.Temp);

        var snapPointHandle = SystemAPI.GetComponentTypeHandle<SnapPoint>(true);
        var localToWorldHandle = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true);
        var parentHandle = SystemAPI.GetComponentTypeHandle<Parent>(true);

        // this looks really really bad but is actually O(ghost snaps * other snaps) which is just bad
        foreach (var ghostSnapChunk in ghostSnapChunks) {

            NativeArray<SnapPoint> ghostSnapPoints = ghostSnapChunk.GetNativeArray(ref snapPointHandle);
            NativeArray<LocalToWorld> ghostSnapTransforms = ghostSnapChunk.GetNativeArray(ref localToWorldHandle);

            for (int i = 0; i < ghostSnapChunk.Count; i++) {
                PartPlacementFlags ghostSnapPlaceFlags = ghostSnapPoints[i].belongsTo;
                LocalToWorld ghostSnapTransform = ghostSnapTransforms[i];

                foreach (var otherSnapChunk in otherSnapChunks) {
                    NativeArray<SnapPoint> otherSnapPoints = otherSnapChunk.GetNativeArray(ref snapPointHandle);
                    NativeArray<LocalToWorld> otherSnapTransforms = otherSnapChunk.GetNativeArray(ref localToWorldHandle);
                    NativeArray<Parent> otherSnapParents = otherSnapChunk.GetNativeArray(ref parentHandle);

                    for (int j = 0; j < otherSnapChunk.Count; j++) {
                        PartPlacementFlags otherSnapPlaceFlags = otherSnapPoints[j].connectsWith;

                        if ((ghostSnapPlaceFlags & otherSnapPlaceFlags) == 0) {
                            continue;
                        }

                        LocalToWorld otherSnapTransform = otherSnapTransforms[j];
                        float3 diff = ghostSnapTransform.Position - otherSnapTransform.Position;
                        if (math.length(diff) < SNAP_DISTANCE) {
                            snapTransform = otherSnapTransform;
                            part = otherSnapParents[j].Value; // assuming that the hierarchy is flat
                            return true;
                        }
                    }
                }
            }
        }
        snapTransform = default;
        part = default;
        return false;
    }

    protected override void OnDestroy() {
        input.Disable();
    }
}
