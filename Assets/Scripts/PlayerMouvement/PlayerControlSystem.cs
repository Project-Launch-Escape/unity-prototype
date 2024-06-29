using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Threading;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.LightTransport;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using static PlayerBpdyTAg;
using static UnityEditor.FilePathAttribute;
using static UnityEngine.Rendering.DebugUI;




public partial struct PlayerControlSystem : ISystem
{

    private void AlignToCelestialBody(ref SystemState state)
    {   // THE HEAD CAN NOT ROTATE YET -> GOAL IS TO MAKE MINECRAFT LIKE CONTROL (BODY FOLLOWING HEAD WHEN MOVING)
        foreach ((RefRW<LocalTransform> localTransform, RefRW<LocalToWorld> localToWorld, RefRW<PlayerBodyTag> playerBodyTag, RefRW<PhysicsVelocity> physicsVelocity)
        in SystemAPI.Query<RefRW<LocalTransform>, RefRW<LocalToWorld>, RefRW<PlayerBodyTag>, RefRW<PhysicsVelocity>>())
        {

            // FOR DEBUG PURPERSE
            Debug.DrawRay(localToWorld.ValueRO.Position, math.normalize(localToWorld.ValueRO.Forward) * 2, Color.blue);
            Debug.DrawRay(localToWorld.ValueRO.Position, math.normalize(localToWorld.ValueRO.Right) * 2, Color.red);
            Debug.DrawRay(localToWorld.ValueRO.Position, math.normalize(localToWorld.ValueRO.Up) * 2, Color.green);

            Debug.DrawRay(localTransform.ValueRO.Position, math.normalize(localTransform.ValueRO.Forward()), Color.magenta);
            Debug.DrawRay(localTransform.ValueRO.Position, math.normalize(localTransform.ValueRO.Right()), Color.yellow);
            Debug.DrawRay(localTransform.ValueRO.Position, math.normalize(localTransform.ValueRO.Up()), Color.cyan);

            // GET MOUSE INPUT
            float sensibility = 5;
            float rotate_deg = Input.GetAxis("Mouse X") * sensibility;
            playerBodyTag.ValueRW.x += rotate_deg;
            //Debug.Log(playerBodyTag.ValueRW.x);

            // ROTATE
            localTransform.ValueRW.Rotation = math.mul(localTransform.ValueRW.Rotation, quaternion.RotateY(rotate_deg * 2 * Mathf.PI / 360));

            // GET AXIS PLAYER SHOULD BE ALIGNED WITH
            float3 down = math.normalize(new float3(0, 0, 0) - localTransform.ValueRO.Position);
            float3 up = -down;
            float3 back = Vector3.Cross(localTransform.ValueRO.Right(), down);
            float3 forward = -back;
            float3 right = Vector3.Cross(forward, down);

            // SET FOOT TOWARDS GND
            localTransform.ValueRW.Rotation = Quaternion.LookRotation(forward, up);

            // MOUVE (WHEN MOVING DIAGONALY SPEED NEED TO BE LOWER)
            float speed = 10;
            if (Input.GetKey(KeyCode.LeftShift)) { speed = 15; }
            if (Input.GetKey(KeyCode.W)) { localTransform.ValueRW.Position += forward * Time.deltaTime * speed; }
            if (Input.GetKey(KeyCode.S)) { localTransform.ValueRW.Position -= forward * Time.deltaTime * speed; }
            if (Input.GetKey(KeyCode.D)) { localTransform.ValueRW.Position += right   * Time.deltaTime * speed; }
            if (Input.GetKey(KeyCode.A)) { localTransform.ValueRW.Position -= right   * Time.deltaTime * speed; }
            
            // Assuming on the ground 
            /*
            physicsVelocity.ValueRW.Linear -= playerBodyTag.ValueRO.mouvement;
            playerBodyTag.ValueRW.mouvement = 0;
            if (Input.GetKey(KeyCode.W)) { playerBodyTag.ValueRW.mouvement = forward * Time.deltaTime * speed; }
            else if (Input.GetKey(KeyCode.S)) { playerBodyTag.ValueRW.mouvement = -forward * Time.deltaTime * speed; }
            else if (Input.GetKey(KeyCode.D)) { playerBodyTag.ValueRW.mouvement = right * Time.deltaTime * speed; }
            else if (Input.GetKey(KeyCode.A)) { playerBodyTag.ValueRW.mouvement = -right * Time.deltaTime * speed; }
            physicsVelocity.ValueRW.Linear += playerBodyTag.ValueRO.mouvement;*/
            //physicsVelocity.ValueRW.Angular = 0;
            // KILL VELOCITY (FOR DEV ONLY)
            //physicsVelocity.ValueRW.Angular = 0;
            if ((Input.GetKey(KeyCode.V))) { physicsVelocity.ValueRW.Linear = 0; } // Celestial Body Velocity,

            
        }
    }
    public void OnCreate(ref SystemState state){state.RequireForUpdate<PlayerComponent>();}
  
