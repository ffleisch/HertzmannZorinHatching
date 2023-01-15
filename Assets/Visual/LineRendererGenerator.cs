using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineRendererGenerator : MonoBehaviour
{
    private GameObject lineRendererParent;
    private Hatching h;
    public void init(Hatching h)
    {
        lineRendererParent = new GameObject("AllLineRenderers");
        this.h = h;
    }


    public void updateLineRenderers(List<Vector3[]> lines)
    {
    /*
        foreach (Transform t in lineRendererParent.transform)
        {
            GameObject.Destroy(t.gameObject);
        }


        foreach (Transform t in lineRendererParent.transform)
        {
            GameObject.Destroy(t);
        }

        //var lineRenderers=lineRendererParent.GetComponentsInChildren<LineRenderer>();




        for (int i = 0; i < lines.Count; i++)
        {

            Vector3[] l = lines[i];



            LineRenderer lr;
	            
            
            
            var newLr = Instantiate(h.lineRendererPrefab);
            newLr.transform.parent = lineRendererParent.transform;
            lr = newLr.GetComponent<LineRenderer>();
            lr.widthMultiplier = 0.01f;
            lr.useWorldSpace = true;
            //}


            lr.transform.parent = lineRendererParent.transform;
            //   LineRenderer lr = LineRendererParent.AddComponent(typeof(LineRenderer))as LineRenderer;


            lr.positionCount = l.Length;
            lr.SetPositions(l);

        }*/

    }



    public void setCameraTransform(Camera cam)
    {

    }
}
