using UnityEngine;
using UnityEngine.Events;

public class ShootCheck : MonoBehaviour
{
	private float bulletLocalT;

	public string TargetTag;

	public bool isTarget;

	public UnityEvent shoot;

	private ShipController SC;

	private void Awake()
	{
		SC = GameObject.Find("MyPos").GetComponent<ShipController>();
	}

	private void OnEnable()
	{
		isTarget = false;
	}

	private void Update()
	{
		if (bulletLocalT > 0f)
		{
			bulletLocalT -= Time.deltaTime;
		}
		else if (isTarget && !SC.isParking)
		{
			bulletLocalT = 3f;
			isTarget = false;
			shoot.Invoke();
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (other.CompareTag(TargetTag))
		{
			isTarget = true;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag(TargetTag))
		{
			isTarget = false;
		}
	}
}