    public void OnUpdate(ref SystemState state) {
        
        AlignToCelestialBody(ref state);

        //Interact
        UnityEngine.Ray screenRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        PhysicsWorldSingleton physicsworld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld cw = physicsworld.CollisionWorld;
        RaycastInput input = new RaycastInput { Start = screenRay.GetPoint(0), End = screenRay.GetPoint(100), Filter = CollisionFilter.Default };
        if (!Input.GetMouseButton(0)) { return; }
        Unity.Physics.RaycastHit hit;
        if (physicsworld.CastRay(input, out hit))
        if (cw.CastRay(input, out hit)) 
        
        Debug.DrawRay(screenRay.origin, screenRay.direction * 10f, Color.white);

        NativeList<Unity.Physics.RaycastHit> hits = new NativeList<Unity.Physics.RaycastHit>(Allocator.Temp);
        if (cw.CastRay(input, ref hits))
        {
            
            foreach (var ahit in hits)
            {
                // Retrieve the entity associated with the hit collider
                Entity hitEntity = physicsworld.Bodies[ahit.RigidBodyIndex].Entity;

                // Assuming we are interested in child entities, you can add additional logic
                // here to determine if the hit entity is a child entity and handle it accordingly.

                Debug.Log("Hit entity: " + hitEntity.Index.ToString());

                // Add any additional logic for processing the hit entity here

                // Break after the first hit if only the nearest hit is required
                //break;
            }
        }
        hits.Dispose();

        /*
        EntityManager entityManager = new EntityManager();
        var child = entityManager.GetComponentData<>(hit.Entity);*/

        return;/*
        //Head Rotation
        float xrot = 0;
        foreach ((RefRW<LocalTransform> localtransform, RefRW<PlayerComponent> playercomponenet, RefRO<LocalToWorld> world) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<PlayerComponent>, RefRO<LocalToWorld>>())
        {
            // Get Mouse Input
            playercomponenet.ValueRW.mouseX += Input.GetAxis("Mouse X") / 20;
            playercomponenet.ValueRW.mouseY -= Input.GetAxis("Mouse Y") / 20;
            // Horizontal Head Mouvement
            float xmax = 1f;
            if (playercomponenet.ValueRO.mouseX < -xmax) {
                xrot = playercomponenet.ValueRO.mouseX + xmax;
                playercomponenet.ValueRW.mouseX = -xmax; }
            if (playercomponenet.ValueRO.mouseX > +xmax) {
                xrot = playercomponenet.ValueRO.mouseX - xmax;
                playercomponenet.ValueRW.mouseX = +xmax; }
            // Vertical Head Mouvement
            float ymax = 1f;
            if (playercomponenet.ValueRO.mouseY < -ymax) { playercomponenet.ValueRW.mouseY = -ymax; }
            if (playercomponenet.ValueRO.mouseY > +ymax) { playercomponenet.ValueRW.mouseY = +ymax; }
            // If Mouving need to mach body rotation to head rotation (Like MINECRAFT)
            if (Input.GetKey(KeyCode.W) | Input.GetKey(KeyCode.S) | Input.GetKey(KeyCode.A) | Input.GetKey(KeyCode.D))
            { xrot = playercomponenet.ValueRO.mouseX; playercomponenet.ValueRW.mouseX = 0; }
            // Rotate Head
            localtransform.ValueRW.Rotation = quaternion.Euler(playercomponenet.ValueRW.mouseY, playercomponenet.ValueRW.mouseX, 0f);

            Debug.DrawRay(world.ValueRO.Position, math.normalize(world.ValueRO.Forward), Color.blue);
            Debug.DrawRay(world.ValueRO.Position, math.normalize(world.ValueRO.Right), Color.red);
            Debug.DrawRay(world.ValueRO.Position, math.normalize(world.ValueRO.Up), Color.green);
            /*
            float3 down    = math.normalize(new float3(0,0,0)-world.ValueRO.Position);
            float3 up      = math.normalize(world.ValueRO.Position-new float3(0,0,0));
            float3 forward = math.normalize(-Vector3.Cross(world.ValueRO.Right, down));
            float3 right   = math.normalize(-Vector3.Cross(forward, up));*/
            /*float3 up = world.ValueRO.Up;
            float3 right = world.ValueRO.Right;
            float3 forward = world.ValueRO.Forward;*/

            /*Camera cam = GetComponent<Camera>();Ray ray = cam.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));*/

            /*UnityEngine.Ray screenRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            PhysicsWorldSingleton physicsworld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            RaycastInput input = new RaycastInput { Start = screenRay.GetPoint(0), End = screenRay.GetPoint(100), Filter = CollisionFilter.Default };
            if (physicsworld.CastRay(input, out var hits)) { Debug.Log(hits.Entity); }

            Debug.DrawRay(screenRay.origin, screenRay.direction, Color.white);*/
            /*
            Debug.DrawRay(world.ValueRO.Position, forward, Color.blue);
            Debug.DrawRay(world.ValueRO.Position, up, Color.green);
            Debug.DrawRay(world.ValueRO.Position, right, Color.red);*/

        //}
        /*
        // Body Rotation And Mouvmeent
        foreach ((RefRW<LocalTransform> localTransform , RefRW<PlayerBodyTag> playerbody, RefRW<LocalToWorld> world) in SystemAPI.Query<RefRW<LocalTransform>,RefRW<PlayerBodyTag>,RefRW<LocalToWorld>>())
        {
            // Rotation for body to mach head
            playerbody.ValueRW.x += xrot;
            // Rotate Body
            localTransform.ValueRW.Rotation = quaternion.Euler(0f, playerbody.ValueRO.x, 0f);
            // Get Heading
            var down = math.normalize(new float3(0, 0, 0) - localTransform.ValueRO.Position);
            float3 forward = Vector3.Cross(localTransform.ValueRO.Right(), down);
            float3 right   = Vector3.Cross(forward, down);
            right = world.ValueRW.Right;
            forward = world.ValueRW.Forward;

            Debug.DrawRay(world.ValueRO.Position, math.normalize(world.ValueRO.Forward), Color.blue);
            Debug.DrawRay(world.ValueRO.Position, math.normalize(world.ValueRO.Right), Color.red);
            Debug.DrawRay(world.ValueRO.Position, math.normalize(world.ValueRO.Up), Color.green);


            // Moving
            float m = 5;
            if (Input.GetKey(KeyCode.LeftShift)){m=15;}
            if (Input.GetKey(KeyCode.W)) { localTransform.ValueRW.Position += forward * -m * Time.deltaTime; }
            if (Input.GetKey(KeyCode.S)) { localTransform.ValueRW.Position += forward * +m * Time.deltaTime; }
            if (Input.GetKey(KeyCode.D)) { localTransform.ValueRW.Position += right * -m * Time.deltaTime; }
            if (Input.GetKey(KeyCode.A)) { localTransform.ValueRW.Position += right * +m * Time.deltaTime; }
            Debug.DrawRay(localTransform.ValueRO.Position, -5*forward, Color.red);
            // Set Rotation
            localTransform.ValueRW.Rotation = Quaternion.LookRotation(-forward, -down);

            /*float3 d = -localTransform.ValueRO.Position;
            d = math.normalize(d);
            Quaternion q = Quaternion.FromToRotation(localTransform.ValueRO.Up(), -d);
            q = q * localTransform.ValueRO.Rotation;
            localTransform.ValueRW.Rotation = Quaternion.Slerp(localTransform.ValueRW.Rotation, q, 1);*/
        //localTransform.ValueRW.Rotation = quaternion.LookRotation(-d,Vector3.down);
    
        //}
    
    }
}
