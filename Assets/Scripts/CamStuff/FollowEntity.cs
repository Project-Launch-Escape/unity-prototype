using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.UIElements;
using System.Collections;
using Unity.VisualScripting;
using System.Numerics;
[AddComponentMenu("Camera Stuff")]
public class FollowEntity : MonoBehaviour
{

    public Entity entitytofollow;
    private EntityManager manager;
    public float3 offset;

    public float sensitivity = 10f; // Sensitivity for manual camera rotation

    //private float mouseX;
    //private float mouseY;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private IEnumerator Start()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        yield return new WaitForSeconds(0.5f);
        entitytofollow = manager.CreateEntityQuery(typeof(BeFollowed)).GetSingletonEntity();
        transform.rotation = manager.GetComponentData<LocalTransform>(entitytofollow).Rotation;
        offset = manager.GetComponentData<BeFollowed>(entitytofollow).offset;
    }
    // Update is called once per frame
    void LateUpdate()
    {
        if (entitytofollow.Index == 0) { return; }
        transform.position = manager.GetComponentData<LocalToWorld>(entitytofollow).Position - manager.GetComponentData<LocalToWorld>(entitytofollow).Forward *5f;
        transform.rotation = manager.GetComponentData<LocalToWorld>(entitytofollow).Rotation;
        /*
        float3 t = transform.rotation.eulerAngles;
        transform.rotation = manager.GetComponentData<LocalTransform>(entitytofollow).Rotation;
        float3 t2 = transform.rotation.eulerAngles;
        t2[1] = t[1];
        t2[0] = t[0];
        transform.rotation = Quaternion.Euler(t2);*/
        //  transform.rotation = manager.GetComponentData<LocalTransform>(entitytofollow).Rotation;
        // Creating a float4 using the components


        //quaternion a = manager.GetComponentData<LocalTransform>(entitytofollow).Rotation;
        //quaternion b = transform.rotation;


        //mouseX += Input.GetAxis("Mouse X");
        //mouseY += Input.GetAxis("Mouse Y");
        //transform.Rotate(UnityEngine.Vector3.up * mouseX);
        //transform.Rotate(UnityEngine.Vector3.left * mouseY);
        /*
        float4 ra = new float4(math.
        (a.value.x * 100) / 100, math.round(a.value.y * 100) / 100, math.round(a.value.z * 100) / 100, math.round(a.value.w * 100) / 100);
        float4 rb = new float4(math.round(b.value.x * 100) / 100, math.round(b.value.y * 100) / 100, math.round(b.value.z * 100) / 100, math.round(b.value.w * 100) / 100);
        
        Debug.Log("Repere: "+ ra.ToString());
        Debug.Log("Actual: "+ rb.ToString());

        quaternion d = a * UnityEngine.Quaternion.Inverse(b);
        float4 rd = new float4(math.round(d.value.x * 100) / 100, math.round(d.value.y * 100) / 100, math.round(d.value.z * 100) / 100, math.round(d.value.w * 100) / 100);
        Debug.Log("SUB: " + rd.ToString());

        */



    }


}

