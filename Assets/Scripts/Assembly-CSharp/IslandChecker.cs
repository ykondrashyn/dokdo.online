using System.Collections.Generic;
using EZObjectPools;
using UnityEngine;
using UnityEngine.Events;

public class IslandChecker : MonoBehaviour
{
	[HideInInspector]
	public int IslandNumber;

	public int Tier;

	private ShipController SC;

	private GameManager GM;

	public float r = 15f;

	private Vector3 ranpos;

	public List<GameObject> IslandEnemy = new List<GameObject>();

	public bool isdone;

	public EZObjectPool enemyPool;

	public float ShipDistance;

	private Transform myT;

	private Transform myship;

	private bool isGen;

	private GameObject Fog;

	private SpriteRenderer worldMap;

	private UnityAction whenPark;

	private Vector3 mar = new Vector3(0f, 0f, -20f);

	private void Awake()
	{
		GM = GameManager.Instance;
		myship = GameObject.Find("MyPos").transform;
		SC = GameObject.Find("MyPos").GetComponent<ShipController>();
		enemyPool = GameObject.Find("Gen Enemy").GetComponent<EZObjectPool>();
		myT = base.transform;
		Fog = base.transform.Find("IslandFog").gameObject;
		whenPark = Regen;
		if (base.transform.parent.parent.Find("WorldMapPointer") != null)
		{
			worldMap = base.transform.parent.parent.Find("WorldMapPointer").GetComponent<SpriteRenderer>();
		}
		else
		{
			worldMap = base.transform.parent.Find("WorldMapPointer").GetComponent<SpriteRenderer>();
		}
	}

	private void OnEnable()
	{
		Fog.SetActive(false);
		EventManager.StartListening(MyEvent.isParking, whenPark);
	}

	private void OnDisable()
	{
		EventManager.StopListening(MyEvent.isParking, whenPark);
	}

	private void Regen()
	{
		if (IslandEnemy.Count > 0)
		{
			GameObject[] array = IslandEnemy.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(false);
			}
		}
		IslandEnemy.Clear();
		isGen = false;
	}

	private void Start()
	{
		if (!GM.IslandDone[IslandNumber])
		{
			isdone = false;
			isGen = false;
		}
		else
		{
			isdone = true;
			isGen = true;
		}
		worldMap.color = SC.OtherColor[GM.AmblemNumber(IslandNumber)];
	}

	private void GenIfCome()
	{
		if (IslandEnemy.Count > 0)
		{
			for (int i = 0; i < IslandEnemy.Count; i++)
			{
				IslandEnemy[i].SetActive(false);
			}
		}
		IslandEnemy.Clear();
		int num = 1;
		if (GM.CurrentWorld.Equals(WorldType.Dokdo))
		{
			switch (Tier)
			{
			case 0:
				num = 3;
				break;
			case 1:
				num = 3;
				break;
			case 2:
				num = 4;
				break;
			case 3:
				num = 4;
				break;
			case 4:
				num = 5;
				break;
			case 5:
				num = 5;
				break;
			case 6:
				num = 6;
				break;
			}
		}
		else
		{
			num = 10;
		}
		for (int j = 0; j < num; j++)
		{
			GenArmy();
		}
	}

	private void GenArmy()
	{
		do
		{
			ranpos = myT.position + mar + new Vector3(Random.Range(0f - r, r), 0f, Random.Range(0f - r, r));
		}
		while (Vector3.Distance(myT.position, ranpos) < r);
		ranpos.y = 0f;
		GameObject obj;
		if (!enemyPool.TryGetNextObject(ranpos, Quaternion.identity, out obj))
		{
			return;
		}
		obj.GetComponent<Enemy_Ship>().SetHome(IslandNumber);
		obj.GetComponent<Enemy_Ship>().isPirate = false;
		obj.GetComponent<Enemy_Ship>().init(Tier);
		obj.SetActive(true);
		IslandEnemy.Add(obj);
		obj.GetComponent<Enemy_Ship>().IslandCheck = this;
	}

	private void FixedUpdate()
	{
		if (!isdone)
		{
			if (!SC.isParking)
			{
				if (!GM.IslandDone[IslandNumber])
				{
					ShipDistance = Vector3.Distance(myT.position, myship.position);
					if (ShipDistance < 150f)
					{
						if (GM.CurrentWorld.Equals(WorldType.Dokdo))
						{
							Fog.SetActive(true);
						}
					}
					else
					{
						Fog.SetActive(false);
					}
					if (ShipDistance < 80f)
					{
						GM.isNearIsland = true;
						if (GM.CurrentIsland == null)
						{
							GM.CurrentIsland = this;
						}
						else if (!GM.CurrentIsland.Equals(this))
						{
							GM.CurrentIsland = this;
						}
						if (!isGen)
						{
							isGen = true;
							GenIfCome();
						}
					}
					else
					{
						if (ShipDistance > 120f)
						{
							Regen();
						}
						if (GM.CurrentIsland != null && GM.CurrentIsland.Equals(this))
						{
							GM.CurrentIsland = null;
						}
					}
					if (IslandEnemy.Count <= 0 && isGen)
					{
						isdone = true;
						SC.GetRewardIsland(IslandNumber);
						Fog.SetActive(false);
						worldMap.color = SC.MyColor;
						if (GM.CurrentIsland != null && GM.CurrentIsland.Equals(this))
						{
							GM.CurrentIsland = null;
						}
					}
				}
				else
				{
					Fog.SetActive(false);
				}
			}
			else
			{
				Regen();
				Fog.SetActive(false);
				if (GM.CurrentIsland != null && GM.CurrentIsland.Equals(this))
				{
					GM.CurrentIsland = null;
				}
			}
			return;
		}
		worldMap.color = SC.MyColor;
		if (GM.CurrentIsland != null && GM.CurrentIsland.Equals(this))
		{
			GM.CurrentIsland = null;
		}
		ShipDistance = Vector3.Distance(myT.position, myship.position);
		if (ShipDistance < 50f)
		{
			if (ShipDistance < 40f)
			{
				SC.isFishing = true;
				GM.isNearIsland = true;
			}
			else
			{
				SC.isFishing = false;
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player") && GM.IslandDone[IslandNumber] && !SC.isParking)
		{
			SC.Parking(IslandNumber);
		}
	}
}
