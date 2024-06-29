using Unity.Entities;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class OnRailBodyAuthoring : MonoBehaviour
{
    public float eccentricity =0 ; // Sharpness of orbit
    public float inclination = 0; // RADIANS
    public float semiMajorAxis = 1; // Distance Unit ? 
    public float ascendingNodeLongitude = 0; // RADIANS
    public float periapsisLongitude = 0; // PROBABLY RADIANS
    public float years = 1; // Duration of orbit
    public float multiplier = 60*60*24*365.25f; // 1sec irl = 1year in simu
    public float epochMeanLongitude = 0; // Radians
    private class Baker : Baker<OnRailBodyAuthoring>
    {

        public override void Bake(OnRailBodyAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new OnRailBodyComponent() {
                multiplier = authoring.multiplier,
                eccentricity = authoring.eccentricity,
                inclination = authoring.inclination,
                ascendingNodeLongitude = authoring.ascendingNodeLongitude,
                periapsisLongitude = authoring.periapsisLongitude,
                semiMajorAxis = authoring.semiMajorAxis,
                years = authoring.years,
                epochMeanLongitude = authoring.epochMeanLongitude
            }) ;
        }
    }
}

public struct OnRailBodyComponent : IComponentData {
    public float eccentricity;
    public float inclination;
    public float semiMajorAxis;
    public float ascendingNodeLongitude;
    public float periapsisLongitude;
    public float years;
    public float multiplier;
    public float time;
    public float epochMeanLongitude;
}