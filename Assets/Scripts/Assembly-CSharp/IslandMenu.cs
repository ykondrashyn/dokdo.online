using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class IslandMenu : MonoBehaviour
{
	private ShipController SC;

	public RectTransform myMenu;

	public RectTransform InMenu;

	private bool isdone;

	private bool isUP;

	private GameManager GM;

	public Transform PortPos;

	public Transform LandPos;

	private Camera cam;

	private Transform CamMom;

	public Image ray;

	public Transform followTarget;

	public UIManager UM;

	public GameObject[] appearMenu;

	public GameObject[] RepairMenu;

	public Text[] RepairTimer;

	public GameObject marketAdsBtn;

	public Text Marketimer;

	public Text IslandName;

	public GameObject market;

	public GameObject garage;

	public Transform marketpop;

	public GameObject tuto;

	public GameObject tuto1;

	public GameObject select;

	public Image FlagBtn;

	public Sprite[] flags;

	public PauseMenu PM;

	public Text revival;

	public Toggle[] tapMenu;

	public GameObject[] menus;

	public GameObject DailyReward;

	public GameObject[] DailyDone;

	public Transform dayPop;

	public GameObject newAssi;

	private UnityAction someListener;

	private Vector3 tempPos;

	private float timer;

	private float bodylvzoom = 10f;

	private int b;

	private bool isFirst = true;

	public Vector3 comTargetPOs;

	public Transform assiCamPos;

	private bool daydone;

	private void Awake()
	{
		cam = GameObject.Find("Main Camera").GetComponent<Camera>();
		CamMom = GameObject.Find("Camera And Light").transform;
		GM = GameManager.Instance;
		SC = GameObject.Find("MyPos").GetComponent<ShipController>();
		isdone = false;
		isUP = false;
		SC.isOuting = false;
		ray.enabled = false;
		for (int i = 0; i < appearMenu.Length; i++)
		{
			appearMenu[i].SetActive(false);
		}
		myMenu.DOAnchorPosX(0f, 0f);
		myMenu.DOAnchorPosY(-1400f, 0f);
		myMenu.gameObject.SetActive(false);
		someListener = ZoomUpgrade;
		b = GM.Level_Body;
		float num = (float)(Screen.height / Screen.width) * 1f;
		if (num >= 1.8f)
		{
			InMenu.localScale = Vector3.one * 0.9f;
			InMenu.DOLocalMoveY(-1570f, 0f);
			Debug.Log("비율 조정");
		}
		else
		{
			InMenu.localScale = Vector3.one;
			InMenu.DOLocalMoveY(-1600f, 0f);
		}
	}

	private void OnEnable()
	{
		EventManager.StartListening(MyEvent.Upgrade, someListener);
		for (int i = 0; i < RepairMenu.Length; i++)
		{
			RepairMenu[i].SetActive(false);
		}
		if (!GM.isTuto)
		{
			GM.isTuto = true;
			select.SetActive(true);
			tuto.SetActive(true);
			tuto1.SetActive(true);
		}
		else
		{
			select.SetActive(false);
			tuto.SetActive(false);
		}
		for (int j = 0; j < menus.Length; j++)
		{
			menus[j].SetActive(false);
		}
	}

	public void ChangeAmblem(int n)
	{
		GM.MyAmblemN = n;
		EventManager.TriggerEvent(MyEvent.Upgrade);
		EventManager.TriggerEvent(MyEvent.AssiUpdate);
	}

	public void SetRepairTime()
	{
		timer = (float)((int)GM.MyHP() - (int)GM.MyCurrnetHP) * 2f / (float)(int)GM.HealPoint();
	}

	private void OnDisable()
	{
		EventManager.StopListening(MyEvent.Upgrade, someListener);
	}

	public void popMarket()
	{
		marketpop.DOKill();
		marketpop.DOLocalMoveY(0f, 0f);
		marketpop.DOLocalMoveY(36f, 0.1f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutCubic);
	}

	private void Update()
	{
		if (SC.isParking && !isUP)
		{
			isUP = true;
			UpMenu();
		}
		if (!SC.isParking)
		{
			return;
		}
		FlagBtn.sprite = flags[(int)GM.MyAmblemN];
		revival.text = "X " + GM.Revival;
		if ((int)GM.MyHP() > (int)GM.MyCurrnetHP)
		{
			if (timer > 0f)
			{
				timer -= Time.deltaTime;
			}
			else
			{
				SetRepairTime();
			}
		}
		else
		{
			timer = 0f;
			for (int i = 0; i < RepairMenu.Length; i++)
			{
				RepairMenu[i].SetActive(false);
			}
		}
		for (int j = 0; j < RepairMenu.Length; j++)
		{
			RepairTimer[j].text = showTime(timer);
		}
		Marketimer.text = showTime(GM.ResetMarketTime);
		newAssi.SetActive(!GM.pickboat);
	}

	private string showTime(float t)
	{
		int num = Mathf.FloorToInt(t / 60f);
		int num2 = Mathf.FloorToInt(t - (float)(num * 60));
		return string.Format("{0:0}:{1:00}", num, num2);
	}

	private void ZoomUpgrade()
	{
		if (b != (int)GM.Level_Body)
		{
			cam.DOKill();
			cam.DOOrthoSize(bodylvzoom + (float)(int)GameManager.Instance.Level_Body * 0.5f, 1f);
			CamMom.DOKill();
			CamMom.DOMove(tempPos + new Vector3(0.15f * (float)(int)GM.Level_Body, 0f, -0.4f * (float)(int)GM.Level_Body), 1f);
			SetRepairTime();
		}
	}

	public void UpMenu()
	{
		if (!isFirst)
		{
			if (!GM.noAds)
			{
				ThirdPartyManager.instance.ShowInterstitialAds();
			}
		}
		else
		{
			isFirst = false;
		}
		myMenu.gameObject.SetActive(true);
		UM.LabelUpdate();
		ray.enabled = true;
		float duration = 0.5f;
		IslandName.text = GM.IslandName[(int)GM.LastIslandNumber];
		for (int i = 0; i < tapMenu.Length; i++)
		{
			tapMenu[i].isOn = false;
		}
		tapMenu[0].isOn = true;
		myMenu.DOAnchorPosX(0f, 0f);
		myMenu.DOAnchorPosY(-1400f, 0f);
		myMenu.DOAnchorPosY(0f, duration).SetDelay(1f).OnComplete(delegate
		{
			Invoke("openother", 1f);
		});
		cam.orthographicSize = 15f;
		tempPos = CamMom.position + new Vector3(0f, 0f, -5f);
		comTargetPOs = tempPos + new Vector3(0.15f * (float)(int)GM.Level_Body, 0f, -0.4f * (float)(int)GM.Level_Body);
		CamMom.DOKill();
		CamMom.DOMove(comTargetPOs, 1f);
		cam.DOOrthoSize(6f, 0f);
		cam.DOOrthoSize(bodylvzoom + (float)(int)GameManager.Instance.Level_Body * 0.5f, 3f);
	}

	public void CamToAssi(bool yes)
	{
		if (yes)
		{
			CamMom.DOKill();
			CamMom.DOMove(assiCamPos.position, 0.5f);
		}
		else if (SC.isParking)
		{
			CamMom.DOKill();
			CamMom.DOMove(comTargetPOs, 0.5f);
		}
	}

	private void openother()
	{
		for (int i = 0; i < appearMenu.Length; i++)
		{
			appearMenu[i].SetActive(true);
			appearMenu[i].transform.DOKill();
			appearMenu[i].transform.localScale = Vector3.zero;
			appearMenu[i].transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
		}
		if ((int)GM.MyCurrnetHP >= (int)GM.MyHP())
		{
			for (int j = 0; j < RepairMenu.Length; j++)
			{
				RepairMenu[j].SetActive(false);
			}
		}
		else
		{
			for (int k = 0; k < RepairMenu.Length; k++)
			{
				RepairMenu[k].SetActive(true);
			}
		}
		dailyCheck();
	}

	private void closeother()
	{
		int i;
		for (i = 0; i < appearMenu.Length; i++)
		{
			appearMenu[i].transform.DOKill();
			appearMenu[i].transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack).OnComplete(delegate
			{
				appearMenu[i].SetActive(true);
			});
		}
	}

	public void DownMenu()
	{
		GM.Save();
		if (!isdone && SC.isParking)
		{
			isdone = true;
			StartCoroutine(OutIsland());
		}
	}

	private IEnumerator OutIsland()
	{
		closeother();
		float downT = 0.5f;
		myMenu.DOAnchorPosY(-1400f, downT).SetEase(Ease.InBack);
		SC.transform.DOKill();
		CamMom.DOKill();
		float camT = 0.5f;
		CamMom.DOMove(followTarget.position, camT);
		yield return new WaitForSeconds(camT);
		SC.isOuting = true;
		yield return new WaitForSeconds(0.5f);
		SC.v = SC.motorForce * 2f;
		cam.DOOrthoSize(40f, 2f).SetDelay(1f);
		yield return new WaitForSeconds(3f);
		ray.enabled = false;
		SC.isOuting = false;
		SC.isParking = false;
		UM.LabelUpdate();
		UM.Damaged();
		isdone = false;
		isUP = false;
		myMenu.gameObject.SetActive(false);
	}

	private void dailyCheck()
	{
		if (GM.isNewDay())
		{
			DailyReward.SetActive(true);
			for (int i = 0; i < DailyDone.Length; i++)
			{
				DailyDone[i].SetActive(i < (int)GM.currentday - 1);
				DailyDone[i].transform.Find("Image").gameObject.SetActive(i < (int)GM.currentday - 1);
			}
			daydone = false;
		}
		else
		{
			DailyReward.SetActive(false);
		}
	}

	public void GetDailyReward()
	{
		if (!daydone)
		{
			daydone = true;
			DailyDone[(int)GM.currentday - 1].SetActive(true);
			DailyDone[(int)GM.currentday - 1].transform.Find("Image").gameObject.SetActive(true);
			GM.DailyReward(GM.currentday);
			dayPop.DOScale(Vector3.zero, 0.5f).SetDelay(0.5f).SetEase(Ease.InBack)
				.OnComplete(delegate
				{
					DailyReward.SetActive(false);
				});
		}
	}
}
