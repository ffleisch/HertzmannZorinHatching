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
	public Shader brightnessShader;

	public float dSep = 10;
	public float dTest = 0.5f;

	public float lowerLimit = 0.65f;
	public float upperLimit = 0.95f;

	//public LineRenderer lineRenderer;
	//public GameObject lineRendererPrefab;

	
    [SerializeField] private LayerMask renderLayer=5;

	void Start()
	{

		gameObject.layer =renderLayer;//TODO mybe make this configurable

		if (myCamera == null)
		{
			myCamera = Camera.main;
		}



		contour = gameObject.AddComponent<Contour>();
		crossFields = gameObject.AddComponent<CrossFields>();
		//lineRendererGenerator = gameObject.AddComponent<LineRendererGenerator>();


		GameObject lineChild = new("LineMesh");
		lineChild.transform.parent = transform;
		customLinerenderer=lineChild.AddComponent<CustomLineRenderer>();
		

		MeshFilter mf = GetComponent<MeshFilter>();

		/*if (lineRendererPrefab == null) {
			Debug.LogError("Line Renderer not set");

			GameObject go = new();
			go.AddComponent<LineRenderer>();
			lineRendererPrefab = go;
			
		}*/




		if (mf == null)
		{
			Debug.LogError("No Meshcollider found");
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
		Debug.Log("Optmizing cross fields");
		crossFields.init(this);
		Debug.Log("Done optmizing cross fields");
		mf.mesh.SetTangents(crossFields.mixedTangents);



		directionSnapShot = gameObject.AddComponent<Snapshot>();
		directionSnapShot.shader = Shader.Find("Unlit/CrossFieldShader");

		if (brightnessShader == null) {
			brightnessShader =Shader.Find("Standard");
		}


		brightnessSnapshot = gameObject.AddComponent<Snapshot>();
		brightnessSnapshot.shader = brightnessShader;

		directionRT = new RenderTexture(myCamera.pixelWidth, myCamera.pixelHeight, 16, RenderTextureFormat.ARGBFloat);
		directionTex = new Texture2D(myCamera.pixelWidth, myCamera.pixelHeight, TextureFormat.RGBAFloat, false);

		brightnessRT = new RenderTexture(myCamera.pixelWidth, myCamera.pixelHeight, 16, RenderTextureFormat.ARGBFloat);
		brightnessTex = new Texture2D(myCamera.pixelWidth, myCamera.pixelHeight, TextureFormat.RGBA32, false);
		
		directionSnapShot.init(directionRT, directionTex, myCamera);
		brightnessSnapshot.init(brightnessRT, brightnessTex, myCamera);

		generateCrosshatch = gameObject.AddComponent<GenerateCrosshatch>();

		generateCrosshatch.init(this, directionTex,brightnessTex);


		//lineRendererGenerator.init(this);
	}




	// Update is called once per frame
	void Update()
	{
		if (myCamera.transform.hasChanged || transform.hasChanged)
		{
			contour.CalcContourSegments();



			directionSnapShot.takeSnapshot();
			brightnessSnapshot.takeSnapshot();

			generateCrosshatch.generateHatches();
			myCamera.transform.hasChanged = false;
			transform.hasChanged = false;

			customLinerenderer.mf.mesh =generateCrosshatch.generateMixedLineMesh();
			//lineRendererGenerator.updateLineRenderers(generateCrosshatch.generateLinerendererPoints());	
		}


	}
}
