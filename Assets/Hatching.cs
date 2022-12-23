using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hatching : MonoBehaviour
{
    // Start is called before the first frame update

    Contour contour;

    public Vector3[] e1;
    public Vector3[] e2;
    public float[] k1;
    public float[] k2;


    void Start()
    {




        contour = gameObject.AddComponent<Contour>();


        MeshFilter mf = GetComponent<MeshFilter>();






        if (mf == null)
        {
            Debug.LogError("No Meshcollider found");
        }




        var vertices = mf.mesh.vertices;
        var triangles = mf.mesh.triangles;
        var normals = mf.mesh.normals;
        float[] pointAreas;
        Vector3[] cornerAreas;

        //calculate curvatures
        MeshCurvature.ComputePointAndCornerAreas(vertices, triangles, out pointAreas, out cornerAreas);
        MeshCurvature.ComputeCurvature(vertices, normals, triangles, pointAreas, cornerAreas, out e1, out e2, out k1, out k2);

        contour.init();

    }

    // Update is called once per frame
    void Update()
    {
        contour.CalcContourSegments();
    }
}
