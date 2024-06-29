using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities.UniversalDelegates;
using Unity.Physics;
using Unity.Transforms;
using Unity.VisualScripting;

public partial struct OnRailBodySystem : ISystem
{
    public void OnCreate(ref SystemState state) { state.RequireForUpdate<OnRailBodyComponent>(); }

    public void OnUpdate(ref SystemState state) {
        
        foreach ((RefRW< LocalTransform> localTransform, RefRW<OnRailBodyComponent> onRailBodyComponent) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<OnRailBodyComponent>>())
        {
            //https://space.stackexchange.com/questions/8911/determining-orbital-position-at-a-future-point-in-time
            //https://ssd.jpl.nasa.gov/planets/approx_pos.html

            // PARAMETERS (DO NOT CHANGE OVER TIME)(but you can if you want)
            float L0 = onRailBodyComponent.ValueRO.epochMeanLongitude; // Mean longitude at epoch T0 (Where the planet start)
            float p = onRailBodyComponent.ValueRO.periapsisLongitude;// Longitude of periapsis (Where the closest point to the star* is)
            float W = onRailBodyComponent.ValueRO.ascendingNodeLongitude;// Longitude of the ascending node (Where it goes up ? For inclination!=0)
            float e = onRailBodyComponent.ValueRO.eccentricity;// Eccentricity (Sharpness of orbit)
            float a = onRailBodyComponent.ValueRO.semiMajorAxis;// Semi-major axis (Average Radius (in some way))
            float i = onRailBodyComponent.ValueRO.inclination;// Inclination
            
            // https://en.wikipedia.org/wiki/Mean_motion
            float n = (2*Mathf.PI) / onRailBodyComponent.ValueRO.years;  // Meean motion = Revolution / Time

            //https://en.wikipedia.org/wiki/Mean_longitude
            onRailBodyComponent.ValueRW.time += Time.deltaTime * onRailBodyComponent.ValueRO.multiplier;
            float L = (L0 + n * (onRailBodyComponent.ValueRO.time/(3600*24*365.25f)))%360; //Mean longitude = Longitude at T0 + Mean motion * Time

            // Everything underhere basicly comme from  https://space.stackexchange.com/questions/8911/determining-orbital-position-at-a-future-point-in-time
            var M = L - p; //Mean anomaly
            var w = p - W; //Argument of periapsis

            // Solve M = E - ( e * sin(E) ) with M mean anomaly, https://www.met.reading.ac.uk/~ross/Documents/OrbitNotes.pdf
            var E = M;
            // E=M is a commun initial guess , and it is recommended to have good ones especialy for high eccentricity , 
            // (We could add a variable in the component to store previous E if need (Idk if there's realy the need))
            int t = 0;
            while (true)
            {
                var dE = (E - e * Mathf.Sin(E) - M) / (1 - e * Mathf.Cos(E)); // cos vax -1 a 1 
                E -= dE;
                t++;
                if (Mathf.Abs(dE) < 1e-5) {break;}
                if (t > 10) {break;} // prevent infinit loop (if we dont add this we need to add more tolerance (1e-5 <- 1e-4) (It was original 1e-6)
                // The more eccentric orbits are the the more iterations it will need or dE sometimes may never go below 1e-5
                //
            }
            // (P and Q form a 2d coordinate system in the plane of the orbit, with +P pointing towards periapsis.)
            var P = a * (Mathf.Cos(E) - e);
            var Q = a * Mathf.Sin(E) * Mathf.Sqrt(1 - Mathf.Pow(e, 2));
            // rotate by argument of periapsis
            var x = Mathf.Cos(w) * P - Mathf.Sin(w) * Q;
            var y = Mathf.Sin(w) * P + Mathf.Cos(w) * Q;
            // rotate by inclination
            var z = Mathf.Sin(i) * y;
            y = Mathf.Cos(i) * y;
            // rotate by longitude of ascending node
            var xtemp = x;
            x = Mathf.Cos(W) * xtemp - Mathf.Sin(W) * y;
            y = Mathf.Sin(W) * xtemp + Mathf.Cos(W) * y;
            localTransform.ValueRW.Position=new float3(x,z , y ); // This of course need to be changed later (1000 is here to make bigger orbits at the scale i'm working im my demotest thing)
        }
        

    }
}
