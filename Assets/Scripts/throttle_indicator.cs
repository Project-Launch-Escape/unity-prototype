using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using System.Collections;
public class throttle_indicator : MonoBehaviour
{
    private RectTransform rectTransform;
    private float2 size_ref;
    public float throttle;
    private EntityManager manager;
    private Entity where_to_get_throttle;

    private IEnumerator Start()
    {   
        
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        yield return new WaitForSeconds(0.5f);
        rectTransform = GetComponent<RectTransform>();
        size_ref = rectTransform.sizeDelta;
        where_to_get_throttle = manager.CreateEntityQuery(typeof(VehicleControl)).GetSingletonEntity();
        throttle = manager.GetComponentData<VehicleControl>(where_to_get_throttle).throttle;
        
    }
    // Update is called once per frame
    void Update()
    {
        // Maybe not very efficient
        if (where_to_get_throttle.Index != 0) { throttle = manager.GetComponentData<VehicleControl>(where_to_get_throttle).throttle;
            
            rectTransform.sizeDelta = new float2(size_ref.x, size_ref.y * throttle); }
        
    }
}
