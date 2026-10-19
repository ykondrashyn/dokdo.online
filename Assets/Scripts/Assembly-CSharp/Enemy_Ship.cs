using System.Collections.Generic;
using DG.Tweening;
using EZObjectPools;
using UnityEngine;

public class Enemy_Ship : MonoBehaviour
{
	[Header("CurrentData")]
	public int CurrentHP;

	public int Atk;

	public bool isDead;

	public bool LockOn;

	public bool isPirate;

	public bool isArena;

	public IslandChecker IslandCheck;

	public Transform HEAD;

	public int Lv_body;

	public int Lv_Sail;

	public int Lv_Head;

	public int Lv_Cannon;

	[Header("BaseData")]
	public int HP;

	public float AtkRange;

	public float DetactRange;

	public float ShootPower;

	public float motorForce;

	public float steerForce;

	[Header("Other")]
	public Transform tempobj;

	public Transform myShip;

	public Transform boat;

	public CannonParent Cannons;

	public EZObjectPool bulletPool;

	public AudioSource audios;

	public AudioClip boom;

	private float v;

	private float v2;

	private float d;

	private float e;

	private float s;

	private float s0;

	private float h;

	private float Distance;

	private float angle;

	private Transform myT;

	private float addAngle = 45f;

	private EZObjectPool shipboom;

	private EZObjectPool itemPool;

	private ShipController SC;

	private Rigidbody rigid;

	public Transform[] boats;

	public int HomeN;

	private Vector3 homeVec;

	private GameManager GM;

	private GenPirate GP;

	private bool isBack;

	private RigidbodyConstraints FixRigidInit = (RigidbodyConstraints)84;

	private RigidbodyConstraints DieRigid = RigidbodyConstraints.FreezeRotation;

	private float pirateT;

	private Boat_Setting BS;

	private EffectList BurnFx;

	private float angleLocalT;

	private List<Item> thisReward = new List<Item>();

	private void Start()
	{
	}

	private void Awake()
	{
		GM = GameManager.Instance;
		myT = base.transform;
		bulletPool = GameObject.Find("bullet Enemy").GetComponent<EZObjectPool>();
		shipboom = GameObject.Find("ship boom Effect").GetComponent<EZObjectPool>();
		itemPool = GameObject.Find("Floating Objects").GetComponent<EZObjectPool>();
		GP = GameObject.Find("Generators").GetComponent<GenPirate>();
		myShip = GameObject.Find("MyPos").transform;
		SC = myShip.GetComponent<ShipController>();
		rigid = GetComponent<Rigidbody>();
	}

	private void OnEnable()
	{
		myT.DOKill();
		myT.localScale = Vector3.one;
		myT.DOScale(Vector3.zero, 0.5f).From().SetEase(Ease.OutBack);
		rigid.constraints = FixRigidInit;
		rigid.velocity = Vector3.zero;
		rigid.angularVelocity = Vector3.zero;
		myT.localRotation = Quaternion.identity;
		isArena = SC.isArena;
	}

	private void OnDisable()
	{
		if (SC.NearEnemys.Contains(HEAD))
		{
			SC.NearEnemys.Remove(HEAD);
		}
		if (GP.pirateList.Contains(base.gameObject))
		{
			GP.pirateList.Remove(base.gameObject);
		}
		if (IslandCheck != null && IslandCheck.IslandEnemy.Contains(base.gameObject))
		{
			IslandCheck.IslandEnemy.Remove(base.gameObject);
		}
	}

	public void SetHome(int IslandN)
	{
		HomeN = IslandN;
		homeVec = SC.IslandList[HomeN].position;
	}

