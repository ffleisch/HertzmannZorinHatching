using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Snapshot : MonoBehaviour
{
	// Start is called before the first frame update
	void Start()
	{
		//initTest();
		if (myCam == null)
		{
			myCam = Instantiate<Camera>(Camera.main);
			myCam.transform.parent = Camera.main.transform;
			myCam.name = "SnapshotHelperCam";
			myCam.tag = "Untagged";
		}

		var audioListener = referenceCam.GetComponent<AudioListener>();
		if (audioListener)
		{
			GameObject.Destroy(audioListener);
		}
	}

	public RenderTexture rt;
	public Shader shader;
	public Camera referenceCam;
	private static Camera myCam;

	private Texture2D tex;

	public void init(RenderTexture rt,Texture2D tex,Camera referenceCam)
	{
		this.rt = rt;
		this.tex = tex;
		this.referenceCam = referenceCam;
	}




	public void takeSnapshot()
	{
		myCam.CopyFrom(referenceCam);

		referenceCam.enabled = false;
		myCam.enabled = true;

		myCam.clearFlags = CameraClearFlags.Color;
		myCam.backgroundColor = new Color(0,0,0,0);
		myCam.cullingMask =0b100000;
		myCam.targetTexture = rt;
		RenderTexture.active = rt;
		myCam.RenderWithShader(shader, "");



		Rect regionToReadFrom = new Rect(0, 0, tex.width, tex.height);
		tex.ReadPixels(regionToReadFrom, 0, 0, false);
		tex.Apply();
		referenceCam.enabled = true;
		myCam.enabled = false;
	}

}
