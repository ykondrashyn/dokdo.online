using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class GetIsland : MonoBehaviour
{
	public Sprite[] lslandFlag;

	public Sprite[] playerFlag;

	public Transform oriFlag;

	public Transform MyFlag;

	public GameObject[] res;

	public Text IslandName;

	private GameManager GM;

	private ShipController SC;

	public Image fade;

	public Transform Pop;

	public Transform btn;

	private bool isdone;

	public int islandN;

	private List<Item> reward = new List<Item>();

	private void Awake()
	{
		GM = GameManager.Instance;
		SC = GameObject.Find("MyPos").GetComponent<ShipController>();
	}

	public void OnEnable()
	{
		StartCoroutine(Open());
		ShowReward();
	}

	public void OnDisable()
	{
		Time.timeScale = 1f;
	}

	public void GetReward()
	{
		if (!isdone)
		{
			isdone = true;
			reward.Clear();
			int num = GM.SetTier(islandN);
			reward.Add(new Item(itemType.coin, num * 2500 + 300 * num * num));
			switch (num)
			{
			case 0:
				reward.Add(new Item(itemType.iron, 10));
				break;
			case 1:
				reward.Add(new Item(itemType.iron, 20));
				break;
			case 2:
				reward.Add(new Item(itemType.silver, 10));
				break;
			case 3:
				reward.Add(new Item(itemType.silver, 20));
				break;
			case 4:
				reward.Add(new Item(itemType.gold, 10));
				break;
			case 5:
				reward.Add(new Item(itemType.gold, 20));
				break;
			}
			Debug.Log(reward[0].value);
			SC.GetItem(reward.ToArray());
			GM.AddStringToMonitor("장악보상 수령");
			StartCoroutine(Close());
		}
	}

	private IEnumerator Open()
	{
		int n = GM.AmblemNumber(islandN);
		oriFlag.GetComponent<Image>().sprite = lslandFlag[n];
		MyFlag.GetComponent<Image>().sprite = playerFlag[(int)GM.MyAmblemN];
		Pop.localScale = Vector3.zero;
		fade.DOFade(0f, 0f).SetUpdate(true);
		yield return new WaitForSecondsRealtime(0.5f);
		Time.timeScale = 0f;
		fade.DOFade(0.3f, 0.5f).SetUpdate(true);
		Pop.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);
		oriFlag.DOScaleX(1f, 0f).SetUpdate(true);
		MyFlag.DOScaleX(0f, 0f).SetUpdate(true);
		yield return new WaitForSecondsRealtime(0.5f);
		GM.PosiSound(0);
		oriFlag.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5).SetUpdate(true);
		yield return new WaitForSecondsRealtime(0.5f);
		oriFlag.DOScaleX(0f, 0.3f).SetUpdate(true).SetEase(Ease.InCirc);
		MyFlag.DOScaleX(1f, 0.3f).SetUpdate(true).SetDelay(0.3f)
			.SetEase(Ease.OutCirc);
		yield return new WaitForSecondsRealtime(0.6f);
		GM.PosiSound(1);
		MyFlag.DOPunchScale(Vector3.one * 0.2f, 0.5f, 5).SetUpdate(true);
		yield return new WaitForSecondsRealtime(0.2f);
		isdone = false;
	}

	private IEnumerator Close()
	{
		fade.DOFade(0f, 0.3f).SetUpdate(true);
		Pop.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).SetUpdate(true);
		yield return new WaitForSecondsRealtime(0.3f);
		Time.timeScale = 1f;
		if (!GM.isAllIsland && (int)GM.TotalIsland() >= GM.IslandDone.Length)
		{
			GM.isAllIsland = true;
			Object.FindObjectOfType<panel_end>().Show();
		}
		base.gameObject.SetActive(false);
	}

	private void ShowReward()
	{
		IslandName.text = GM.IslandName[islandN];
		for (int i = 0; i < Cost().Length; i++)
		{
			if (Cost()[i] > 0)
			{
				res[i].SetActive(true);
				res[i].GetComponent<Text>().text = Cost()[i].ToString("N0");
			}
			else
			{
				res[i].SetActive(false);
			}
		}
	}

	private int[] Cost()
	{
		int num = GM.SetTier(islandN);
		int[] array = new int[7]
		{
			num * 2500 + 300 * num * num,
			0,
			0,
			0,
			0,
			0,
			0
		};
		switch (num)
		{
		case 0:
			array[3] = 10;
			break;
		case 1:
			array[3] = 20;
			break;
		case 2:
			array[4] = 10;
			break;
		case 3:
			array[4] = 20;
			break;
		case 4:
			array[5] = 10;
			break;
		case 5:
			array[5] = 20;
			break;
		}
		return array;
	}
}
