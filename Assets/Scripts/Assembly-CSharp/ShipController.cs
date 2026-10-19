using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using EZObjectPools;
using UnityEngine;
using UnityEngine.Events;

public class ShipController : MonoBehaviour
{
	public Color MyColor;

	public Color[] OtherColor;

	[Header("Near Enemy")]
	public List<Transform> NearEnemys = new List<Transform>();

	public bool isFishing;

	public bool isArena;

	public GameObject[] Worlds;

	[Header("Ship Spec")]
	public float motorForce;

	public float steerForce;

	public float ShootPower;

	public Transform LastIsland;

	public GameObject[] AssiShips;

	public GameObject pickupboat;

	public GameObject pickupFx;

	public MeshRenderer pickAmblem;

	public Transform AssiPos;

	public Transform[] AssiPosList;

	[Header("Status")]
	public float IslandDistance;

	public Transform IslandPointer;

	public float ShowIslandPointLimit;

	public Transform controller;

	public float v;

	public bool isTouch;

	public bool isParking;

	public bool isOuting;

	private float v2;

	private float h;

	private float d;

	private float e;

	private float s0;

	private float s;

	private float s1;

	[Header("Other")]
	public Vector2 tempMousePos;

	public Transform myShip;

	public float angle;

	private float tempx;

	private float tempy;

	public Transform boat;

	public GameObject DeadPanel;

	public bool isDead;

	public WheelCollider Wheel1;

	public WheelCollider Wheel2;

	public WheelCollider Wheel3;

	public WheelCollider Wheel4;

	public Transform tempobj;

	public Transform mousePos;

	public AudioSource moveSound;

	public AudioSource steerSound;

	public AudioSource audios;

	public AudioClip getItem;

	public AudioClip boom;

	private float doubleclickT;

	private bool isDouble;

	public EZObjectPool hudText;

	public EZObjectPool bulletPool;

	private GameManager GM;

	private Transform CamMom;

	public GameObject WaterFx;

	public Rigidbody rigid;

	private EZObjectPool boompool;

	public InfiniteWater IW;

	public Transform[] boats;

	public float OverWorld;

	public GameObject IslandRewardPanel;

	public Transform[] IslandList;

	public GameObject Dead_Arena;

	public ArenaResult AR;

	private EZObjectPool shipboom;

	private IslandMenu IM;

	private Camera cam;

	public GameObject[] SeagullBody;

	public GameObject nessybody;

	public Transform nessybodyT;

	public Transform[] Seagull;

	private UnityAction someListener1;

	private UnityAction someListener2;

	private UnityAction changeHP;

	private UnityAction AssInit;

	private UnityAction pfx;

	private Vector3 tempRot;

	private Boat_Setting BS;

	private int tempHP = 40;

	private float tempCurrnetHP;

	private EffectList BurnFx;

	public float SailRot;

	public GameObject ShowFishing;

	private float tempFish;

	private int FishGet;

	private int SeagullHeal;

	private float DistanceFromZero;

	private float LocalHealT;

	private float itemLocalT;

	private List<Item> remainItems = new List<Item>();

	private void Awake()
	{
		GM = GameManager.Instance;
		CamMom = GameObject.Find("Camera And Light").transform;
		cam = GameObject.Find("Main Camera").GetComponent<Camera>();
		boompool = GameObject.Find("crash Effect").GetComponent<EZObjectPool>();
		shipboom = GameObject.Find("ship boom Effect").GetComponent<EZObjectPool>();
		IM = Object.FindObjectOfType<IslandMenu>();
		IslandRewardPanel.SetActive(false);
		isDead = false;
		DeadPanel.SetActive(false);
		Time.timeScale = 1f;
		someListener1 = Dead;
		someListener2 = SetBody;
		changeHP = BurnEffectCheck;
		AssInit = SetAssi;
		pfx = pickFx;
		for (int i = 0; i < IslandList.Length; i++)
		{
			if (IslandList[i] != null)
			{
				IslandList[i].GetComponent<IslandChecker>().IslandNumber = i;
				IslandList[i].GetComponent<IslandChecker>().Tier = GM.SetTier(i);
			}
		}
	}

