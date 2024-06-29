using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using System.Collections;

public class fuel_indicator : MonoBehaviour
{
    private RectTransform rectTransform;
    private float2 size_ref;
    public float fuel;
    private EntityManager manager;
    private Entity where_to_get_fuel;
    private float max_fuel_ref;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private IEnumerator Start()
    {

        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        yield return new WaitForSeconds(0.5f);
        rectTransform = GetComponent<RectTransform>();
        size_ref = rectTransform.sizeDelta;
        where_to_get_fuel = manager.CreateEntityQuery(typeof(TankComponent)).GetSingletonEntity();
        fuel = manager.GetComponentData<TankComponent>(where_to_get_fuel).fuel;
        max_fuel_ref = fuel;

    }
    // Update is called once per frame
    void Update()
    {
        // Maybe not very efficient
        if (where_to_get_fuel.Index != 0)
        {
            fuel = manager.GetComponentData<TankComponent>(where_to_get_fuel).fuel;

            rectTransform.sizeDelta = new float2(size_ref.x, size_ref.y * fuel/max_fuel_ref);
        }

    }
}
