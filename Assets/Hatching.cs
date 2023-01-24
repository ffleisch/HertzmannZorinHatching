using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hatching : MonoBehaviour
{
    // Start is called before the first frame update

    [HideInInspector]
    public Contour contour;

    [HideInInspector]
    CrossFields crossFields;

    [HideInInspector]
    GenerateCrosshatch generateCrosshatch;



    //LineRendererGenerator lineRendererGenerator;
    CustomLineRenderer customLinerenderer;


    public float parabolicLimit = 0.1f;
    private float _parabolicLimit = 0;
    //public float parabolicLimit { get { return _parabolicLimit; } set { _parabolicLimit = value;recalculateCrossfields();recalculateHatching(); } }
    //private float _parabolicLimit = 0.05f;

    [HideInInspector]
    public Vector3[] e1;
    [HideInInspector]
    public Vector3[] e2;
    [HideInInspector]
    public float[] k1;
    [HideInInspector]
    public float[] k2;

    [HideInInspector]
    public Vector3[] vertices;
    [HideInInspector]
    public Vector3[] normals;
    [HideInInspector]
    public int[] triangles;


    RenderTexture directionRT;
    [HideInInspector]
    public Texture2D directionTex;

    RenderTexture brightnessRT;
    [HideInInspector]
    public Texture2D brightnessTex;

    [HideInInspector]
    Snapshot directionSnapShot;
    [HideInInspector]
    Snapshot brightnessSnapshot;


    public Camera myCamera;
    public Material brightnessMaterial;
    private Material _brightnessMaterial;

    [Range(1, 100)]
    public float dSep = 10;
    private float _dSep;

    [Range(0.1f, 1)]
    public float dTest = 0.5f;
    private float _dTest;

    [Range(0, 1)]
    public float lowerLimit = 0.65f;
    private float _lowerLimit;


    [Range(0, 1)]
    public float upperLimit = 0.95f;
    private float _upperLimit;

    [Range(0, 50)]
    public float lineWidth = 8;
    //public LineRenderer lineRenderer;
    //public GameObject lineRendererPrefab;


    public int renderLayer = 5;

    void Start()
    {

        _dSep = dSep;
        _dTest = dTest;
        _lowerLimit = lowerLimit;
        _upperLimit = upperLimit;
        _parabolicLimit = parabolicLimit;


        gameObject.layer = renderLayer;
        if (myCamera == null)
        {
            myCamera = Camera.main;
        }



        contour = gameObject.AddComponent<Contour>();
        crossFields = gameObject.AddComponent<CrossFields>();
        //lineRendererGenerator = gameObject.AddComponent<LineRendererGenerator>();


        GameObject lineChild = new("LineMesh");
        lineChild.transform.parent = transform;
        customLinerenderer = lineChild.AddComponent<CustomLineRenderer>();


        MeshFilter mf = GetComponent<MeshFilter>();

        /*if (lineRendererPrefab == null) {
			Debug.LogError("Line Renderer not set");

			GameObject go = new();
			go.AddComponent<LineRenderer>();
			lineRendererPrefab = go;
			
		}*/




        if (mf == null)
        {
            Debug.LogError("No Meshfilter found");
        }




        vertices = mf.mesh.vertices;
        triangles = mf.mesh.triangles;
        normals = mf.mesh.normals;
        float[] pointAreas;
        Vector3[] cornerAreas;

        //calculate curvatures
        MeshCurvature.ComputePointAndCornerAreas(vertices, triangles, out pointAreas, out cornerAreas);
        MeshCurvature.ComputeCurvature(vertices, normals, triangles, pointAreas, cornerAreas, out e1, out e2, out k1, out k2);

        contour.init();

        contour.CalcContourSegments();

        recalculateCrossfields();


        directionSnapShot = gameObject.AddComponent<Snapshot>();
        directionSnapShot.shader = Shader.Find("Unlit/CrossFieldShader");

        if (brightnessMaterial == null)
        {
            brightnessMaterial = new Material(Shader.Find("Standard"));
        }


        brightnessSnapshot = gameObject.AddComponent<Snapshot>();
        brightnessSnapshot.shader = brightnessMaterial.shader;

        directionRT = new RenderTexture(myCamera.pixelWidth, myCamera.pixelHeight, 16, RenderTextureFormat.ARGBFloat);
        directionTex = new Texture2D(myCamera.pixelWidth, myCamera.pixelHeight, TextureFormat.RGBAFloat, false);

        brightnessRT = new RenderTexture(myCamera.pixelWidth, myCamera.pixelHeight, 16, RenderTextureFormat.Default);
        brightnessTex = new Texture2D(myCamera.pixelWidth, myCamera.pixelHeight, TextureFormat.RGBA32, false);

        directionSnapShot.init(directionRT, directionTex, myCamera);
        brightnessSnapshot.init(brightnessRT, brightnessTex, myCamera);


        directionSnapShot.cullingMask = 1<<renderLayer;
        brightnessSnapshot.cullingMask = 1<<renderLayer;
        myCamera.cullingMask = ~(1<<renderLayer);


        generateCrosshatch = gameObject.AddComponent<GenerateCrosshatch>();

        generateCrosshatch.init(this, directionTex, brightnessTex);


        //lineRendererGenerator.init(this);
    }




    // Update is called once per frame
    void Update()
    {
        customLinerenderer.setLineWidth(lineWidth);


        bool doRecalculateHatching = false;
        bool doRecalculateCrossfields = false;
        bool doRecalculateHatchReduction = false;

        if (myCamera.transform.hasChanged || transform.hasChanged)
        {
            doRecalculateHatching = true;
            myCamera.transform.hasChanged = false;
            transform.hasChanged = false;

        }

        if (_dSep != dSep)
        {
            _dSep = dSep;
            doRecalculateHatching = true;
        }

        if (_dTest != dTest)
        {
            _dTest = dTest;
            doRecalculateHatching = true;
        }
        if (_lowerLimit != lowerLimit)
        {
            _lowerLimit = lowerLimit;
            doRecalculateHatchReduction = true;
        }
        if (_upperLimit != upperLimit)
        {
            _upperLimit = upperLimit;
            doRecalculateHatchReduction = true;
        }
        if (_parabolicLimit != parabolicLimit)
        {
            _parabolicLimit = parabolicLimit;
            doRecalculateCrossfields = true;
        }

        if (_brightnessMaterial != brightnessMaterial) {
            _brightnessMaterial = brightnessMaterial;

        
            doRecalculateHatchReduction = true;

            brightnessSnapshot.shader = brightnessMaterial.shader;
            brightnessSnapshot.takeSnapshot();
            
        }


        if (doRecalculateCrossfields)
        {
            recalculateCrossfields();
            recalculateHatching();
        }

        if (doRecalculateHatching)
        {
            recalculateHatching();
        }

        if ((!doRecalculateHatching) && doRecalculateHatchReduction)
        {
            recalculateReduceHatching();
        }

    }
    void recalculateHatching()
    {
        contour.CalcContourSegments();
        directionSnapShot.takeSnapshot();


        MeshRenderer mr =gameObject.GetComponent<MeshRenderer>();
        Material oldMat=mr.material;
        mr.material = brightnessMaterial;
        brightnessSnapshot.shader = brightnessMaterial.shader;

        brightnessSnapshot.takeSnapshot();
        mr.material = oldMat;

        generateCrosshatch.generateHatches();
        customLinerenderer.mf.mesh = generateCrosshatch.generateMixedLineMesh();

        customLinerenderer.transform.position =myCamera.ViewportToWorldPoint(new Vector3(0.5f,0.5f,(myCamera.nearClipPlane+myCamera.farClipPlane)/2f));
        //lineRendererGenerator.updateLineRenderers(generateCrosshatch.generateLinerendererPoints());	

    }

    void recalculateCrossfields()
    {

        MeshFilter mf = GetComponent<MeshFilter>();
        Debug.Log("Optmizing cross fields");
        crossFields.init(this);
        Debug.Log("Done optmizing cross fields");
        mf.mesh.SetTangents(crossFields.mixedTangents);

    }

    void recalculateReduceHatching()
    {
        generateCrosshatch.reduceHatches();

        customLinerenderer.mf.mesh = generateCrosshatch.generateMixedLineMesh();

        customLinerenderer.transform.position =myCamera.ViewportToWorldPoint(new Vector3(0.5f,0.5f,(myCamera.nearClipPlane+myCamera.farClipPlane)/2f));
    }

}
