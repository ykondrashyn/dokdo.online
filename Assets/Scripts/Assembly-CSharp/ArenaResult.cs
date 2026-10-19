using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ArenaResult : MonoBehaviour
{
	public Text myrank;

	public Text myname;

	public Text myScore;

	public Text[] ranks;

	public Text[] names;

	public Text[] scores;

	public Text toppercent;

	public GameObject ranking;

	public GameObject logInPop;

	public Text currentKill;

	public Text bestKill;

	public Text rewardLabel;

	private GameManager GM;

	public GameObject offTarget;

	private ShipController SC;

	public GameObject hud;

	public Image BG;

	public Transform popup;

	public GameObject loading;

	public ArenaDoor AD;

	public GameObject[] title;

	private bool isdone;

	private string rankBoard;

	private int bonus = 1;

	private void Awake()
	{
		GM = GameManager.Instance;
		SC = GameObject.Find("MyPos").GetComponent<ShipController>();
	}

	private void OnDisable()
	{
	}

	private void OnEnable()
	{
		BG.DOKill();
		popup.DOKill();
		BG.DOFade(0f, 0f);
		BG.DOFade(0.5f, 2f);
		loading.SetActive(true);
		popup.localScale = Vector3.zero;
		popup.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetDelay(2f);
		hud.SetActive(false);
		rankBoard = string.Empty;
		for (int i = 0; i < title.Length; i++)
		{
			title[i].SetActive(false);
		}
		if (GM.CurrentWorld.Equals(WorldType.Dokdo))
		{
			title[0].SetActive(true);
			rankBoard = "dokdo_arena";
			if ((int)GM.CurrentArenaKill > (int)GM.BestArenaKill1)
			{
				GM.BestArenaKill1 = GM.CurrentArenaKill;
			}
			bestKill.text = string.Concat("BEST ", GM.BestArenaKill1, " KILL");
			bonus = 1;
		}
		else
		{
			title[1].SetActive(true);
			rankBoard = "dokdo_Colloseo";
			if ((int)GM.CurrentArenaKill > (int)GM.BestColloseoKill)
			{
				GM.BestColloseoKill = GM.CurrentArenaKill;
			}
			bestKill.text = string.Concat("BEST ", GM.BestColloseoKill, " KILL");
			bonus = 2;
		}
		GM.Save();
		currentKill.text = string.Concat(GM.CurrentArenaKill, " KILL");
		rewardLabel.text = reward().ToString("N0");
		isdone = false;
		if (rankBoard != string.Empty)
		{
			StartCoroutine(load());
		}
	}

	private int reward()
	{
		int num = (int)GM.CurrentArenaKill * ((int)GM.CurrentArenaKill - 2) * 20 + 50;
		if ((int)GM.CurrentArenaKill <= 0)
		{
			num = 0;
		}
		return num * bonus;
	}

	public void GetReward()
	{
		if (!isdone)
		{
			isdone = true;
			GM.AddMoney(reward());
			offTarget.SetActive(false);
			SC.Restart();
		}
	}

	private IEnumerator load()
	{
		ranking.SetActive(false);
		logInPop.SetActive(false);
		yield return new WaitForSeconds(0.1f);
		for (int i = 0; i < ranks.Length; i++)
		{
			ranks[i].text = "-";
			names[i].text = "-";
			scores[i].text = "-";
		}
		myrank.text = "-";
		myname.text = "LOCAL";
		myScore.text = GM.CurrentArenaKill + " KILL";
		toppercent.text = "OFFLINE";
		ranking.SetActive(true);
		loading.SetActive(false);
	}

	public void logIn()
	{
		MNPopup popup = new MNPopup("SYSTEM", "Online services unavailable.");
		popup.AddAction("Ok", delegate { });
		popup.Show();
	}

	private void Update()
	{
	}
}
