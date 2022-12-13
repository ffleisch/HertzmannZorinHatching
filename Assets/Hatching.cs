using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hatching : MonoBehaviour
{
    // Start is called before the first frame update

    Contour contour;
    void Start()
    {
        contour = gameObject.AddComponent<Contour>();
    }

    // Update is called once per frame
    void Update()
    {
        contour.CalcContourSegments();
    }
}
