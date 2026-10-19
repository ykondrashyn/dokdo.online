using UnityEngine;

public class GetFish : MonoBehaviour
{
	private ShipController SC;

	private void Start()
	{
		SC = GameObject.Find("MyPos").GetComponent<ShipController>();
	}

	public void Get()
	{
		SC.GetFish();
	}
}
