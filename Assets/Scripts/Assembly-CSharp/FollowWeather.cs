using UnityEngine;

public class FollowWeather : MonoBehaviour
{
	public Transform myShip;

	public Vector3 addPos;

	private Transform myT;

	public float distance;

	public float ResetPosDistance;

	private Vector3 tempPos;

	private void Awake()
	{
		myT = base.transform;
	}

	private void LateUpdate()
	{
		distance = Vector3.Distance(myShip.position, myT.position);
		if (distance > ResetPosDistance)
		{
			tempPos = myShip.position;
			tempPos.y = 0f;
			if (myShip.position.x > myT.position.x)
			{
				myT.position = tempPos + addPos;
			}
			else
			{
				myT.position = tempPos - addPos;
			}
		}
	}
}
