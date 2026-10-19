using DG.Tweening;
using Dokdo;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
	[Header("HpBar")]
	public CanvasGroup HpCG;

	public Image HpGage;

	public Image HpGageBack;

	[Header("MiniMap")]
	public CanvasGroup MinimapCG;

	public ControllerView CV;

	[Header("Resources")]
	public Text[] Labels;

	public Image[] ResourceIcon;

	private GameManager GM;

	private ShipController SC;

	private float tempa1;

	public CanvasScaler canvasScale;

	private UnityAction damage;

	private UnityAction heal;

	private UnityAction labelup;

	private UnityAction ReReward;

	public CanvasGroup cgLabel;

	public GameObject hiddenpop;

	public Text reRewardDesc;

	public Text ReRewardCoin;

	private int tempReward;

	public GameObject ReRewardPanel;

	public GameObject Loading;

	private void Awake()
	{
		GM = GameManager.Instance;
		SC = GameObject.Find("MyPos").GetComponent<ShipController>();
		damage = Damaged;
		heal = HpGageUpdate;
		labelup = LabelUpdate;
		float num = (float)Screen.height * 1f / (float)Screen.width;
		if (num >= 2f)
		{
			canvasScale.matchWidthOrHeight = 0.4f;
		}
		else
		{
			canvasScale.matchWidthOrHeight = 1f;
		}
	}

	private void OnEnable()
	{
		EventManager.StartListening(MyEvent.Attacked, damage);
		EventManager.StartListening(MyEvent.HpUpdate, heal);
		EventManager.StartListening(MyEvent.ResourceUpdate, labelup);
		EventManager.StartListening(MyEvent.ReReward, ShowReReward);
	}

	private void OnDisable()
	{
		EventManager.StopListening(MyEvent.Attacked, damage);
		EventManager.StopListening(MyEvent.HpUpdate, heal);
		EventManager.StopListening(MyEvent.ResourceUpdate, labelup);
		EventManager.StopListening(MyEvent.ReReward, ShowReReward);
	}

	private void ShowReReward()
	{
		tempReward = 0;
		int num = (int)((float)(int)GM.ReRewardT / 60f);
		tempReward = num * (int)GM.Level_Fishing * 3;
		reRewardDesc.text = string.Concat(LocalizeManager.Instance.translate("fishing"), " <color=#FFC300FF>Lv.", GM.Level_Fishing, "</color> x ", tTOhm(num));
		ReRewardCoin.text = tempReward.ToString("N0");
		ReRewardPanel.SetActive(true);
	}

	public void GetReReward()
	{
		GM.AddMoney(tempReward);
		ReRewardPanel.SetActive(false);
	}

	private string tTOhm(int t)
	{
		string empty = string.Empty;
		if (t >= 60)
		{
			int num = Mathf.FloorToInt((float)t / 60f);
			int num2 = t - num * 60;
			return num + "h " + num2 + "min";
		}
		return t + "min";
	}

	private void Start()
	{
		Invoke("init", 5f);
	}

	private void init()
	{
		LabelUpdate();
		HpCG.alpha = 0f;
		HpGage.fillAmount = (float)(int)GM.MyCurrnetHP / (float)(int)GM.MyHP();
		HpGageBack.fillAmount = (float)(int)GM.MyCurrnetHP / (float)(int)GM.MyHP();
	}

	public void LabelUpdate()
	{
		for (int i = 0; i < ResourceIcon.Length; i++)
		{
			ResourceIcon[i].DOKill();
			Labels[i].DOKill();
			if (Labels[i].text != GM.myResource[i].value.ToString("N0"))
			{
				Labels[i].DOColor(Color.yellow, 0f);
				Labels[i].rectTransform.DOAnchorPosY(2f, 0.2f);
			}
			Labels[i].rectTransform.DOAnchorPosY(-2f, 0.3f).SetDelay(0.2f);
			Labels[i].DOColor(Color.white, 0.3f).SetDelay(0.2f);
			Labels[i].text = GM.myResource[i].value.ToString("N0");
			ResourceIcon[i].DOFade(1f, 0.5f).SetUpdate(true);
			Labels[i].DOFade(1f, 0.5f).SetUpdate(true);
			if (!SC.isParking && !SC.isDead)
			{
				ResourceIcon[i].DOFade(0f, 2f).SetDelay(1f);
				Labels[i].DOFade(0f, 2f).SetDelay(1f);
			}
		}
	}

	public void ChangeWorld()
	{
		if (GM.CurrentWorld.Equals(WorldType.Dokdo))
		{
			GM.WorldLast1 = GM.LastIslandNumber;
			GM.LastIslandNumber = GM.WorldLast2;
			GM.CurrentWorld = WorldType.Italy;
		}
		else if (GM.CurrentWorld.Equals(WorldType.Italy))
		{
			GM.WorldLast2 = GM.LastIslandNumber;
			GM.LastIslandNumber = GM.WorldLast1;
			GM.CurrentWorld = WorldType.Dokdo;
		}
		Loading.SetActive(true);
	}

	public void Reload()
	{
		SceneManager.LoadScene(SceneIds.Main);
	}

	public void Damaged()
	{
		HpCG.DOKill();
		HpGageBack.DOKill();
		HpCG.alpha = 0.7f;
		HpCG.DOFade(0f, 1f).SetDelay(3f);
		HpGage.fillAmount = (float)(int)GM.MyCurrnetHP / (float)(int)GM.MyHP();
		HpGageBack.DOFillAmount(HpGage.fillAmount, 1f).SetDelay(0.5f);
	}

	public void HpGageUpdate()
	{
		if (!SC.isParking)
		{
			HpCG.DOKill();
			HpGageBack.DOKill();
			HpCG.alpha = 0.7f;
			HpCG.DOFade(0f, 1f).SetDelay(3f);
		}
		HpGage.DOFillAmount((float)(int)GM.MyCurrnetHP / (float)(int)GM.MyHP(), 1f).SetDelay(0.5f);
		HpGageBack.fillAmount = (float)(int)GM.MyCurrnetHP / (float)(int)GM.MyHP();
	}

	public void Hidden()
	{
		if (!GM.isHidden)
		{
			GM.isHidden = true;
			GM.AddMoney(1000);
			hiddenpop.SetActive(true);
		}
	}

	private void FixedUpdate()
	{
		cgLabel.alpha = 1f;
		if (SC.isParking)
		{
			HpCG.alpha = 0.7f;
		}
		if (SC.isParking || SC.isDead)
		{
			if (tempa1 > 0f)
			{
				tempa1 = Mathf.Lerp(MinimapCG.alpha, 0f, 2f * Time.deltaTime);
			}
		}
		else if (tempa1 < 0.75f)
		{
			tempa1 = Mathf.Lerp(MinimapCG.alpha, 0.75f, 2f * Time.deltaTime);
		}
		MinimapCG.alpha = tempa1;
	}
}