	public void init(int tierLevel)
	{
		int num = 1;
		if (GM.CurrentWorld.Equals(WorldType.Dokdo))
		{
			switch (tierLevel)
			{
			case 0:
				num = 1;
				break;
			case 1:
				num = Random.Range(2, 4);
				break;
			case 2:
				num = Random.Range(2, 5);
				break;
			case 3:
				num = Random.Range(3, 6);
				break;
			case 4:
				num = Random.Range(5, 8);
				break;
			case 5:
				num = Random.Range(6, 10);
				break;
			case 6:
				num = 10;
				break;
			}
		}
		else
		{
			switch (tierLevel)
			{
			case 0:
				num = 5;
				break;
			case 1:
				num = 6;
				break;
			case 2:
				num = 7;
				break;
			case 3:
				num = 8;
				break;
			case 4:
				num = 9;
				break;
			case 5:
				num = 10;
				break;
			case 6:
				num = 10;
				break;
			}
		}
		Lv_body = num;
		Lv_Cannon = num * 2;
		Lv_Head = num * 5;
		Lv_Sail = num * 5;
		isDead = false;
		LockOn = false;
		rigid.constraints = FixRigidInit;
		rigid.velocity = Vector3.zero;
		rigid.angularVelocity = Vector3.zero;
		myT.localRotation = Quaternion.identity;
		pirateT = 10f;
		if (!isPirate)
		{
			motorForce = (float)GM.baseSpeed + (float)Lv_Sail * (float)GM.SpeedCoeff;
		}
		else
		{
			motorForce = (float)GM.baseSpeed + (float)Lv_Sail * (float)GM.SpeedCoeff * 3f;
		}
		SetLook();
	}

	public void Hit(int Damage)
	{
		if (!isDead)
		{
			CurrentHP -= Damage;
			BurnFx.init((float)CurrentHP / (float)HP);
			if (CurrentHP <= 0)
			{
				isDead = true;
				Dead();
			}
		}
	}

	public void SetLook()
	{
		if (BS == null)
		{
			BS = GetComponentInChildren<Boat_Setting>();
		}
		boat = boats[Lv_body - 1];
		BS.init(Lv_Sail - 1, Lv_Head - 1, Lv_body - 1);
		BurnFx = GetComponentInChildren<EffectList>();
		steerForce = (float)GM.baseSteer + (float)Lv_Head * (float)GM.steerCoeff;
		rigid.mass = (float)GM.baseMass + (float)Lv_body * (float)GM.massCoeff;
		rigid.angularDrag = GM.rigidAngleDrag;
		float num = 1f;
		num = ((!GM.CurrentWorld.Equals(WorldType.Dokdo)) ? 1.5f : 1f);
		HP = (int)(num * (1f * (float)((int)GM.baseHP + (Lv_body - 1) * (int)GM.hpCoeff) + (float)((int)GM.hpCoeff2 * Lv_body * Lv_body)));
		Atk = (int)(1f * (float)((int)GM.baseAtk + Lv_Cannon * (int)GM.AtkCoeff));
		CurrentHP = HP;
		boat.localRotation = Quaternion.identity;
	}

	private void Dead()
	{
		if (SC.isArena)
		{
			EventManager.TriggerEvent(MyEvent.ArenaKill);
		}
		if (!isPirate)
		{
			if (IslandCheck != null && IslandCheck.IslandEnemy.Contains(base.gameObject))
			{
				IslandCheck.IslandEnemy.Remove(base.gameObject);
			}
		}
		else if (GP != null && GP.pirateList.Contains(base.gameObject))
		{
			GP.pirateList.Remove(base.gameObject);
		}
		if (!SC.isArena)
		{
			Vector3 position = boat.position;
			position.y = 0f;
			shipboom.TryGetNextObject(position, Quaternion.identity);
			for (int i = 0; i < Random.Range(2, 5); i++)
			{
				GameObject obj;
				if (!itemPool.TryGetNextObject(position, Quaternion.identity,
					out obj))
				{
					continue;
				}
				SetRewardItem();
				obj.GetComponent<FloatingObject>().SetItem(thisReward);
			}
		}
		if (SC.NearEnemys.Contains(HEAD))
		{
			SC.NearEnemys.Remove(HEAD);
		}
		rigid.velocity = Vector3.zero;
		rigid.constraints = DieRigid;
		GameManager.Instance.AddStringToMonitor("적 파괴");
		Invoke("off", 3f);
	}

	private void off()
	{
		base.gameObject.SetActive(false);
	}