	private void OnEnable()
	{
		EventManager.StartListening(MyEvent.Dead, someListener1);
		EventManager.StartListening(MyEvent.Upgrade, someListener2);
		EventManager.StartListening(MyEvent.HpUpdate, changeHP);
		EventManager.StartListening(MyEvent.AssiUpdate, AssInit);
		EventManager.StartListening(MyEvent.pickup, pfx);
	}

	private void OnDisable()
	{
		EventManager.StopListening(MyEvent.Dead, someListener1);
		EventManager.StopListening(MyEvent.Upgrade, someListener2);
		EventManager.StopListening(MyEvent.HpUpdate, changeHP);
		EventManager.StopListening(MyEvent.AssiUpdate, AssInit);
		EventManager.StopListening(MyEvent.pickup, pfx);
		GM.Save();
	}

	private void Start()
	{
		WorldInit();
		NearEnemys.Clear();
		WaterFx.SetActive(true);
		d = 0f;
		e = 0f;
		isDouble = false;
		tempHP = GM.MyHP();
		SetBody();
		Reborn();
		GM.ReCalTime();
		GM.ReCalHP();
		Parking(GM.LastIslandNumber);
		if (!GM.isAllIsland && (int)GM.TotalIsland() >= GM.IslandDone.Length)
		{
			GM.isAllIsland = true;
			Object.FindObjectOfType<panel_end>().Show();
		}
		SetAssi();
	}

	private void WorldInit()
	{
		for (int i = 0; i < Worlds.Length; i++)
		{
			Worlds[i].SetActive(i.Equals((int)GM.CurrentWorld));
		}
		if (!GM.CurrentWorld.Equals(WorldType.Dokdo))
		{
			if ((int)GM.LastIslandNumber <= 27)
			{
				GM.LastIslandNumber = 28;
			}
			if ((int)GM.TargetIslandNumber <= 27)
			{
				GM.TargetIslandNumber = 28;
			}
		}
		else if (!GM.CurrentWorld.Equals(WorldType.Italy))
		{
			if ((int)GM.LastIslandNumber > 27)
			{
				GM.LastIslandNumber = 0;
			}
			if ((int)GM.TargetIslandNumber > 27)
			{
				GM.TargetIslandNumber = 0;
			}
		}
	}

	private void pickFx()
	{
		pickupFx.SetActive(false);
		pickupFx.SetActive(true);
	}

	public void SetAssi()
	{
		for (int i = 0; i < AssiShips.Length; i++)
		{
			AssiShips[i].SetActive((int)GM.AssistantShip_Lv[i] > 0);
		}
		for (int j = 0; j < AssiShips.Length; j++)
		{
			AssiShips[j].transform.position = AssiPosList[j].position;
			AssiShips[j].transform.DORotate(tempRot, 0f);
		}
		for (int k = 0; k < SeagullBody.Length; k++)
		{
			if ((int)GM.Seagull_Lv[k] > 0)
			{
				SeagullBody[k].SetActive(true);
				Seagull[k].DOScale(Vector3.one * (0.2f + 0.036f * (float)(int)GM.Seagull_Lv[k]), 0f);
			}
			else
			{
				SeagullBody[k].SetActive(false);
			}
		}
		if ((int)GM.Nessy_Lv > 0)
		{
			nessybody.SetActive(true);
			nessybodyT.DOScale(Vector3.one * (0.2f + 0.036f * (float)(int)GM.Nessy_Lv), 0f);
		}
		else
		{
			nessybody.SetActive(false);
		}
		Debug.Log("들어와야정상");
		pickupboat.SetActive(GM.pickboat);
		pickupFx.SetActive(false);
		pickAmblem.material = GM.Sail_Flag_Player[(int)GM.MyAmblemN];
		float num = 0f;
		for (int l = 0; l < GM.Seagull_Lv.Length; l++)
		{
			num += (float)(int)GM.Seagull_Lv[l] * 0.003f;
		}
		SeagullHeal = (int)((float)(int)GM.MyHP() * num);
		EventManager.TriggerEvent(MyEvent.Upgrade);
	}

	public void Restart()
	{
		GM.FullHP();
		Reborn();
		Parking(GM.LastIslandNumber);
	}

