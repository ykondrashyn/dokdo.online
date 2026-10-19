using UnityEngine;

public class BillBoard : MonoBehaviour
{
	private void Start()
	{
	}

	private void Update()
	{
		base.transform.rotation = Camera.main.transform.rotation;
	}
}