	public void Shoot(bool isLeft)
	{
		if (Cannons == null)
		{
			SetCannon();
		}
		audios.PlayOneShot(boom);
		if (isLeft)
		{
			for (int i = 0; i < Cannons.cannonsL.Length; i++)
			{
				GameObject obj;
				if (!bulletPool.TryGetNextObject(Cannons.cannonsL[i].position,
					Cannons.cannonsL[i].rotation, out obj))
				{
					continue;
				}
				obj.GetComponent<Rigidbody>().velocity = Vector3.zero;
				obj.GetComponent<Rigidbody>().AddForce(obj.transform.forward * ShootPower);
				obj.GetComponent<Bullets>().atk = Atk;
			}
		}
		else
		{
			for (int j = 0; j < Cannons.cannonsR.Length; j++)
			{
				GameObject obj2;
				if (!bulletPool.TryGetNextObject(Cannons.cannonsR[j].position,
					Cannons.cannonsR[j].rotation, out obj2))
				{
					continue;
				}
				obj2.GetComponent<Rigidbody>().velocity = Vector3.zero;
				obj2.GetComponent<Rigidbody>().AddForce(obj2.transform.forward * ShootPower);
				obj2.GetComponent<Bullets>().atk = Atk;
			}
		}
	}

	private void SetCannon()
	{
		Cannons = GetComponentInChildren<CannonParent>();
	}

	private void FixedUpdate()
	{
		Distance = Vector3.Distance(myT.position, myShip.position);
		if (!SC.isDead)
		{
			if (isPirate)
			{
				if (SC.isArena)
				{
					LockOn = true;
				}
				else
				{
					LockOn = Distance < DetactRange + 50f;
				}
			}
			else
			{
				LockOn = Distance < DetactRange;
			}
		}
		else
		{
			LockOn = false;
		}
		if (!SC.isParking)
		{
			if (SC.isArena)
			{
				if (!isArena)
				{
					base.gameObject.SetActive(false);
				}
			}
			else if (isArena)
			{
				base.gameObject.SetActive(false);
			}
			if (LockOn)
			{
				if (!isPirate)
				{
					if (Vector3.Distance(myT.position, homeVec) > 60f)
					{
						BackHome();
					}
					else if (!isBack)
					{
						MovetoAtk();
					}
					else
					{
						BackHome();
					}
				}
				else
				{
					MovetoAtk();
					if (Distance > 60f && !isArena)
					{
						pirateT -= Time.deltaTime;
						if (pirateT < 0f)
						{
							base.gameObject.SetActive(false);
						}
						v = 0f;
						s = 0f;
					}
				}
			}
			else if (!isPirate)
			{
				if (Vector3.Distance(myT.position, homeVec) > 30f)
				{
					BackHome();
				}
				else
				{
					v = 0f;
					s = 0f;
				}
			}
			else
			{
				pirateT -= Time.deltaTime;
				if (pirateT < 0f)
				{
					base.gameObject.SetActive(false);
				}
				v = 0f;
				s = 0f;
			}
		}
		else
		{
			base.gameObject.SetActive(false);
		}
		e = Mathf.Lerp(e, (0f - v) * 0.01f, 0.5f * Time.deltaTime);
		v2 = Mathf.Lerp(v2, v, 0.5f * Time.deltaTime);
		d = Mathf.Lerp(d, s0 * 0.5f * (v2 / motorForce), Time.deltaTime);
		if (!isDead)
		{
			rigid.AddTorque(Vector3.up * s * 5f);
			rigid.AddForce(myT.forward * v2 * 10f);
			if (!myT.eulerAngles.x.Equals(0f) || !myT.eulerAngles.z.Equals(0f))
			{
				Vector3 eulerAngles = myT.eulerAngles;
				eulerAngles.x = 0f;
				eulerAngles.z = 0f;
				myT.eulerAngles = eulerAngles;
			}
			if (!rigid.constraints.Equals(FixRigidInit))
			{
				rigid.constraints = FixRigidInit;
				myT.localRotation = Quaternion.identity;
			}
		}
		boat.localRotation = Quaternion.Euler(Mathf.Clamp(e * 0.5f, -5f, 5f), 0f, Mathf.Clamp(d, -10f, 10f));
	}

