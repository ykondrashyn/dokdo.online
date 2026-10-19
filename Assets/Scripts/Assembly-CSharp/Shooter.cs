using System.Collections.Generic;
using UnityEngine;

public class Shooter : MonoBehaviour
{
	private float bulletLocalT;

	public string TargetTag;

	public bool isTarget;

	private ShipController SC;

	public Transform ShootPoint;

	public Transform guide;

	public Transform RotTrans;

	public Transform ParentTrans;

	public List<Transform> targetList = new List<Transform>();

	private Quaternion angle;

	private Vector3 rot2 = Vector3.zero;

	private float h;

	private float s;

	private void Awake()
	{
		SC = GameObject.Find("MyPos").GetComponent<ShipController>();
	}

	private void OnEnable()
	{
		ResetTarget();
	}

	private void FixedUpdate()
	{
		CheckSight();
		if (bulletLocalT > 0f)
		{
			if (isTarget)
			{
				bulletLocalT -= Time.deltaTime;
			}
		}
		else if (isTarget && !SC.isParking)
		{
			bulletLocalT = 2.8f;
			isTarget = false;
			Invoke("Shoot", 0.1f);
		}
	}

	private void CheckSight()
	{
		if (SC.NearEnemys.Count > 0 && !SC.isParking)
		{
			for (int i = 0; i < SC.NearEnemys.Count; i++)
			{
				if (isOnSight(SC.NearEnemys[i]))
				{
					if (!targetList.Contains(SC.NearEnemys[i]))
					{
						targetList.Add(SC.NearEnemys[i]);
					}
				}
				else if (targetList.Contains(SC.NearEnemys[i]))
				{
					targetList.Remove(SC.NearEnemys[i]);
				}
			}
			if (targetList.Count > 0)
			{
				if (!SC.NearEnemys.Contains(targetList[0]))
				{
					targetList.RemoveAt(0);
					return;
				}
				angle = Quaternion.AngleAxis(GetAngle2(RotTrans.position, targetList[0].position), Vector3.down);
				RotTrans.localEulerAngles = limitAng(angle);
				Vector3 eulerAngles = ShootPoint.eulerAngles;
				eulerAngles.x = -10f;
				ShootPoint.eulerAngles = eulerAngles;
				isTarget = true;
			}
			else
			{
				ResetTarget();
			}
		}
		else if (isTarget)
		{
			ResetTarget();
		}
	}

	private void ResetTarget()
	{
		targetList.Clear();
		isTarget = false;
		bulletLocalT = 0.5f;
	}

	private void Shoot()
	{
		SC.ShootEach(ShootPoint.position, ShootPoint.rotation);
	}

	private Vector3 limitAng(Quaternion rot)
	{
		guide.rotation = rot;
		h = guide.localEulerAngles.y;
		if (h > 180f)
		{
			s = h - 360f;
		}
		else if (h < -180f)
		{
			s = 360f + h;
		}
		else
		{
			s = h;
		}
		rot2.y = Mathf.Clamp(s, -40f, 40f);
		return rot2;
	}

	private bool isOnSight(Transform target)
	{
		bool result = false;
		float num = Vector3.Angle(target.position - RotTrans.position, ParentTrans.forward);
		if (num < 50f)
		{
			result = true;
		}
		return result;
	}

	private float GetAngle2(Vector3 vStart, Vector3 vEnd)
	{
		Vector3 vector = vEnd - vStart;
		return Mathf.Atan2(vector.z, vector.x) * 57.29578f - 90f;
	}
}
