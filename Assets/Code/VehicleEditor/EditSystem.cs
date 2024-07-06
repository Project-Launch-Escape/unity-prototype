using Mono.Cecil;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Physics.CompoundCollider;

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

        changeVariables();

        RaycastInput raycast = GetCameraRaycast(out float3 clickDirection);
        bool hitPart = GetPartFrom(raycast, out Unity.Physics.RaycastHit hitInfo, out Entity parentPart);

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
                    Rotation = quaternion.LookRotationSafe(hitInfo.SurfaceNormal, new float3(0, 1, 0)),
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
                    Rotation = quaternion.LookRotationSafe(snapTransform.Up, new float3(0, 1, 0)),
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

            if (hitPart && input.ShowPathToRoot.WasPressedThisFrame())
            {
                ShowPathToRoot(ref actionQueue, parentPart: parentPart);
            }
            if (input.Place.WasPressedThisFrame()) {
            var partPrefab = SystemAPI.GetSingletonBuffer<PartsBuffer>()[state.SelectedPart].Value;

                SystemAPI.GetSingletonRW<EditSystemData>().ValueRW.ID += 1; // the state in update is not RW
            if (hitPart) // If there is no hitpart then symmetry is not needed
            {
              DoSymmetry(actionQueue: actionQueue, part: partPrefab, parentPart: parentPart, symmetryCount: state.sym, placeTransform: placeTransform,MaxIt:state.it,ID:state.ID);
            }
            else
            {
                PlacePart(ref actionQueue, partPrefab, placeTransform,ID:state.ID);
            }
                
            }
        }

        if (input.Delete.WasPressedThisFrame() && hitPart) {
            // todo - handling for single part deletion
            DeletePartTree(ref actionQueue, parentPart);
        }

        actionQueue.Playback(EntityManager);
    }

    /// <returns>true if part found</returns>
    bool GetPartFrom(RaycastInput raycast, out Unity.Physics.RaycastHit closestHit, out Entity part) {
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

    void PlacePart(ref EntityCommandBuffer actionQueue, Entity part, LocalTransform placeTransform,int ID) {
        var newPart = EntityManager.Instantiate(part);

        // todo - place one part at a time + modifier to place more at a time

        actionQueue.AddBuffer<PartChildBuffer>(newPart);
        actionQueue.SetComponent(newPart, placeTransform);
        actionQueue.AddBuffer<PartSiblingsBuffer>(newPart);
        actionQueue.AppendToBuffer(newPart, new PartSiblingsBuffer { Value = newPart});
        actionQueue.AddComponent(newPart, new PartComponent { id = ID, layer = 1 });
        actionQueue.AddBuffer<PartChildBuffer>(newPart); // when creating a part give him an empty buffer (list) in which childrend entities will be add later
        actionQueue.SetComponent(newPart, placeTransform);
    }
    
    Entity PlaceParts(ref EntityCommandBuffer actionQueue, Entity partToPlace, Entity parentEntity, LocalTransform placementTransform)
    {
        var createdEntity = EntityManager.Instantiate(partToPlace);
        actionQueue.AppendToBuffer(parentEntity, new PartChildBuffer { Value = createdEntity });
        actionQueue.AddComponent(createdEntity, new PartParentComponent { Value = parentEntity });
        actionQueue.AddBuffer<PartChildBuffer>(createdEntity); // when creating a part give him an empty buffer (list) in which childrend entities will be add later // Adding empty buffers to every part might not be the best idea , since some of them might never be used
        actionQueue.SetComponent(createdEntity, placementTransform);
        return createdEntity;
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

    
    void ShowPathToRoot(ref EntityCommandBuffer actionQueue , Entity parentPart)
    {
        // this is just some debug stuff to see how the parts where created
        
            Debug.Log("");
            int iteration = SystemAPI.GetComponent<PartComponent>(parentPart).layer; // ok idk if iteration is the right term but it will do just fine for now

            int[] symmetryCounts = new int[iteration];
            int[] IDs = new int[iteration];
            Entity[] entitiesRootToBottom = new Entity[iteration]; // technicly later on this will be a root to bottom

            var currentPart = parentPart;
            for (int i = 0; i < iteration; i++)
            {
                var siblings = EntityManager.GetBuffer<PartSiblingsBuffer>(currentPart);
                symmetryCounts[iteration - i - 1] = siblings.Length;
                IDs[iteration - i - 1] = SystemAPI.GetComponent<PartComponent>(currentPart).id;
                entitiesRootToBottom[iteration - i - 1] = currentPart;

                if (i != iteration - 1) { currentPart = SystemAPI.GetComponent<PartParentComponent>(currentPart).Value; } // root has no parent
            }
            foreach (var t in entitiesRootToBottom)
            {
                Debug.Log(t.Index);
                LocalTransform lt = SystemAPI.GetComponent<LocalTransform>(t);
                if (lt.Scale == 1.0f) { lt.Scale = 1.2f; }
                else { lt.Scale = 1.0f; }
                actionQueue.SetComponent(t, lt);
            }
    }

    // dont know if not using ref will be an issue
    // THERE MIGHT BE AN ISSUE , IF A PART IS REGISTERED AS ANOTHER PART SIBLING BUT THEN THIS FIRST PART IS DESTROYED
    // IF A NEW PART IS PLACED WITH THE SAME VALUE AS THE DESTROYED ONE THERE MIGHT BE SOME ISSUE -> to fix this the deleted part's reference should be cleaned from his siblings and parents
    void DoSymmetry(EntityCommandBuffer actionQueue,Entity part,Entity parentPart,int symmetryCount,LocalTransform placeTransform,int MaxIt,int ID) {
        int iteration = SystemAPI.GetComponent<PartComponent>(parentPart).layer; // ok idk if iteration is the right term but it will do just fine for now

        if (MaxIt < iteration) { iteration = MaxIt; }
        
        int[] symmetryCounts = new int[iteration];
        int[] IDs = new int[iteration];
        int[] selfplaces = new int[iteration];
        Entity[] entitiesRootToBottom = new Entity[iteration]; // technicly later on this will be a root to bottom

        var currentPart = parentPart;
        for (int i = 0; i < iteration; i++)
        {
            var siblings = EntityManager.GetBuffer<PartSiblingsBuffer>(currentPart);
            symmetryCounts[iteration-i-1] = siblings.Length;
            IDs[iteration - i - 1] = SystemAPI.GetComponent<PartComponent>(currentPart).id;
            entitiesRootToBottom[iteration - i - 1] = currentPart;
            selfplaces[iteration - i - 1] = SystemAPI.GetComponent<PartComponent>(currentPart).selfplace;
            if (i != iteration - 1) {currentPart = SystemAPI.GetComponent<PartParentComponent>(currentPart).Value; } // root has no parent
        }
        symmetryCounts[0] = 1;

        bool var = true;
        float3 firstPartPos = placeTransform.Position; // This variable is going to be constantly changed and rotated in the recursif for
        void RecursifFor(int depth, Entity lastParent) {

            Entity currentParent = Entity.Null;
            if (var){ currentParent = entitiesRootToBottom[depth]; }
            else { 
                if (depth == 0) { currentParent = lastParent;} // for depth = 0 the parent root parent is given and there isnt the need to find it
                else // Else need to determine current Part from lastPart, and its siblings
                {
                    // Find New Children to loop on (maybe multiple , or none)
                    foreach (var child in EntityManager.GetBuffer<PartChildBuffer>(lastParent))
                    {
                        if (!EntityManager.Exists(child.Value)) { continue; } // there might be a better way to place this
                        if (SystemAPI.GetComponent<PartComponent>(child.Value).id == IDs[depth]) {
                            currentParent = child.Value;break;//only need to find one since we will be looping on it's siblings later
                        }
                    }
                }
            }
            if (currentParent==Entity.Null) { return; } // if no conform child part has been found then do nothing (this happens only if parts have been deleted)
            

            DynamicBuffer<PartSiblingsBuffer> siblings_of_current_parent = EntityManager.GetBuffer<PartSiblingsBuffer>(currentParent);

            Quaternion rotate = Quaternion.AngleAxis(360 / symmetryCounts[depth], Vector3.up); // Rotation
            float3 center = SystemAPI.GetComponent<LocalTransform>(lastParent).Position;

            for (int i = 0; i < symmetryCounts[depth]; i++)
            {
                var j = i;
                // this is also important , it will chose the right part to rotate around
                j += selfplaces[depth];
                if (j >= symmetryCounts[depth]) { j -= symmetryCounts[depth]; }

                var parentPart = siblings_of_current_parent[j].Value;
                if (EntityManager.Exists(parentPart))
                {

                    if ((depth + 1) == iteration)
                    { 
                        var = false;
                        RotatingPartPlacement(ref actionQueue, part: part, parentPart: parentPart, firstPartPos: firstPartPos, symmetryCount: symmetryCount, resetlayer: false, placeTransform: placeTransform,ID:ID);
                    }
                    else
                    {
                        RecursifFor(depth: depth + 1, parentPart);
                    }
                }
                // THE IMPORTANT PART : ROTATING // Ok I could use the placeTransform as first part pos but when I first created this program I wanted to make sure that position and rotation dont interfere 
                firstPartPos = (rotate * (firstPartPos - center)) + (Vector3)center;
                placeTransform.Rotation = (Quaternion)placeTransform.Rotation * rotate;
            }
        }
        RecursifFor(depth:0, lastParent:entitiesRootToBottom[0]);

    }

    void RotatingPartPlacement(ref EntityCommandBuffer actionQueue, Entity part, Entity parentPart, float3 firstPartPos, int symmetryCount, bool resetlayer, LocalTransform placeTransform,int ID) {
        float3 center = SystemAPI.GetComponent<LocalTransform>(parentPart).Position;

        Entity[] siblings = new Entity[symmetryCount];
        for (int i = 0; i < symmetryCount; i++)
        {
            Quaternion rotation = Quaternion.AngleAxis(i * 360 / symmetryCount, Vector3.up);
            float3 p = (rotation * (firstPartPos - center)) + (Vector3)center;
            //p.y += 0.25f * i; // debuging purpsers shows which part is placed first second thrid ...
            LocalTransform newp = new LocalTransform
            { Position = p, Rotation = placeTransform.Rotation * rotation, Scale = 1 };
            siblings[i] = PlaceParts(ref actionQueue, partToPlace: part, parentEntity: parentPart, placementTransform: newp);
        }

        // Set PartSiblingsBuffer to every siblings // And PartComponent
        int layer;
        if (!resetlayer) { layer = SystemAPI.GetComponent<PartComponent>(parentPart).layer + 1; }
        else layer = 1;
        for (int i = 0; i < symmetryCount; i++)
        { 
            actionQueue.AddBuffer<PartSiblingsBuffer>(siblings[i]);
            // THIS "PartComponent" is stored a lot of times with the same value , if need to optimse change the structre (the same value is stored 3 times if placed with x3 sym)
            actionQueue.AddComponent(siblings[i], new PartComponent { id = ID , layer = layer , selfplace = i});

            for (int j = 0; j < symmetryCount; j++) { actionQueue.AppendToBuffer(siblings[i], new PartSiblingsBuffer { Value = siblings[j] }); }  
        }
    }

    void changeVariables()
    {
        var state = SystemAPI.GetSingletonRW<EditSystemData>();
        bool print = false;
        if (input.MinusIterations.WasPressedThisFrame())
        {
            print = true;
            state.ValueRW.it--;
        }
        else if (input.AddIterations.WasPressedThisFrame())
        {
            print = true;
            state.ValueRW.it++;
        }
        if (input.MinusSym.WasPressedThisFrame())
        {
            print = true;
            state.ValueRW.sym--;
        }
        else if (input.AddSym.WasPressedThisFrame())
        {
            print = true;
            state.ValueRW.sym++;
        }
        if (state.ValueRO.sym < 1) { state.ValueRW.sym = 1; }
        if (state.ValueRO.it < 1) { state.ValueRW.it = 1; }

        if (print) { Debug.Log($"Symmetry x{state.ValueRO.sym} , {state.ValueRO.it} iterations"); }

    }

}