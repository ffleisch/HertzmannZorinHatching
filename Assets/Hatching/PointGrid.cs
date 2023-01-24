using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public struct Point {
	public Vector2 pos;
	public Vector2 dir;

	public Point(Vector2 pos, Vector2 dir)
	{
		this.pos = pos;
		this.dir = dir;
	}

}

public class StreamlinePoint
{

	public Vector2 pos;
	public Vector2 dir;
	public Vector2 d1;
	public Vector2 d2;




	float[,] distort = new float[2, 2];
	static float[,] rotate_45_right = { { 0.70710678118f, 0.70710678118f }, { -0.70710678118f, 0.70710678118f } };
	static float[,] rotate_45_left = { { 0.70710678118f, -0.70710678118f }, { 0.70710678118f, 0.70710678118f } };
	public bool inQueue;
	public bool marked;

	public int testIndex;


	public int brightnessLevel;
	public float lineWidth=1f;



	public StreamlinePoint(Vector2 pos, Vector2 dir,Vector2 d1, Vector2 d2) : this(pos,d1,d2)
	{
		this.dir = dir;
	}
	public StreamlinePoint(Vector2 pos,Vector2 d1, Vector2 d2)
	{
		this.pos = pos;

		this.dir = d1;

		float determinant = d1.x * d2.y - d1.y * d2.x;
		distort[0, 0] = d2.y / determinant;
		distort[0, 1] = -d2.x / determinant;
		distort[1, 0] = -d1.y / determinant;
		distort[1, 1] = d1.x / determinant;
		this.d1 = d1;
		this.d2 = d2;

	}

	public bool checkParallelToFirstCrossVector(Vector2 dir)//unused i think
	{
		float dotp_1 = dir.x * (-d1.y - d2.y) + dir.y * (d1.x + d2.x);//Vector2.Dot(d1, dir_last) / d1.magnitude;
		float dotp_2 = dir.x * (d1.y - d2.y) + dir.y * (-d1.x + d2.x);//Vector2.Dot(d2, dir_last) / d2.magnitude;
																	  //if (Mathf.Abs(dotp_1) < Mathf.Abs(dotp_2))
		return (dotp_1 * dotp_2 > 0);
	}

	public bool Parallel(Vector2 p1, Vector2 p2)
	{
		Vector2 v1= new Vector2(distort[0, 0] * p1.x + distort[0, 1] * p1.y,distort[1, 0] * p1.x + distort[1, 1] * p1.y);
		Vector2 v2= new Vector2(distort[0, 0] * p2.x + distort[0, 1] * p2.y,distort[1, 0] * p2.x + distort[1, 1] * p2.y);

		return Mathf.Abs(Vector2.Dot(v1,v2)/(v1.magnitude*v2.magnitude))>0.70710678118f;// 1/sqrt(2)=0.707=cos(45°)
	}


	public Vector2 getHatchPerpendicularVector(Vector2 dir) {

		Vector2 v1= new Vector2(distort[0, 0] * dir.x + distort[0, 1] * dir.y,distort[1, 0] * dir.x + distort[1, 1] * dir.y);
		return new Vector2(d1.x*v1.y-d2.x*v1.x,d1.y*v1.y-d2.y*v1.x);
	}

	public bool Parallel(StreamlinePoint other)
	{
		return Parallel(dir, other.dir);
	}

}


public class PointGrid{

	List<StreamlinePoint>[,] array;
	float stride;
	int w, h;
	int nx, ny;

	public PointGrid(int w, int h, float stride) {
		this.stride = stride;
		this.w = w;
		this.h = h;
		nx =Mathf.CeilToInt(w/stride);
		ny =Mathf.CeilToInt(h/stride);

		//Debug.Log("nx "+ nx+ " w "+ w + " nx*stride " + (nx*stride));
		//Debug.Log("ny "+ ny+ " h "+ h + " ny*stride " + (ny*stride));
		array = new List<StreamlinePoint>[nx,ny];
	}


	private (int, int) coords(Vector2 pos) {
		return ((int)(pos.x/stride),(int)(pos.y/stride));
	}
	public bool inBounds(Vector2 p) {
		return (p.x>0)&&(p.x<w)&&(p.y>0)&&(p.y<h);
	}
	public void insert(StreamlinePoint p) {
		if (inBounds(p.pos)) {
			(int x, int y) = coords(p.pos);
			if (array[x, y] == null) {
				array[x, y] = new();
			}
			array[x, y].Add(p);
		}
	
	}

	private bool inBoundsInt(int x ,int y) {
		return (x>0)&&(x<nx)&&(y>0)&&(y<ny);
	}
	public IEnumerable<StreamlinePoint> neighborhoodEnumerator(Vector2 pos) {
		if (!inBounds(pos)) {
			yield break;
		}

		(int x, int y) = coords(pos);
		for (int i = -1; i <= 1; i++) {
			for (int j = -1; j <= 1; j++)
			{
				if ((x+i>=0)&&(x+i<nx)&&(y+j>=0)&&(y+j<ny)) {
					if (array[x+i, y+j] != null) {
						foreach (var p in array[x+i, y+j]) {
							yield return p;
						}
					}
				}
			}
		}
		
	}
}
