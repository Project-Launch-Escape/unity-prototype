using Unity.Entities;
using UnityEngine;

public class FuelTankAuthoring : MonoBehaviour {
    public FuelTank data;

    public class Baker : Baker<FuelTankAuthoring> {
        public override void Bake(FuelTankAuthoring authoring)
            => AddComponent(GetEntity(TransformUsageFlags.Dynamic), authoring.data);
    }
}