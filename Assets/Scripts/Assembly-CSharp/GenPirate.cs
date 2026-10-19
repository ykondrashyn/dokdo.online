using System.Collections;
using System.Collections.Generic;
using EZObjectPools;
using UnityEngine;

public class GenPirate : MonoBehaviour
{
	public EZObjectPool itemPool;

	public float r = 55f;

	public int MaxPirateNum = 10;

	public float Timer;

	private float tempT;

	private Vector3 ranpos;

	private Transform myShip;

	private ShipController SC;

	public List<GameObject> pirateList = new List<GameObject>();

	private float Dis;

	private GameManager GM;

	private Transform genPos;

	private void Awake()
	{
		GM = GameManager.Instance;
		myShip = GameObject.Find("MyPos").transform;
		genPos = myShip.Find("genPos");
		SC = myShip.GetComponent<ShipController>();
	}

	private void Start()
	{
		StartCoroutine(gening());
	}

	private void Gen()
	{
		Dis = Vector3.Distance(myShip.position, Vector3.zero);
		int num = 0;
		num = ((!(Dis < 300f)) ? ((Dis < 400f) ? 1 : ((Dis < 600f) ? 2 : ((Dis < 800f) ? 3 : ((!(Dis < 900f)) ? 5 : 4)))) : 0);
		if (!GM.CurrentWorld.Equals(WorldType.Dokdo) && num >= 5)
		{
			num = 4;
		}
		if (pirateList.Count >= num + 2)
		{
			return;
		}
		do
		{
			ranpos = genPos.position + new Vector3(Random.Range(0f - r, r), 0f, Random.Range(0f - r, r));
		}
		while (Vector3.Distance(genPos.position, ranpos) < r);
		GameObject obj = null;
		ranpos.y = 0f;
		itemPool.TryGetNextObject(ranpos, Quaternion.identity, out obj);
		if (obj != null)
		{
			if (!SC.isArena && Random.Range(0, 200).Equals(1))
			{
				num = 6;
			}
			obj.GetComponent<Enemy_Ship>().isPirate = true;
			obj.GetComponent<Enemy_Ship>().init(num);
			obj.SetActive(true);
			pirateList.Add(obj);
		}
	}

	private IEnumerator gening()
	{
		yield return new WaitForSeconds(10f);
		while (true)
		{
			if (!SC.isParking && !SC.isDead && !SC.isArena)
			{
				if (!GM.isNearIsland)
				{
					Dis = Vector3.Distance(myShip.position, Vector3.zero);
					int max = ((Dis < 500f) ? 2 : ((!(Dis < 900f)) ? 4 : 3));
					for (int i = 0; i < Random.Range(1, max); i++)
					{
						Gen();
					}
				}
				else
				{
					GM.isNearIsland = false;
				}
			}
			yield return new WaitForSeconds(Timer + (float)pirateList.Count * 5f);
		}
	}
}