	private void SetBody()
	{
		tempCurrnetHP = (int)GM.MyCurrnetHP;
		if (BS == null)
		{
			BS = GetComponentInChildren<Boat_Setting>();
		}
		BS.init((int)GM.Level_Sail - 1, (int)GM.Level_Steering - 1, (int)GM.Level_Body - 1);
		BurnFx = GetComponentInChildren<EffectList>();
		boat = boats[(int)GM.Level_Body - 1];
		boat.localRotation = Quaternion.identity;
		steerForce = (float)GM.baseSteer + (float)((int)GM.Level_Body + (int)GM.Level_Steering) * (float)GM.steerCoeff;
		motorForce = (float)GM.baseSpeed + (float)((int)GM.Level_Body + (int)GM.Level_Sail) * (float)GM.SpeedCoeff;
		rigid.mass = (float)GM.baseMass + (float)(int)GM.Level_Body * (float)GM.massCoeff;
		rigid.angularDrag = GM.rigidAngleDrag;
		SetCannon();
		if ((int)GM.MyHP() != tempHP)
		{
			float num = tempCurrnetHP / (float)tempHP;
			GM.MyCurrnetHP = (int)((float)(int)GM.MyHP() * num);
			if (GM.Level_Body.Equals(2))
			{
				AskReview();
			}
		}
		tempHP = GM.MyHP();
		float num2 = 0f;
		for (int i = 0; i < GM.Seagull_Lv.Length; i++)
		{
			num2 += (float)(int)GM.Seagull_Lv[i] * 0.003f;
		}
		SeagullHeal = (int)((float)(int)GM.MyHP() * num2);
	}

	public void AskReview()
	{
		MNPopup popup = new MNPopup("DOKDO", "Online services unavailable.");
		popup.AddAction("Ok", delegate { });
		popup.Show();
	}

	public void GetFish()
	{
		itemLocalT = 0f;
		audios.PlayOneShot(getItem, 0.7f);
		tempFish += 0.01f;
		FishGet = Mathf.CeilToInt((3f + 1.2f * (float)(int)GM.Level_Fishing) * (1f - tempFish) * Random.Range(0.8f, 1.2f));
		if (FishGet < 1)
		{
			FishGet = 1;
		}
		Item item = new Item();
		item.type = itemType.fish;
		item.value = FishGet;
		remainItems.Add(item);
		GM.AddStringToMonitor("낚시 성공");
	}

