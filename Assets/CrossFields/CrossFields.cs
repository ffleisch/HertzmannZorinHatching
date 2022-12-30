using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class CrossFields : MonoBehaviour
{
	// Start is called before the first frame update



	void Start()
	{

	}

	public double[] crossfield;
	bool[] parabolic_criterion;

	public Dictionary<(int, int), float> phis;
	(int, int)[] edges;



	private Hatching h;

	internal  void init(Hatching h)
	{
		this.h = h;

		phis = new();


		parabolic_criterion = new bool[h.vertices.Length];

		for (int i = 0; i < h.vertices.Length; i++)
		{
			parabolic_criterion[i] = h.k2[i] / h.k1[i] < h.parabolicLimit;
		}


		Dictionary<(int, int), int> edge_dict = new();

		for (int i = 0; i < h.triangles.Length; i += 3)
		{
			int i1 = h.triangles[i];
			int i2 = h.triangles[i + 1];
			int i3 = h.triangles[i + 2];
			(int, int) e1 = i1 < i2 ? (i1, i2) : (i2, i1);
			(int, int) e2 = i2 < i3 ? (i2, i3) : (i3, i2);
			(int, int) e3 = i3 < i1 ? (i3, i1) : (i1, i3);
			if (!edge_dict.ContainsKey(e1)) { edge_dict[e1] = 0; }
			if (!edge_dict.ContainsKey(e2)) { edge_dict[e2] = 0; }
			if (!edge_dict.ContainsKey(e3)) { edge_dict[e3] = 0; }
		}
		edges = edge_dict.Keys.ToArray();


		//phis sind die Ringtungen der kante (e,s) in der Tangentialebene zu e (bestimmt durch normale)
		foreach ((int s, int e) in edges)
		{
			Vector3 edge = h.vertices[e] - h.vertices[s]; //kante von e richtung s
			Vector3 projected = Math3d.ProjectVectorOnPlane(h.normals[s], edge);
			Vector3 dir = h.e1[s];  //richtung der gößten krümmung, gibt die Richtung der Tangentialebene an

			//Debug.Log("test  :" +Vector3.Dot(Vector3.Cross(dir, projected), normals[s]));
			//Debug.Log("test2 :" +Vector3.Cross(dir, projected).magnitude);

			//phis[(s, e)] = (float)System.Math.Atan2(Vector3.Dot(Vector3.Cross(dir, projected), (normals[s])), Vector3.Dot(dir, projected));
			phis[(s, e)] = angleInPlane(projected, dir, h.normals[s]);


			Vector3 projected_2 = Math3d.ProjectVectorOnPlane(h.normals[e], -edge);
			Vector3 dir_2 = h.e1[e];  //richtung der gößten krümmung, gibt die Richtung der Tangentialebene an

			//phis[(e, s)] = (float)System.Math.Atan2(Vector3.Dot(Vector3.Cross(dir_2, projected_2), (normals[e])), Vector3.Dot(dir_2, projected_2));
			phis[(e, s)] = angleInPlane(projected_2, dir_2, h.normals[e]);
			//Debug.Log((s, e) + " " + phis[(s, e)]);
			//Debug.Log((e, s) + " " + phis[(e, s)]);
		}
		crossfield = new double[h.vertices.Length];


		//optimise the crossfield with bfgs
		Debug.Log("Energy " + cross_field_energy(crossfield));
		/*double epsg = 0;
		double epsf = 0;
		double epsx = 0.0000000001;
		int maxits = 0;*/


		alglib.minlbfgsstate state = new();
		alglib.minlbfgscreate(4, crossfield, out state);

		//alglib.minlbfgssetcond(state, epsg, epsf, epsx, maxits);

        float tStart=Time.realtimeSinceStartup;
        alglib.minlbfgsoptimize(state, cross_field_energy_grad, null, this);
        float tEnd = Time.realtimeSinceStartup;
		
		
		alglib.minlbfgsreport rep;
		alglib.minlbfgsresults(state, out crossfield, out rep);
		Debug.Log("Terminationtype " + rep.terminationtype);
		//Debug.Log("Terminationtype "+alglib.ap.format(x,2)); 
		Debug.Log("Energy " + cross_field_energy(crossfield));
        Debug.Log("Took "+(tEnd-tStart)+" seconds");

		composeMixedTangents();

	}

	public Vector4[] mixedTangents;
	void composeMixedTangents() {
		mixedTangents = new Vector4[h.e1.Length];

		for (int i = 0; i < h.e1.Length; i++) {
			Vector3 majorCurvature=h.e1[i];
			mixedTangents[i] =new Vector4(majorCurvature.x,majorCurvature.y,majorCurvature.z,(float)crossfield[i]);
		}

	}



	float angleInPlane(Vector3 vec, Vector3 plane_dir, Vector3 normal)
    {
        //https://stackoverflow.com/questions/5188561/signed-angle-between-two-3d-vectors-with-same-origin-within-the-same-plane
        //left handed
        return (float)System.Math.Atan2(Vector3.Dot(Vector3.Cross(plane_dir, vec), normal), Vector3.Dot(plane_dir, vec));
    }
	double cross_field_energy(double[] crossfield)
	{
		double res = 0;
		double[] grad = new double[crossfield.Length];
		cross_field_energy_grad(crossfield, ref res, grad, this);
		/*foreach (double g in grad) {
            Debug.Log(g);
        }*/
		return res;
	}

	public static void cross_field_energy_grad(double[] arg, ref double func, double[] grad, object obj)
	{
		CrossFields cf = (CrossFields)obj;
		double sum = 0;
		for (int i = 0; i < grad.Length; i++)
		{
			grad[i] = 0;
		}
		foreach ((int s, int e) in cf.edges)
		{
			if (cf.parabolic_criterion[s] && cf.parabolic_criterion[e])
			{
				continue;
			}
			double inner = (arg[s] - cf.phis[(s, e)]) - (arg[e] - cf.phis[(e, s)]);
			sum -= System.Math.Cos(4 * inner);
			grad[s] += 4 * System.Math.Sin(4 * inner);
			grad[e] -= 4 * System.Math.Sin(4 * inner);
		}
		func = sum;

	}



	// Update is called once per frame
	void Update()
	{

	}
}
