using DG.Tweening;
using EZObjectPools;
using UnityEngine;
using UnityEngine.Events;

public class Assistant : MonoBehaviour
{
	[Header("CurrentData")]
	public int Atk;

	public IslandChecker IslandCheck;

	public int AssistantNum;

	[Header("BaseData")]
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

	private int Lv_body;

	private int Lv_Sail;

	private int Lv_Head;

	private int Lv_Cannon;

	private Transform myT;

	private ShipController SC;

	private Rigidbody rigid;

	public Transform[] boats;

	private GameManager GM;

	private RigidbodyConstraints FixRigidInit = (RigidbodyConstraints)84;

	private UnityAction AssInit;

	private int max = 50;

	private Boat_Setting BS;

	private Vector3 ranpos;

	private float r = 30f;

	private void Awake()
	{
		GM = GameManager.Instance;
		myT = base.transform;
		myShip = GameObject.Find("MyPos").transform;
		SC = myShip.GetComponent<ShipController>();
		bulletPool = SC.bulletPool;
		rigid = GetComponent<Rigidbody>();
		AssInit = init;
	}

	private void OnEnable()
	{
		myT.DOKill();
		rigid.constraints = FixRigidInit;
		rigid.velocity = Vector3.zero;
		rigid.angularVelocity = Vector3.zero;
		myT.localRotation = Quaternion.identity;
		init();
		EventManager.StartListening(MyEvent.AssiUpdate, AssInit);
	}

	private void OnDisable()
	{
		EventManager.StopListening(MyEvent.AssiUpdate, AssInit);
	}

	private void Start()
	{
		init();
	}

	public void init()
	{
		int num = 0;
		if (GM.AssistantShip_Lv.Length >= AssistantNum - 1)
		{
			num = GM.AssistantShip_Lv[AssistantNum - 1];
		}
		if (num <= 0)
		{
			base.gameObject.SetActive(false);
			return;
		}
		if (num > max)
		{
			num = max;
		}
		Lv_body = Mathf.Clamp((int)((float)num * 0.2f) + 1, 1, 10);
		Lv_Cannon = num;
		Lv_Head = num;
		Lv_Sail = num;
		rigid.constraints = FixRigidInit;
		rigid.velocity = Vector3.zero;
		rigid.angularVelocity = Vector3.zero;
		myT.localRotation = Quaternion.identity;
		Vector3 eulerAngles = myT.eulerAngles;
		eulerAngles.y = 135f;
		myT.eulerAngles = eulerAngles;
		motorForce = (float)GM.baseSpeed + (float)Lv_Sail * (float)GM.SpeedCoeff * 1f;
		SetLook();
	}

	public void SetLook()
	{
		if (BS == null)
		{
			BS = GetComponentInChildren<Boat_Setting>();
		}
		boat = boats[Lv_body - 1];
		boat.localRotation = Quaternion.identity;
		BS.init(Lv_Sail - 1, Lv_Head - 1, Lv_body - 1);
		steerForce = ((float)GM.baseSteer + (float)Lv_Head * (float)GM.steerCoeff) * 0.5f;
		rigid.mass = ((float)GM.baseMass + (float)Lv_body * (float)GM.massCoeff) * 0.5f;
		rigid.angularDrag = GM.rigidAngleDrag;
		Atk = (int)(0.5f * (float)((int)GM.baseAtk + Lv_Cannon * (int)GM.AtkCoeff));
		boat.localRotation = Quaternion.identity;
	}

	private void off()
	{
		base.gameObject.SetActive(false);
	}

	public void Shoot(bool isLeft)
	{
		if (SC.isDead)
		{
			return;
		}
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
		if (!SC.isParking && !SC.isDead)
		{
			if (Distance > 15f)
			{
				if (Distance > 100f)
				{
					do
					{
						ranpos = myShip.position + new Vector3(Random.Range(0f - r, r), 0f, Random.Range(0f - r, r));
					}
					while (Vector3.Distance(myShip.position, ranpos) < r);
					myT.position = ranpos;
				}
				else if (Distance > 40f)
				{
					v = motorForce * 2f;
				}
				else
				{
					v = motorForce * 1f;
				}
				s = steerForce * 0.01f * s0;
			}
			else if (Distance > 20f)
			{
				v = motorForce * 0.5f;
				s = steerForce * 0.005f * s0;
			}
			else if (Distance > 5f)
			{
				v = motorForce * 0.3f;
				s = steerForce * 0.003f * s0;
			}
			else
			{
				v = 0f;
				s = 0f;
			}
			if (Distance < 20f)
			{
				tempobj.rotation = SC.tempobj.rotation;
			}
			else
			{
				angle = GetAngle2(myT.position, myShip.position) - 90f;
				tempobj.rotation = Quaternion.AngleAxis(angle, Vector3.down);
			}
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
			e = Mathf.Lerp(e, (0f - v) * 0.01f, 0.5f * Time.deltaTime);
			v2 = Mathf.Lerp(v2, v, 0.5f * Time.deltaTime);
			d = Mathf.Lerp(d, s0 * 0.5f * (v2 / motorForce), Time.deltaTime);
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
			boat.localRotation = Quaternion.Euler(Mathf.Clamp(e * 0.5f, -5f, 5f), 0f, Mathf.Clamp(d, -10f, 10f));
		}
		else
		{
			rigid.velocity = Vector3.zero;
			rigid.angularVelocity = Vector3.zero;
			v = 0f;
			s = 0f;
		}
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
