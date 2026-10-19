using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class AssiMenu : MonoBehaviour
{
	public int AssiNum;

	public Text NumLabel;

	public Text LevelLabel;

	public Text costLabel;

	public Image UpGage;

	public Image CurrentGage;

	public GameObject MAX;

	private GameManager GM;

	private UIManager UM;

	public GameObject OpenMarket;

	public bool isSeagull;

	public bool isNessy;

	private UnityAction AssInit;

	private int maxLevel = 50;

	private int level;

	private int cost;

	private void Awake()
	{
		GM = GameManager.Instance;
		UM = GameObject.Find("UI Manager").GetComponent<UIManager>();
		MAX.SetActive(false);
		if (!isNessy)
		{
			NumLabel.text = "- " + (AssiNum + 1);
		}
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

	public void init()
	{
		if (!isNessy)
		{
			if (!isSeagull)
			{
				level = GM.AssistantShip_Lv[AssiNum];
			}
			else
			{
				level = GM.Seagull_Lv[AssiNum];
			}
		}
		else
		{
			level = GM.Nessy_Lv;
		}
		if (level <= 0)
		{
			OpenMarket.SetActive(true);
			return;
		}
		OpenMarket.SetActive(false);
		UpGage.fillAmount = (float)(level + 1) / 50f;
		CurrentGage.fillAmount = (float)level / 50f;
		LevelLabel.text = "lv." + level;
		if (level >= maxLevel)
		{
			MAX.SetActive(true);
			UpGage.fillAmount = (float)level / 50f;
		}
		else
		{
			MAX.SetActive(false);
			cost = 10000 * (level + 1);
			costLabel.text = cost.ToString("N0");
		}
	}

	public void Upgrade()
	{
		if (level < maxLevel)
		{
			if (!GM.isEnoughMoney(cost))
			{
				return;
			}
			if (!isNessy)
			{
				if (!isSeagull)
				{
					ObscuredInt[] assistantShip_Lv = GM.AssistantShip_Lv;
					int assiNum = AssiNum;
					assistantShip_Lv[assiNum] = (int)assistantShip_Lv[assiNum] + 1;
				}
				else
				{
					ObscuredInt[] seagull_Lv = GM.Seagull_Lv;
					int assiNum2 = AssiNum;
					seagull_Lv[assiNum2] = (int)seagull_Lv[assiNum2] + 1;
				}
			}
			else
			{
				GameManager gM = GM;
				gM.Nessy_Lv = (int)gM.Nessy_Lv + 1;
			}
			UM.LabelUpdate();
			EventManager.TriggerEvent(MyEvent.AssiUpdate);
		}
		else
		{
			Debug.Log("최대 레벨 도달");
		}
	}
}
