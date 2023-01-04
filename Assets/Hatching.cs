using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hatching : MonoBehaviour
{
	// Start is called before the first frame update

	public Contour contour;

	CrossFields crossFields;

	GenerateCrosshatch generateCrosshatch;

	public float parabolicLimit = 0.1f;


	public Vector3[] e1;
	public Vector3[] e2;
	public float[] k1;
	public float[] k2;

	public Vector3[] vertices;
	public Vector3[] normals;
	public int[] triangles;



	RenderTexture directionRT;
	public Texture2D directionTex;

	Snapshot directionSnapShot;

	public Camera myCamera;

	public float dSep = 10;
	public float dTest = 0.5f;


	void Start()
	{

		if (myCamera == null)
		{
			myCamera = Camera.main;
		}



		contour = gameObject.AddComponent<Contour>();
		crossFields = gameObject.AddComponent<CrossFields>();

		MeshFilter mf = GetComponent<MeshFilter>();






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

		directionRT = new RenderTexture(myCamera.pixelWidth, myCamera.pixelHeight, 16, RenderTextureFormat.ARGBFloat);
		directionTex = new Texture2D(myCamera.pixelWidth, myCamera.pixelHeight, TextureFormat.RGBAFloat, false);

		directionSnapShot.init(directionRT, directionTex, myCamera);


		generateCrosshatch = gameObject.AddComponent<GenerateCrosshatch>();

		generateCrosshatch.init(this, directionTex);
	}

	// Update is called once per frame
	void Update()
	{
		if (myCamera.transform.hasChanged || transform.hasChanged)
		{
			contour.CalcContourSegments();
			directionSnapShot.takeSnapshot();
			myCamera.transform.hasChanged = false;
			transform.hasChanged = false;
		}


	}
}
