using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomLineRenderer : MonoBehaviour
{
    public MeshFilter mf;
    private Material LineMaterial;
    private MeshRenderer mr;
    // Start is called before the first frame update
    void Start()
    {
        mf = GetComponent<MeshFilter>();
        if (mf == null) {
            mf=gameObject.AddComponent<MeshFilter>();
        }
        mr =GetComponent<MeshRenderer>();
        if (mr == null) {
            mr = gameObject.AddComponent<MeshRenderer>();
        }
        LineMaterial = Instantiate((Material)Resources.Load("FastScreenspaceLineMaterial", typeof(Material)));

        mr.material = LineMaterial;
    }


    public void setLineWidth(float width) {
        mr.material.SetFloat("_pixelWidth",width);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
