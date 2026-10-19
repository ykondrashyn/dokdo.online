using UnityEngine;

public class CannonParent : MonoBehaviour
{
	public Transform[] cannonsL;

	public Transform[] cannonsR;

	private void Start()
	{
	}

	private void OnEnable()
	{
		if ((bool)GetComponentInParent<Enemy_Ship>())
		{
			GetComponentInParent<Enemy_Ship>().Cannons = this;
		}
		else if ((bool)GetComponentInParent<Assistant>())
		{
			GetComponentInParent<Assistant>().Cannons = this;
		}
	}

	private void Update()
	{
		for (int i = 0; i < cannonsL.Length; i++)
		{
			Vector3 eulerAngles = cannonsL[i].eulerAngles;
			eulerAngles.x = -20f;
			cannonsL[i].eulerAngles = eulerAngles;
		}
		for (int j = 0; j < cannonsR.Length; j++)
		{
			Vector3 eulerAngles2 = cannonsR[j].eulerAngles;
			eulerAngles2.x = -20f;
			cannonsR[j].eulerAngles = eulerAngles2;
		}
	}
}
