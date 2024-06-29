using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
public class GravityTagAutoring : MonoBehaviour
{
    public GameObject gravitingBody;
    public float mass;

    public bool ApplyForce;
    public float3 lastparentvelocity;
    public bool CopyCat;
    public float3 force;

    private class Baker : Baker<GravityTagAutoring>
    {
        
        public override void Bake(GravityTagAutoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new GravityTAGa() {t=authoring.ApplyForce, mass=authoring.mass,force = authoring.force,gravitingBody= GetEntity(authoring.gravitingBody, TransformUsageFlags.Dynamic),copycat=authoring.CopyCat, lastparentvelocity = authoring.lastparentvelocity}) ;
        }
    }
}
// ECS component
// Tag Component , Empty Component
public struct GravityTAGa : IComponentData{public bool t;public float mass;public float3 force;public Entity gravitingBody;public bool copycat;public float3 lastparentvelocity; }
