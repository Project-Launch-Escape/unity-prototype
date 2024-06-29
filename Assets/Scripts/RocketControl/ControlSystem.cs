using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public partial struct ControlSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<VehicleControl>();
        /*foreach (RefRW<TankComponent> tankComponent in SystemAPI.Query<RefRW<TankComponent>>()){Debug.Log(tankComponent.ValueRO.size);}*/
    }
    public void OnUpdate(ref SystemState state)
    {
        float consumed = 0;
        float3 gimbal = new float3(0, 0, 0);
        // Change throttle
        foreach (RefRW<VehicleControl> vehicleControl in SystemAPI.Query<RefRW<VehicleControl>>()) 
        {
            if (Input.GetKey(KeyCode.LeftControl)) { vehicleControl.ValueRW.throttle -= Time.deltaTime; }
            if (Input.GetKey(KeyCode.LeftShift)) { vehicleControl.ValueRW.throttle += Time.deltaTime; }
            if (Input.GetKey(KeyCode.X)) { vehicleControl.ValueRW.throttle = 0; }
            if (Input.GetKey(KeyCode.Z)) { vehicleControl.ValueRW.throttle = 1; }


            if (vehicleControl.ValueRO.throttle < 0) { vehicleControl.ValueRW.throttle = 0; }
            if (vehicleControl.ValueRO.throttle > 1) { vehicleControl.ValueRW.throttle = 1; }
            // This structure need to be replaced later on 
            foreach (RefRW<TankComponent> tankComponent in SystemAPI.Query<RefRW<TankComponent>>())
            {
                float requested_consume = vehicleControl.ValueRO.throttle * Time.deltaTime *5;
                float new_val = tankComponent.ValueRW.fuel - requested_consume;
                if (new_val <= 0) { consumed = tankComponent.ValueRO.fuel; tankComponent.ValueRW.fuel = 0;}
                else { tankComponent.ValueRW.fuel = new_val; consumed = requested_consume; }
                
            }
        }
        // Speed Up Rocket
        foreach ((RefRO<VehicleControl> vehicleControl,RefRW<PhysicsVelocity> physicsVelocity,RefRW<LocalTransform> localTransform) in SystemAPI.Query<RefRO<VehicleControl>, RefRW<PhysicsVelocity>,RefRW<LocalTransform>>())
        {
            
            /*
            if (Input.GetKey(KeyCode.A)) { gimbal.x = -0.7f; }
            if (Input.GetKey(KeyCode.D)) { gimbal.x = 0.7f; }
            if (Input.GetKey(KeyCode.W)) { gimbal.z = -0.7f; }
            if (Input.GetKey(KeyCode.S)) { gimbal.z = 0.7f; }*/

            // Base thrust direction (upwards)
            float3 thrustDirection = localTransform.ValueRO.Up();

            // Gimbal
            quaternion gimbalRotation = quaternion.Euler(gimbal);
            float3 adjustedThrustDirection = math.mul(gimbalRotation, thrustDirection);

            // force to be applied
            float3 thrustForce = adjustedThrustDirection * vehicleControl.ValueRO.throttle * 2000 * Time.deltaTime * consumed;

            // add velocity
            physicsVelocity.ValueRW.Linear += thrustForce;


            if (Input.GetKey(KeyCode.A)) { physicsVelocity.ValueRW.Angular.x += 0.5f * Time.deltaTime; }
            if (Input.GetKey(KeyCode.D)) { physicsVelocity.ValueRW.Angular.x -= 0.5f * Time.deltaTime; }
            if (Input.GetKey(KeyCode.W)) { physicsVelocity.ValueRW.Angular.z += 0.5f * Time.deltaTime; }
            if (Input.GetKey(KeyCode.S)) { physicsVelocity.ValueRW.Angular.z -= 0.5f * Time.deltaTime; }
        }
        // Tank 
        
        
    }
}