	private void SetRewardItem()
	{
		thisReward.Clear();
		if (Random.value <= 1f)
		{
			Item item = new Item();
			item.type = itemType.coin;
			item.value = Mathf.CeilToInt(0.3f * (float)(Lv_body * 100 + 50 * Lv_body * Lv_body));
			item.value = (int)item.value + (int)((float)(int)item.value * Random.Range(0f, 0.2f));
			thisReward.Add(item);
		}
		if (Random.value < 0.9f)
		{
			Item item2 = new Item();
			item2.type = itemType.wood;
			item2.value = Mathf.CeilToInt(0.006f * (float)(Lv_body * 100 + 50 * Lv_body * Lv_body));
			item2.value = (int)item2.value + (int)((float)(int)item2.value * Random.Range(0f, 0.2f));
			thisReward.Add(item2);
		}
		if (Random.value < 0.5f)
		{
			Item item3 = new Item();
			item3.type = itemType.iron;
			item3.value = Mathf.CeilToInt(0.003f * (float)(Lv_body * 100 + 50 * Lv_body * Lv_body));
			item3.value = (int)item3.value + (int)((float)(int)item3.value * Random.Range(0f, 0.2f));
			thisReward.Add(item3);
		}
		if (Random.value < 0.02f * (float)Lv_body)
		{
			Item item4 = new Item();
			item4.type = itemType.silver;
			item4.value = Lv_body * 2;
			item4.value = (int)item4.value + (int)((float)(int)item4.value * Random.Range(0f, 0.2f));
			thisReward.Add(item4);
		}
		if (Random.value < 0.01f * (float)Lv_body)
		{
			Item item5 = new Item();
			item5.type = itemType.gold;
			item5.value = Lv_body;
			item5.value = (int)item5.value + (int)((float)(int)item5.value * Random.Range(0f, 0.2f));
			thisReward.Add(item5);
		}
		if (Random.value < 0.001f * (float)Lv_body)
		{
			Item item6 = new Item();
			item6.type = itemType.diamond;
			item6.value = 1;
			item6.value = (int)item6.value + (int)((float)(int)item6.value * Random.Range(0f, 0.2f));
			thisReward.Add(item6);
		}
	}

	private void BackHome()
	{
		if (Vector3.Distance(myT.position, homeVec) < 10f)
		{
			isBack = false;
		}
		else
		{
			isBack = true;
		}
		angle = GetAngle2(myT.position, homeVec) - 90f;
		tempobj.rotation = Quaternion.AngleAxis(angle, Vector3.down);
		h = tempobj.eulerAngles.y - myT.eulerAngles.y;
		if (h > 180f)
		{
			s0 = h - 360f;
		}
		else if (h < -180f)
		{
			s0 = 360f + h;
		}
		else
		{
			s0 = h;
		}
		s = steerForce * 0.03f * s0;
		v = motorForce * 2f;
	}

	private void MovetoAtk()
	{
		pirateT = 5f;
		if (Distance > AtkRange)
		{
			v = motorForce;
			angle = GetAngle2(myT.position, myShip.position) - 90f;
		}
		else
		{
			angle = GetAngle2(myT.position, myShip.position) + addAngle;
			v = motorForce;
		}
		angleLocalT -= Time.deltaTime;
		if (angleLocalT <= 0f)
		{
			angleLocalT = 5f;
			if (Random.value > 0.5f)
			{
				addAngle = 45f;
			}
			else
			{
				addAngle = -225f;
			}
		}
		tempobj.rotation = Quaternion.AngleAxis(angle, Vector3.down);
		h = tempobj.eulerAngles.y - myT.eulerAngles.y;
		if (h > 180f)
		{
			s0 = h - 360f;
		}
		else if (h < -180f)
		{
			s0 = 360f + h;
		}
		else
		{
			s0 = h;
		}
		s = steerForce * 0.01f * s0;
	}

	private float GetAngle(Vector3 vStart, Vector3 vEnd)
	{
		Vector3 vector = vEnd - vStart;
		return Mathf.Atan2(vector.y, vector.x) * 57.29578f;
	}

	private float GetAngle2(Vector3 vStart, Vector3 vEnd)
	{
		Vector3 vector = vEnd - vStart;
		return Mathf.Atan2(vector.z, vector.x) * 57.29578f;
	}
}
