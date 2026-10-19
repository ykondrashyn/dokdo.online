using System.Collections;
using EZObjectPools;
using UnityEngine;

public class GenDokdo : MonoBehaviour
{
	private ShipController SC;

	public float r = 30f;

	private Vector3 ranpos;

	public EZObjectPool enemyPool;

	public float ShipDistance;

	private Transform myT;

	private Transform myship;

	private bool isGen;

	private GenPirate GP;

	private void Awake()
	{
		myship = GameObject.Find("MyPos").transform;
		SC = GameObject.Find("MyPos").GetComponent<ShipController>();
		enemyPool = GameObject.Find("Gen Enemy").GetComponent<EZObjectPool>();
		myT = base.transform;
		GP = GameObject.Find("Generators").GetComponent<GenPirate>();
	}

	private void Start()
	{
		StartCoroutine(gening());
	}

	private void Gen()
	{
		if (GP.pirateList.Count < GP.MaxPirateNum)
		{
			do
			{
				ranpos = myT.position + new Vector3(Random.Range(0f - r, r), 0f, Random.Range(0f - r, r));
			}
			while (Vector3.Distance(myT.position, ranpos) < r);
			GameObject obj = null;
			ranpos.y = 0f;
			enemyPool.TryGetNextObject(ranpos, Quaternion.identity, out obj);
			if (obj != null)
			{
				obj.GetComponent<Enemy_Ship>().isPirate = true;
				obj.GetComponent<Enemy_Ship>().init(6);
				obj.SetActive(true);
				GP.pirateList.Add(obj);
			}
		}
	}

	private IEnumerator gening()
	{
		yield return new WaitForSeconds(10f);
		while (true)
		{
			if (!SC.isParking && !SC.isDead)
			{
				ShipDistance = Vector3.Distance(myT.position, myship.position);
				if (ShipDistance < 150f)
				{
					for (int i = 0; i < Random.Range(1, GP.MaxPirateNum); i++)
					{
						Gen();
					}
				}
			}
			yield return new WaitForSeconds(10f);
		}
	}
}
