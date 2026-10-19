using UnityEngine;

public class InfiniteWater : MonoBehaviour
{
	public Transform myShip;

	private Transform myT;

	private Vector3 tempPos;

	private void Awake()
	{
		EnsureTransform();
	}

	private void Start()
	{
	}

	public void initPosition(Transform target)
	{
		EnsureTransform();
		tempPos = target.position;
		tempPos.y = 0f;
		myT.position = tempPos;
	}

	private void FixedUpdate()
	{
		RecenterIfNeeded();
	}

	public void RecenterIfNeeded()
	{
		EnsureTransform();
		if (myT != null && myShip != null &&
			(myT.position - myShip.position).sqrMagnitude > 49f)
		{
			initPosition(myShip);
		}
	}

	private void EnsureTransform()
	{
		if (myT == null)
		{
			myT = base.transform;
		}
	}
}
