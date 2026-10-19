using UnityEngine;

public class Homing : MonoBehaviour
{
	public Transform Target;

	private Transform myT;

	private float dis;

	private Vector3 temppos;

	private float angle;

	private Quaternion a;

	private void Awake()
	{
		myT = base.transform;
	}

	private void Start()
	{
	}

	private void FixedUpdate()
	{
		dis = Vector3.Distance(Target.position, myT.position);
		if (dis > 10f)
		{
			temppos = myT.position;
			temppos = Vector3.Lerp(temppos, Target.position, Time.deltaTime * 0.3f);
			temppos.y = 0f;
			myT.position = temppos;
		}
		angle = GetAngle2(myT.position, Target.position) - 90f;
		a = Quaternion.AngleAxis(angle, Vector3.down);
		myT.rotation = Quaternion.Lerp(myT.rotation, a, Time.deltaTime * 1f);
	}

	private float GetAngle2(Vector3 vStart, Vector3 vEnd)
	{
		Vector3 vector = vEnd - vStart;
		return Mathf.Atan2(vector.z, vector.x) * 57.29578f;
	}
}
