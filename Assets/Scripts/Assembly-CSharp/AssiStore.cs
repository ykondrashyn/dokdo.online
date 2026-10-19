using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class AssiStore : MonoBehaviour
{
	public Text costLabel;

	public Text costLabel2;

	public Text costLabel3;

	public GameObject MAX;

	public GameObject MAX2;

	public GameObject MAX3;

	public GameObject picksold;

	private GameManager GM;

	private UIManager UM;

	private UnityAction AssInit;

	private int cost = 1000000;

	private int cost2 = 1000000;

	private int cost3 = 5000000;

	private int n;

	private int n2;

	private int n3;

	private void Awake()
	{
		GM = GameManager.Instance;
		UM = GameObject.Find("UI Manager").GetComponent<UIManager>();
		AssInit = init;
	}

	private void OnEnable()
	{
		init();
		EventManager.StartListening(MyEvent.AssiUpdate, AssInit);
	}

	private void OnDisable()
	{
		EventManager.StopListening(MyEvent.AssiUpdate, AssInit);
	}

	private void init()
	{
		n = 0;
		for (int i = 0; i < GM.AssistantShip_Lv.Length; i++)
		{
			if ((int)GM.AssistantShip_Lv[i] > 0)
			{
				n++;
			}
		}
		switch (n)
		{
		case 0:
			cost = 100000;
			break;
		case 1:
			cost = 500000;
			break;
		case 2:
			cost = 1000000;
			break;
		}
		costLabel.text = cost.ToString("N0");
		if (n >= 3)
		{
			MAX.SetActive(true);
		}
		else
		{
			MAX.SetActive(false);
		}
		n2 = 0;
		for (int j = 0; j < GM.Seagull_Lv.Length; j++)
		{
			if ((int)GM.Seagull_Lv[j] > 0)
			{
				n2++;
			}
		}
		switch (n2)
		{
		case 0:
			cost2 = 500000;
			break;
		case 1:
			cost2 = 1000000;
			break;
		}
		costLabel2.text = cost2.ToString("N0");
		if (n2 >= 2)
		{
			MAX2.SetActive(true);
		}
		else
		{
			MAX2.SetActive(false);
		}
		n3 = 0;
		if ((int)GM.Nessy_Lv > 0)
		{
			n3 = 1;
		}
		costLabel3.text = cost3.ToString("N0");
		if (n3 >= 1)
		{
			MAX3.SetActive(true);
		}
		else
		{
			MAX3.SetActive(false);
		}
		picksold.SetActive(GM.pickboat);
	}

	public void BuyPickupBoat(bool isIAP)
	{
		Debug.Log(GM.pickboat);
		if (GM.pickboat)
		{
			return;
		}
		if (!isIAP)
		{
			if (GM.isEnoughMoney(5000000))
			{
				GM.pickboat = true;
				EventManager.TriggerEvent(MyEvent.AssiUpdate);
				GM.Save();
			}
		}
		else
		{
			ThirdPartyManager.instance.BuyProduct("dokdo_pick");
		}
	}

	public void BuyAssistant()
	{
		if (n < 3)
		{
			if (GM.isEnoughMoney(cost))
			{
				if ((int)GM.AssistantShip_Lv[0] <= 0)
				{
					GM.AssistantShip_Lv[0] = 1;
				}
				else if ((int)GM.AssistantShip_Lv[1] <= 0)
				{
					GM.AssistantShip_Lv[1] = 1;
				}
				else if ((int)GM.AssistantShip_Lv[2] <= 0)
				{
					GM.AssistantShip_Lv[2] = 1;
				}
				UM.LabelUpdate();
				EventManager.TriggerEvent(MyEvent.AssiUpdate);
			}
		}
		else
		{
			Debug.Log("최대 조수");
		}
	}

	public void BuySeagull()
	{
		if (n2 < 2)
		{
			if (GM.isEnoughMoney(cost2))
			{
				if ((int)GM.Seagull_Lv[0] <= 0)
				{
					GM.Seagull_Lv[0] = 1;
				}
				else if ((int)GM.Seagull_Lv[1] <= 0)
				{
					GM.Seagull_Lv[1] = 1;
				}
				UM.LabelUpdate();
				EventManager.TriggerEvent(MyEvent.AssiUpdate);
			}
		}
		else
		{
			Debug.Log("최대 조수");
		}
	}

	public void BuyNessy()
	{
		if (n3 < 1)
		{
			if (GM.isEnoughMoney(cost3))
			{
				GM.Nessy_Lv = 1;
				UM.LabelUpdate();
				EventManager.TriggerEvent(MyEvent.AssiUpdate);
			}
		}
		else
		{
			Debug.Log("최대 조수");
		}
	}
}
