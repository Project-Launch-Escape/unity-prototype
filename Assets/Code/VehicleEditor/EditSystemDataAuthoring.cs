using System.Linq;
using Unity.Entities;
using UnityEngine;

public class EditSystemDataAuthoring : MonoBehaviour {

    public GameObject[] parts;

    public class Baker : Baker<EditSystemDataAuthoring> {
        public override void Bake(EditSystemDataAuthoring authoring) {
            Entity entity = GetEntity(authoring.gameObject, TransformUsageFlags.None);
            var partsBuffer = AddBuffer<PartsBuffer>(entity);
            authoring.parts.Select(x => GetEntity(x, TransformUsageFlags.Dynamic)).ToList().ForEach(x => partsBuffer.Add(new PartsBuffer { Value = x }));
            AddComponent(entity,
                new EditSystemData { SelectedPart = 0, AvailablePartsCount = authoring.parts.Length, });
        }
    }
}