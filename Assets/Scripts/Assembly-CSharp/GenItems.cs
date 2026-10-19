using System.Collections;
using EZObjectPools;
using UnityEngine;

public class GenItems : MonoBehaviour
{
	public EZObjectPool itemPool;

	public float r = 15f;

	public float Timer;

	private Vector3 ranpos;

	private Transform myShip;

	private ShipController SC;

	private void Awake()
	{
		myShip = GameObject.Find("MyPos").transform;
		SC = myShip.GetComponent<ShipController>();
	}

	private void Start()
	{
		StartCoroutine(gening());
	}

	private void Gen()
	{
		do
		{
			ranpos = myShip.position + new Vector3(Random.Range(0f - r, r), 0f, Random.Range(0f - r, r));
		}
		while (Vector3.Distance(myShip.position, ranpos) < r);
		GameObject obj = null;
		ranpos.y = 0f;
		itemPool.TryGetNextObject(ranpos, Quaternion.identity, out obj);
		if (obj != null)
		{
			obj.SetActive(true);
		}
	}

	private IEnumerator gening()
	{
		yield return new WaitForSeconds(Timer);
		while (true)
		{
			if (!SC.isParking && !SC.isDead && !SC.isArena)
			{
				Gen();
			}
			yield return new WaitForSeconds(Timer);
		}
	}
}
