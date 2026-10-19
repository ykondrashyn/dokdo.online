using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class IslandMarket : MonoBehaviour
{
	public GameObject[] BuyMax;

	public GameObject[] SellMax;

	public Text[] remainLabel;

	public Text[] BuyLabel;

	public Text[] SellLabel;

	private GameManager GM;

	private int Tier;

	private UnityAction remarket;

	private bool isPress;

	private WaitForSeconds ms = new WaitForSeconds(0.1f);

	private Coroutine goCo;

	private void Awake()
	{
		GM = GameManager.Instance;
		remarket = init;
	}

	private void OnEnable()
	{
		EventManager.StartListening(MyEvent.ReMarket, remarket);
		GM.popSound();
		init();
	}

	private void OnDisable()
	{
		EventManager.StopListening(MyEvent.ReMarket, remarket);
	}

	public void init()
	{
		Tier = GM.SetTier(GM.LastIslandNumber);
		for (int i = 0; i < remainLabel.Length; i++)
		{
			remainLabel[i].text = GM.IslandGoods[(int)GM.LastIslandNumber].goods[i].value.ToString();
			BuyLabel[i].text = GM.MarketPrice[Tier].p[i].ToString();
			SellLabel[i].text = ((int)(0.8f * (float)(int)GM.MarketPrice[Tier].p[i])/*cast due to .constrained prefix*/).ToString();
			if ((int)GM.IslandGoods[(int)GM.LastIslandNumber].goods[i].value <= 0)
			{
				BuyMax[i].SetActive(true);
			}
			else
			{
				BuyMax[i].SetActive(false);
			}
			if ((int)GM.myResource[i + 1].value <= 0)
			{
				SellMax[i].SetActive(true);
			}
			else
			{
				SellMax[i].SetActive(false);
			}
		}
	}

	public void Buy(int type)
	{
		if ((int)GM.IslandGoods[(int)GM.LastIslandNumber].goods[type].value > 0)
		{
			if (GM.isEnoughMoney(GM.MarketPrice[Tier].p[type]))
			{
				IdAndInt obj = GM.IslandGoods[(int)GM.LastIslandNumber].goods[type];
				obj.value = (int)obj.value - 1;
				GM.AddResource((itemType)(type + 1), 1);
			}
		}
		else
		{
			Debug.Log("섬의 물건이 부족합니다.");
		}
		init();
	}

	public void PressBuy(int type)
	{
		isPress = true;
		goCo = StartCoroutine(pressBuyBtn(type));
	}

	public void PressSell(int type)
	{
		isPress = true;
		goCo = StartCoroutine(pressSellBtn(type));
	}

	public void Up()
	{
		isPress = false;
		StopCoroutine(goCo);
	}

	private IEnumerator pressBuyBtn(int type)
	{
		Buy(type);
		yield return new WaitForSeconds(0.5f);
		while (isPress)
		{
			Buy(type);
			yield return ms;
		}
	}

	private IEnumerator pressSellBtn(int type)
	{
		Sell(type, 1);
		int p = 1;
		float m = 1f;
		yield return new WaitForSeconds(0.5f);
		while (isPress)
		{
			m += 0.2f;
			p = (int)m;
			Sell(type, p);
			yield return ms;
		}
	}

	public void Sell(int type, int n)
	{
		if ((int)GM.myResource[type + 1].value >= n)
		{
			GM.AddResource((itemType)(type + 1), -1 * n);
			IdAndInt obj = GM.IslandGoods[(int)GM.LastIslandNumber].goods[type];
			obj.value = (int)obj.value + n;
			GM.AddMoney((int)((float)(int)GM.MarketPrice[Tier].p[type] * 0.9f) * n);
		}
		else
		{
			GM.AddStringToMonitor("소유한 물건이 부족합니다.");
		}
		init();
	}
}
