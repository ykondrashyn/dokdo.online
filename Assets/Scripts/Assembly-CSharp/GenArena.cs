using EZObjectPools;
using UnityEngine;

public class GenArena : MonoBehaviour
{
	private ShipController SC;

	private GameManager GM;

	public float r = 30f;

	private Vector3 ranpos;

	public EZObjectPool enemyPool;

	private Transform myT;

	private bool isGen;

	private GenPirate GP;

	public bool GenOk;

	private int lv = -1;

	private float localT;

	private void Awake()
	{
		GM = GameManager.Instance;
		SC = GameObject.Find("MyPos").GetComponent<ShipController>();
		enemyPool = GameObject.Find("Gen Enemy").GetComponent<EZObjectPool>();
		myT = base.transform;
		GP = GameObject.Find("Generators").GetComponent<GenPirate>();
	}

	private void Start()
	{
		GenOk = false;
	}

	public void init()
	{
		lv = 0;
		localT = 2f;
	}

	private void Gen()
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
			obj.GetComponent<Enemy_Ship>().init(lv);
			obj.SetActive(true);
			GP.pirateList.Add(obj);
		}
	}

	private void Gen5()
	{
		GM.PosiSound(2);
		if (lv > 6)
		{
			lv = 6;
		}
		if (GM.CurrentWorld.Equals(WorldType.Italy))
		{
			lv = 6;
		}
		for (int i = 0; i < 7; i++)
		{
			Gen();
		}
		lv++;
		localT = 2f;
	}

	private void FixedUpdate()
	{
		if (!SC.isArena)
		{
			return;
		}
		if (localT > 0f && GP.pirateList.Count <= 0 && lv >= 0)
		{
			localT -= Time.deltaTime;
			if (localT <= 0f)
			{
				GenOk = true;
			}
		}
		if (GenOk)
		{
			GenOk = false;
			Gen5();
		}
	}
}