	private void FixedUpdate()
	{
		if (Mathf.Abs(myShip.position.y) > 20f && isDead)
		{
			myShip.DOMoveY(-20f, 0f);
		}
		if (!isParking && !isDead && isFishing && NearEnemys.Count <= 0)
		{
			ShowFishing.SetActive(true);
		}
		else
		{
			ShowFishing.SetActive(false);
		}
		if (!isParking && !isDead)
		{
			if (!myShip.eulerAngles.x.Equals(0f) || !myShip.eulerAngles.z.Equals(0f))
			{
				Vector3 eulerAngles = myShip.eulerAngles;
				eulerAngles.x = 0f;
				eulerAngles.z = 0f;
				myShip.eulerAngles = eulerAngles;
			}
			if (Input.touchCount < 2)
			{
				if (isTouch)
				{
					v = motorForce;
					if (!isDouble)
					{
						angle = controller.localEulerAngles.z + 90f;
						tempobj.rotation = Quaternion.AngleAxis(angle, Vector3.down);
					}
					h = tempobj.eulerAngles.y - myShip.eulerAngles.y;
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
					mousePos.gameObject.SetActive(true);
					mousePos.position = Input.mousePosition;
				}
				else
				{
					if (!isDouble)
					{
						v = 0f;
						s = 0f;
					}
					else
					{
						h = tempobj.eulerAngles.y - myShip.eulerAngles.y;
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
					mousePos.gameObject.SetActive(false);
				}
			}
			if (doubleclickT > 0f)
			{
				doubleclickT -= Time.deltaTime;
			}
		}
		tempobj.gameObject.SetActive(isDouble);
		v2 = Mathf.Lerp(v2, v, 1f * Time.deltaTime);
		d = Mathf.Lerp(d, s0 * 0.2f * (v2 / motorForce), 0.5f * Time.deltaTime);
		s1 = Mathf.Lerp(s1, s * 5f * (v2 / motorForce), 0.5f * Time.deltaTime);
		SailRot = Mathf.Lerp(SailRot, s * 0.1f * (v2 / motorForce), 2f * Time.deltaTime);
		e = Mathf.Lerp(e, (0f - v) * 0.01f, 0.5f * Time.deltaTime);
		rigid.AddTorque(Vector3.up * s * 5f);
		rigid.AddForce(myShip.forward * v2 * 10f);
		boat.localRotation = Quaternion.Euler(Mathf.Clamp(e * 0.5f, -5f, 5f), 0f, Mathf.Clamp(d, -10f, 10f));
		if (!isDead)
		{
			moveSound.volume = rigid.velocity.magnitude * 0.5f;
			steerSound.volume = rigid.angularVelocity.magnitude * 4f;
		}
		else
		{
			moveSound.volume = 0f;
			steerSound.volume = 0f;
		}
		if (remainItems.Count > 0)
		{
			itemLocalT -= Time.deltaTime;
			if (itemLocalT <= 0f)
			{
				ShowGotItem();
				itemLocalT = 0.7f;
			}
		}
		GetIslandDistance();
		if (isParking)
		{
			if ((int)GM.MyCurrnetHP < (int)GM.MyHP())
			{
				LocalHealT += Time.deltaTime;
				if (LocalHealT > 2f)
				{
					GM.AddHP(GM.HealPoint());
					IM.SetRepairTime();
					LocalHealT = 0f;
				}
			}
			else
			{
				GM.MyCurrnetHP = GM.MyHP();
			}
		}
		else if ((int)GM.MyCurrnetHP < (int)GM.MyHP())
		{
			LocalHealT += Time.deltaTime;
			if (LocalHealT > 30f)
			{
				GM.AddHP(SeagullHeal);
				LocalHealT = 0f;
			}
		}
		else
		{
			GM.MyCurrnetHP = GM.MyHP();
		}
		DistanceFromZero = Vector3.Distance(Vector3.zero, myShip.position);
		if (DistanceFromZero > OverWorld)
		{
			Vector3 position = myShip.position;
			position.x = (0f - position.x) * 0.9f;
			position.z = (0f - position.z) * 0.9f;
			myShip.position = position;
			Debug.Log("맵 넘어감");
		}
		if (DistanceFromZero < 300f)
		{
			GM.CurrentTier = 1;
		}
		else if (DistanceFromZero < 600f)
		{
			GM.CurrentTier = 2;
		}
		else if (DistanceFromZero < 900f)
		{
			GM.CurrentTier = 3;
		}
		else if (DistanceFromZero < 1200f)
		{
			GM.CurrentTier = 4;
		}
		else if (DistanceFromZero < 1500f)
		{
			GM.CurrentTier = 5;
		}
		else if (DistanceFromZero < 1800f)
		{
			GM.CurrentTier = 6;
		}
		else
		{
			GM.CurrentTier = 7;
		}
	}

	private void OnCollisionEnter(Collision other)
	{
		if (other.transform.CompareTag("Obstacle"))
		{
		}
		if (other.transform.CompareTag("Enemy"))
		{
			ContactPoint contactPoint = other.contacts[0];
			Quaternion rot = Quaternion.FromToRotation(Vector3.up, contactPoint.normal);
			Vector3 point = contactPoint.point;
			boompool.TryGetNextObject(contactPoint.point, rot);
			other.transform.GetComponent<Enemy_Ship>().Hit(GM.Level_Body);
			Hit(other.transform.GetComponent<Enemy_Ship>().Lv_body);
		}
	}

	public void Hit(int damage)
	{
		if (!isDead)
		{
			GM.AddHP(-damage);
		}
	}

	private void BurnEffectCheck()
	{
		if (BurnFx == null)
		{
			BurnFx = GetComponentInChildren<EffectList>();
		}
		if (BurnFx != null)
		{
			BurnFx.init((float)(int)GM.MyCurrnetHP / (float)(int)GM.MyHP());
		}
	}

	public void Reborn()
	{
		GM.CurrentArenaKill = 0;
		AR.AD.Over();
		GM.isRevived = true;
		isDead = false;
		rigid.velocity = Vector3.zero;
		myShip.DOMoveY(0f, 0f);
		rigid.constraints = (RigidbodyConstraints)84;
		Vector3 eulerAngles = myShip.eulerAngles;
		eulerAngles.x = 0f;
		eulerAngles.z = 0f;
		myShip.eulerAngles = eulerAngles;
		GM.AddStringToMonitor("재생성 성공");
	}

	public void Dead()
	{
		GameManager gM = GM;
		gM.DeadNum = (int)gM.DeadNum + 1;
		isDead = true;
		Vector3 position = boat.position;
		position.y = 0f;
		shipboom.TryGetNextObject(position, Quaternion.identity);
		v = 0f;
		s = 0f;
		rigid.velocity = Vector3.zero;
		rigid.constraints = RigidbodyConstraints.FreezeRotation;
		if (isArena)
		{
			isArena = false;
			Dead_Arena.SetActive(true);
		}
		else
		{
			DeadPanel.SetActive(true);
		}
		GM.Save();
	}

	public void Parking(int Num)
	{
		tempFish = 0f;
		GM.isRevived = false;
		EventManager.TriggerEvent(MyEvent.isParking);
		GM.LastIslandNumber = Num;
		Transform transform = IslandList[Num];
		isParking = true;
		isTouch = false;
		isDouble = false;
		rigid.velocity = Vector3.zero;
		rigid.angularVelocity = Vector3.zero;
		boat.localRotation = Quaternion.Euler(0f, 0f, 0f);
		v = 0f;
		v2 = 0f;
		s = 0f;
		s0 = 0f;
		s1 = 0f;
		e = 0f;
		d = 0f;
		mousePos.gameObject.SetActive(false);
		WaterFx.SetActive(false);
		WaterFx.SetActive(true);
		GM.AddStringToMonitor("주차중");
		SetPosMove(transform.Find("ParkingPos"));
		GM.Save();
	}

	public void SetPosMove(Transform target)
	{
		Vector3 position = target.position;
		tempRot = target.eulerAngles;
		float duration = 5f;
		myShip.DORotate(tempRot, 0f);
		myShip.DOMoveX(position.x - 5f, 0f);
		myShip.DOMoveZ(position.z + 5f, 0f);
		myShip.DOMoveX(position.x, duration).SetDelay(0.2f);
		myShip.DOMoveZ(position.z, duration).SetDelay(0.2f);
		position.x += 1.5f;
		position.z -= 5f;
		CamMom.position = position;
		IW.initPosition(CamMom);
		position.y = 0f;
		AssiPos.position = position;
		SetAssi();
	}

	public void SetPosMove2(Transform target)
	{
		cam.DOOrthoSize(10f, 0f);
		cam.DOOrthoSize(40f, 2f);
		rigid.velocity = Vector3.zero;
		rigid.angularVelocity = Vector3.zero;
		Vector3 position = target.position;
		tempRot = target.eulerAngles;
		myShip.DORotate(tempRot, 0f);
		myShip.DOMoveX(position.x, 0f);
		myShip.DOMoveZ(position.z, 0f);
		position.x += 1.5f;
		position.y = 0f;
		AssiPos.position = position;
		SetAssi();
	}

	private void GetIslandDistance()
	{
		IslandDistance = Vector3.Distance(myShip.position, IslandList[(int)GM.TargetIslandNumber].transform.position);
		ShowMiniMapPointer(GM.TargetIslandNumber);
	}

	private void ShowMiniMapPointer(int N)
	{
		if (IslandDistance > ShowIslandPointLimit)
		{
			IslandPointer.gameObject.SetActive(true);
			Vector3 eulerAngles = IslandPointer.eulerAngles;
			eulerAngles.z = GetAngle2(myShip.position, IslandList[N].transform.position);
			IslandPointer.eulerAngles = eulerAngles;
		}
		else
		{
			IslandPointer.gameObject.SetActive(false);
		}
	}

	public void Shoot(bool isLeft)
	{
	}

	public void ShootEach(Vector3 pos, Quaternion rot)
	{
		audios.PlayOneShot(boom);
		GameObject obj;
		if (!bulletPool.TryGetNextObject(pos, rot, out obj))
		{
			return;
		}
		obj.GetComponent<Bullets>().atk = GM.MyAtk();
		obj.GetComponent<Rigidbody>().velocity = Vector3.zero;
		obj.GetComponent<Rigidbody>().AddForce(obj.transform.forward * ShootPower);
	}

	private void SetCannon()
	{
	}

	public void onTouch()
	{
		if (Input.touchCount < 2 && !isParking)
		{
			isTouch = true;
			isDouble = false;
			if (doubleclickT > 0f)
			{
				GM.AddStringToMonitor("자동항해 시작");
				isDouble = true;
				tempx = (float)Screen.width * 0.5f;
				tempy = (float)Screen.height * 0.5f;
				tempMousePos = new Vector2(Input.mousePosition.x - tempx, Input.mousePosition.y - tempy);
				angle = GetAngle(Vector2.zero, tempMousePos) - 90f;
				tempobj.rotation = Quaternion.AngleAxis(angle, Vector3.down);
			}
			doubleclickT = 0.5f;
		}
	}

	public void outTouch()
	{
		isTouch = false;
	}

	public void GetItem(Item[] items)
	{
		audios.PlayOneShot(getItem);
		for (int i = 0; i < items.Length; i++)
		{
			remainItems.Add(items[i]);
		}
	}

	public void GetRewardIsland(int islandN)
	{
		GM.GetIsland(islandN);
		IslandRewardPanel.GetComponent<GetIsland>().islandN = islandN;
		IslandRewardPanel.SetActive(false);
		IslandRewardPanel.SetActive(true);
	}

	private void ShowGotItem()
	{
		itemType type = remainItems[0].type;
		int num = remainItems[0].value;
		GameObject obj = null;
		if (hudText.TryGetNextObject(Vector3.zero, Quaternion.identity, out obj))
		{
			obj.SetActive(true);
			obj.GetComponent<HudText>().SetTypeValue(num, type);
			obj.transform.SetSiblingIndex(-1);
		}
		GM.AddResource(type, num);
		remainItems.RemoveAt(0);
	}

	private IEnumerator getItemCo(Item[] items)
	{
		audios.PlayOneShot(getItem);
		for (int i = 0; i < items.Length; i++)
		{
			itemType type = items[i].type;
			int value = items[i].value;
			GameObject go = null;
			if (hudText.TryGetNextObject(Vector3.zero, Quaternion.identity,
				out go))
			{
				go.SetActive(true);
				go.GetComponent<HudText>().SetTypeValue(value, type);
				go.transform.SetSiblingIndex(-1);
			}
			GM.AddResource(type, value);
			yield return new WaitForSeconds(0.5f);
		}
	}

	private float GetAngle(Vector2 vStart, Vector2 vEnd)
	{
		Vector2 vector = vEnd - vStart;
		return Mathf.Atan2(vector.y, vector.x) * 57.29578f;
	}

	private float GetAngle2(Vector3 vStart, Vector3 vEnd)
	{
		Vector3 vector = vEnd - vStart;
		return Mathf.Atan2(vector.z, vector.x) * 57.29578f;
	}

	private void LateUpdate()
	{
		if (Input.GetKeyUp(KeyCode.Escape))
		{
			MNPopup mNPopup = new MNPopup("DOKDO", "Game Over?");
			mNPopup.AddAction("No", delegate
			{
				Debug.Log("close");
			});
			mNPopup.AddAction("Yes", delegate
			{
				GM.Save();
				Application.Quit();
			});
			mNPopup.AddDismissListener(delegate
			{
				Debug.Log("dismiss listener");
			});
			mNPopup.Show();
		}
	}

	private void OnApplicationPause(bool pauseStatus)
	{
		if (pauseStatus)
		{
			GM.Save();
		}
		else
		{
			Invoke("ReCalTime", 1f);
		}
	}

	private void OnApplicationQuit()
	{
		GM.Save();
	}

	public void ReCalTime()
	{
		GM.ReCalTime();
		if (isParking)
		{
			GM.ReCalHP();
		}
	}
}
