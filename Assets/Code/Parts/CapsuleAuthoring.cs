using Unity.Entities;
using UnityEngine;

public class CapsuleAuthoring : MonoBehaviour {

    public class Baker : Baker<CapsuleAuthoring> {
        public override void Bake(CapsuleAuthoring authoring)
            => AddComponent(GetEntity(TransformUsageFlags.Dynamic), typeof(Capsule));
    }
}