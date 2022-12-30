using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GenerateCrosshatch : MonoBehaviour
{
	Texture2D directionImage;
	Hatching h;

	// Start is called before the first frame update
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{

	}


	public void init(Hatching h,Texture2D directionImage)
	{
		this.directionImage = directionImage;
		this.h = h;
	}


	(Vector2, Vector2) GetDirectionsFromImage(Vector2 p)
	{
		if (p.x >= 0 && p.x < directionImage.width && p.y >= 0 && p.y < directionImage.height)
		{
			Vector4 c = directionImage.GetPixel((int)p[0], (int)p[1]);

			return (new Vector2(c.x, c.y), new Vector2(c.z, c.w));
		}
		else
		{
			return (Vector2.zero, Vector2.zero);
		}
	}

	Vector2 GetAlignedVectorFromImage(Vector2 p, Vector2 dir) {
		(Vector2 d1, Vector2 d2) = GetDirectionsFromImage(p);
		float dotp_1 = dir.x * (-d1.y - d2.y) + dir.y * (d1.x + d2.x);
		float dotp_2 = dir.x * (d1.y - d2.y) + dir.y * (-d1.x + d2.x);
		Vector2 dir_out = dotp_1*dotp_2>0?d1:d2;

		int same_dir = Vector2.Dot(dir, dir_out) > 0 ? 1 : -1;
		return same_dir*dir_out;
	}



	private void OnDrawGizmos()
	{
		(Vector2 d1, Vector2 d2) = GetDirectionsFromImage(Input.mousePosition);

		float mult = Mathf.Tan(Mathf.PI * Camera.main.fieldOfView / 360f);
		float w = Camera.main.scaledPixelWidth;
		float h = Camera.main.scaledPixelHeight;

		Matrix4x4 flatMatrix = Camera.main.cameraToWorldMatrix * Matrix4x4.Translate(-Vector3.forward) * Matrix4x4.Scale(new Vector3(Camera.main.aspect * mult, mult, 1)); //* Matrix4x4.Scale(new Vector3(1, Camera.main.pixelHeight / (float)Camera.main.pixelWidth, 1));
		flatMatrix = flatMatrix * Matrix4x4.Translate(new Vector3(-1, -1, 0)) * Matrix4x4.Scale(new Vector3(2 / w, 2 / h, 1));

		Handles.matrix = flatMatrix;


		Handles.color = Color.red;
		Handles.DrawLine(Input.mousePosition, Input.mousePosition + (Vector3)d1 * 200);
		Handles.color = Color.green;
		Handles.DrawLine(Input.mousePosition, Input.mousePosition + (Vector3)d2 * 200);
		generateStreamLine(Input.mousePosition);

	}


	int maxits = 10000;
	void generateStreamLine(Vector2 start)
	{

		float stepSize = h.dSep / 2f;
		Vector2 pos = start;
		Vector2 dir_last = Vector2.up;//TODO

		for (int i = 0; i < maxits; i++)
		{
			Vector2 dir = GetAlignedVectorFromImage(pos,dir_last);

			Vector2 step = dir.normalized*stepSize;
			pos += step;
			Handles.color = Color.black;
			Handles.DrawLine(pos, pos+Vector2.up, 0.1f);

			dir_last = dir;

			if (dir == Vector2.zero) {
				break;
			}
		}

	}


}
