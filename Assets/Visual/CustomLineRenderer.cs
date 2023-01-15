using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomLineRenderer : MonoBehaviour
{
    public MeshFilter mf;
    // Start is called before the first frame update
    void Start()
    {
        mf = GetComponent<MeshFilter>();
        if (mf == null) {
            mf=gameObject.AddComponent<MeshFilter>();
        }
        MeshRenderer mr =GetComponent<MeshRenderer>();
        if (mr == null) {
            mr = gameObject.AddComponent<MeshRenderer>();
        }
        Material LineMaterial = (Material)Resources.Load("ScreenspaceLineMaterial", typeof(Material));

        mr.material = LineMaterial;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
