using System.Collections.Generic;
using UnityEngine;

public class Curver : MonoBehaviour
{
	public static Vector3[] MakeSmoothCurve(Vector3[] arrayToCurve, float smoothness)
	{
		int num = 0;
		int num2 = 0;
		if (smoothness < 1f)
		{
			smoothness = 1f;
		}
		num = arrayToCurve.Length;
		num2 = num * Mathf.RoundToInt(smoothness) - 1;
		List<Vector3> list = new List<Vector3>(num2);
		float num3 = 0f;
		for (int i = 0; i < num2 + 1; i++)
		{
			num3 = Mathf.InverseLerp(0f, num2, i);
			List<Vector3> list2 = new List<Vector3>(arrayToCurve);
			for (int num4 = num - 1; num4 > 0; num4--)
			{
				for (int j = 0; j < num4; j++)
				{
					list2[j] = (1f - num3) * list2[j] + num3 * list2[j + 1];
				}
			}
			list.Add(list2[0]);
		}
		return list.ToArray();
	}
}
