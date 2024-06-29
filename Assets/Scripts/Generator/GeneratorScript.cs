using Unity.Burst;
using UnityEngine;

[RequireComponent (typeof(MeshFilter),typeof(MeshRenderer))]
public class GeneratorScript : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;

    [SerializeField]
    int gridSizeX = 50;
    [SerializeField]
    int gridSizeY = 50;
    [SerializeField]
    float cellsize = 0.5f;
    [SerializeField]
    float v1 = 0.1f;
    [SerializeField]
    float v2 = 4f;
    private void Awake()
    {
        mesh = GetComponent<MeshFilter>().mesh;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Update()
    {
        //Make Mesh
        //int gridSizeX = 50;
        //int gridSizeY = 50;
        //float cellsize = 0.5f;
        vertices = new Vector3[((gridSizeX + 1) * (1 + gridSizeY))*2];
        triangles = new int[(gridSizeX * gridSizeY * 6)*2];

        int a = 0;
        int b = 0;
        float vectorOffset = cellsize * 0.5f;


        //Creat POINTS

        for (int x = 0; x <= gridSizeX; x++)
        {
            for (int y = 0; y <= gridSizeY; y++)
            {
                vertices[a] = new Vector3(x * cellsize, 5, y * cellsize);
                a += 1;
            }
        }
        for (int x = 0; x <= gridSizeX; x++)
        {
            for (int y = 0; y <= gridSizeY; y++)
            {
                vertices[a] = new Vector3(x * cellsize, -5, y * cellsize);
                a += 1;

            }
        }
        //Create Quads
        int t = 0;
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {

                triangles[b] = t + 1;
                triangles[b + 1] = t + gridSizeY + 1;
                triangles[b + 2] = t + 0;

                triangles[b + 3] = t + 1;
                triangles[b + 4] = t + gridSizeY + 2;
                triangles[b + 5] = t + gridSizeY + 1;
                b += 6;
                t += 1;
                if (y + 1 == gridSizeY) { t += 1; }
            }
        }
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {

                triangles[b] = t + 1;
                triangles[b + 1] = t + gridSizeY + 1;
                triangles[b + 2] = t + 0;

                triangles[b + 3] = t + 1;
                triangles[b + 4] = t + gridSizeY + 2;
                triangles[b + 5] = t + gridSizeY + 1;
                b += 6;
                t += 1;
                if (y + 1 == gridSizeY) { t += 1; }
            }
        }
        //Update Mesh
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    void bosse()
    {
        //Make Mesh
        //int gridSizeX = 50;
        //int gridSizeY = 50;
        //float cellsize = 0.5f;
        vertices = new Vector3[(gridSizeX+1) * (1+gridSizeY)];
        triangles = new int[gridSizeX * gridSizeY * 6];

        int a = 0;
        int b = 0;
        float vectorOffset = cellsize*0.5f;

       
        //Creat POINTS
        for(int x = 0;x <= gridSizeX; x++)
        {
            for(int y = 0; y <= gridSizeY; y++)
            {
                float x1 = (x) - gridSizeX * 0.5f;
                float y1 = (y) - gridSizeY * 0.5f;
                float m = Mathf.Exp(v1*0.01f * (-(x1 * x1) - (y1 * y1))) * v2*4;

                vertices[a] = new Vector3(x * cellsize, m, y * cellsize);
                a += 1;

            }
        }

        //Create Quads
        int t = 0;
        for (int x = 0; x < gridSizeX ; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                
                triangles[b+2]   = t + 1;
                triangles[b+1] = t + gridSizeY + 1;
                triangles[b] = t + 0;

                triangles[b+5] = t +  1;
                triangles[b+4] = t +  gridSizeY + 2;
                triangles[b+3] = t +  gridSizeY + 1;
                b +=6;
                t += 1;
                if (y+1 == gridSizeY) { t += 1; }
            }    
        }
        //Update Mesh
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
    }

}
