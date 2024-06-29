using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;
using Unity.Transforms;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public partial struct GravitySystem : ISystem
{
    public void OnCreate(ref SystemState state){state.RequireForUpdate<GravityTAGa>();}

    public void OnUpdate(ref SystemState state)
    {
        // Need to add a way to force the orbit of an object without calculating SOI (planets will always orbit the same star)
        double G = 6.67 * 10E-11;
        G = 100000;
        EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;

        foreach ((RefRO<PhysicsMass> physicsMass, RefRO<LocalToWorld> localToWorld, RefRW<GravityTAGa>applyGravityComponent,RefRW<PhysicsVelocity>velocity) in SystemAPI.Query<RefRO<PhysicsMass>, RefRO<LocalToWorld>, RefRW<GravityTAGa>,RefRW<PhysicsVelocity>>())
        {

            if (applyGravityComponent.ValueRO.t) { velocity.ValueRW.Linear = applyGravityComponent.ValueRO.force; applyGravityComponent.ValueRW.t = false; }

            float3 position = localToWorld.ValueRO.Position;
            float mass = 1 / physicsMass.ValueRO.InverseMass;

            // I NEED TO MAKE THE ORBITING OBJECT KEEP THE SAME SPEED AS THE THING BEING ORBITED but that means that i need to remeber its local speed and modify
            // it idependently to the oribted object speed 
            // i could try first to just make an object copy the velocity of an object and then later make it have independent local speed
            
            
            
            if (applyGravityComponent.ValueRO.gravitingBody.Index != 0)
            {
                

                float3 position2 = manager.GetComponentData<LocalToWorld>(applyGravityComponent.ValueRO.gravitingBody).Position;

                float mass2 = 1 / manager.GetComponentData<PhysicsMass>(applyGravityComponent.ValueRO.gravitingBody).InverseMass;
                
                float3 dir = (position2 - position);
                
                float force = (float)G * mass * mass2 / (math.pow(math.length(dir), 2)); //if dont work maybe change everything to double
                dir = math.normalize(dir);
                
                if (applyGravityComponent.ValueRO.copycat)
                {
                    
                    //  Debug.Log(applyGravityComponent.ValueRO.relativeVelocity);
                    velocity.ValueRW.Linear += -applyGravityComponent.ValueRO.lastparentvelocity + manager.GetComponentData<PhysicsVelocity>(applyGravityComponent.ValueRO.gravitingBody).Linear + SystemAPI.Time.DeltaTime * dir * force / mass;
                    applyGravityComponent.ValueRW.lastparentvelocity = manager.GetComponentData<PhysicsVelocity>(applyGravityComponent.ValueRO.gravitingBody).Linear;
                }
                else
                {
                    velocity.ValueRW.Linear += SystemAPI.Time.DeltaTime * dir * force / mass;
                }
            }
            else
            {
                float3 fmax = float3.zero;

                // Need to add a gravitable tag to reduce complexity
                // Mass is multiplied and then divided (need to be corrected)
                // this systeme does not read distance to body to see if it is in SOI but compare the force of every bodies (it's useless and ineficient)
                foreach ((RefRO<PhysicsMass> physicsMass2, RefRO<LocalToWorld> localToWorld2) in SystemAPI.Query<RefRO<PhysicsMass>, RefRO<LocalToWorld>>())
                {
                    float3 position2 = localToWorld2.ValueRO.Position;
                    if (math.all(position == position2)) { continue; }
                    float mass2 = 1 / physicsMass2.ValueRO.InverseMass;

                    float3 dir = (position2 - position);
                    float force = (float)G * mass * mass2 / (math.pow(math.length(dir), 2)); //if dont work maybe change everything to double
                    dir = math.normalize(dir);
                    if (math.length(force * dir) > math.length(fmax)) { fmax = force * dir; }
                }

                velocity.ValueRW.Linear += SystemAPI.Time.DeltaTime * fmax / mass;
            }
        }


        /*
        foreach ((RefRW<PhysicsVelocity> velocity, RefRO<LocalTransform> localtransform,RefRW<GravityTAGa>g)  in SystemAPI.Query<RefRW<PhysicsVelocity>,RefRO<LocalTransform>,RefRW<GravityTAGa>>())
        {
            if (g.ValueRO.t) { velocity.ValueRW.Linear =g.ValueRO.force;g.ValueRW.t = false; }

            //EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            float3 center = Vector3.zero;
            if (g.ValueRO.gravitingBody.Index != 0) {center = manager.GetComponentData<LocalToWorld>(g.ValueRO.gravitingBody).Position; }

            float3 dir = (center - localtransform.ValueRO.Position);
            float dist = math.length(dir);
            dir = math.normalize(dir);
            float invdist = 1f / dist;
            float force = g.ValueRO.mass * invdist * invdist *100;
            velocity.ValueRW.Linear += SystemAPI.Time.DeltaTime * dir * force;
            
        }*/
    }
}

