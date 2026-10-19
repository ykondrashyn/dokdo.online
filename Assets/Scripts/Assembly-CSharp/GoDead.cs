using EZObjectPools;
using UnityEngine;

public class GoDead : MonoBehaviour
{
	private EZObjectPool shipboom;

	private void Awake()
	{
		shipboom = GameObject.Find("ship boom Effect").GetComponent<EZObjectPool>();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			other.GetComponentInParent<ShipController>().Dead();
			shipboom.TryGetNextObject(other.transform.position, Quaternion.identity);
		}
	}
}
