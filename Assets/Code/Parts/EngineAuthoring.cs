using Unity.Entities;
using UnityEngine;

public class EngineAuthoring : MonoBehaviour {
    public Engine data;

    public class Baker : Baker<EngineAuthoring> {
        public override void Bake(EngineAuthoring authoring)
            => AddComponent(
                    GetEntity(TransformUsageFlags.Dynamic),
                    authoring.data
                );
    }

}
