using DG.Tweening;
using UnityEngine;

public class Craken : MonoBehaviour
{
	public Animator animator;

	private Transform myT;

	private Transform myship;

	private void Awake()
	{
		myT = base.transform;
		myship = GameObject.Find("MyPos").transform;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			animator.SetTrigger("atk");
			myT.DOKill();
			myT.DOLookAt(myship.position, 1f, AxisConstraint.W, myT.up);
			Invoke("Off", 3f);
		}
	}

	private void Off()
	{
		base.gameObject.SetActive(false);
	}
}
