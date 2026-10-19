using UnityEngine;

public class FollowShip : MonoBehaviour
{
	public Transform target;

	private Transform myT;

	private ShipController SC;

	private void Awake()
	{
		myT = base.transform;
		SC = GameObject.Find("MyPos").GetComponent<ShipController>();
	}

	private void Start()
	{
	}

	private void FixedUpdate()
	{
		if ((!SC.isParking || SC.isOuting) && !SC.isDead)
		{
			myT.position = target.position;
		}
	}
}
