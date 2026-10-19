using UnityEngine;

public class Arena_trigger : MonoBehaviour
{
	public ArenaDoor AD;

	public Transform enterPos;

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			AD.EnterPos = enterPos;
			AD.Enter();
		}
	}
}
