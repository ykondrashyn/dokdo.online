using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ShipUpgradeBtn : MonoBehaviour
{
	public LevelType UpgradeType;

	public Text Title;

	public Image icon;

	public Image EleType;

	public Text LevelLabel;

	public Image MaxGage;

	public Image UpGage;

	public Image CurrentGage;

	public GameObject MAX;

	private int level;

	public Sprite[] icons;

	public GameObject[] res;

	private GameManager GM;

	private int maxLevel;

	private IslandMenu IM;

	private void Awake()
	{
		GM = GameManager.Instance;
		IM = GameObject.Find("IslandMenu").GetComponent<IslandMenu>();
		MAX.SetActive(false);
	}

	private void OnEnable()
	{
		if (UpgradeType.Equals(LevelType.Body))
		{
			GM.popSound();
		}
		init();
	}

	private void Start()
	{
		icon.sprite = icons[(int)UpgradeType];
		switch (UpgradeType)
		{
		case LevelType.Body:
			Title.text = LocalizeManager.Instance.translate("body");
			break;
		case LevelType.Sail:
			Title.text = LocalizeManager.Instance.translate("sail");
			break;
		case LevelType.Steering:
			Title.text = LocalizeManager.Instance.translate("steer");
			break;
		case LevelType.Cannon:
			Title.text = LocalizeManager.Instance.translate("cannon");
			break;
		case LevelType.Fishing:
			Title.text = LocalizeManager.Instance.translate("fishing");
			break;
		case LevelType.Storage:
			break;
		}
	}

	public void init()
	{
		switch (UpgradeType)
		{
		case LevelType.Body:
			level = GM.Level_Body;
			break;
		case LevelType.Sail:
			level = GM.Level_Sail;
			break;
		case LevelType.Steering:
			level = GM.Level_Steering;
			break;
		case LevelType.Storage:
			level = GM.Level_Storage;
			break;
		case LevelType.Cannon:
			level = GM.Level_Cannon;
			break;
		case LevelType.Fishing:
			level = GM.Level_Fishing;
			break;
		}
		if (UpgradeType.Equals(LevelType.Body))
		{
			MaxGage.fillAmount = 1f;
			UpGage.fillAmount = (float)(level + 1) / 10f;
			CurrentGage.fillAmount = (float)level / 10f;
			maxLevel = 10;
		}
		else
		{
			MaxGage.fillAmount = (float)(int)GM.Level_Body / 10f;
			UpGage.fillAmount = (float)(level + 1) / 50f;
			CurrentGage.fillAmount = (float)level / 50f;
			maxLevel = (int)GM.Level_Body * 5;
		}
		LevelLabel.text = "lv." + level;
		if (level >= maxLevel)
		{
			MAX.SetActive(true);
			UpGage.fillAmount = (float)level / 50f;
			return;
		}
		MAX.SetActive(false);
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

	private string ShowCost(int[] cost)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < cost.Length; i++)
		{
			if (!cost[i].Equals(0))
			{
				stringBuilder.Append((itemType)i);
				stringBuilder.Append(":");
				stringBuilder.Append(cost[i]);
				stringBuilder.Append(" ");
			}
		}
		return stringBuilder.ToString();
	}

	private int[] Cost()
	{
		int[] array = new int[7];
		switch (UpgradeType)
		{
		case LevelType.Body:
			if (level <= 4)
			{
				if (level <= 2)
				{
					array[0] = 100 * (level + 1 + (level + 1) * (level + 1));
				}
				else
				{
					array[0] = 300 * (level + 1 + (level + 1) * (level + 1));
				}
			}
			else
			{
				array[5] = (level - 4) * (level - 4) * 10;
			}
			array[2] = 20 * (level * level) + 10 * (level - 1);
			break;
		case LevelType.Sail:
			if (level <= 19)
			{
				array[0] = 10 * (level + 1);
			}
			else if (level <= 39)
			{
				array[4] = 3 * (level - 19);
			}
			else if (level <= 48)
			{
				array[5] = 5 * (level - 38);
			}
			else
			{
				array[5] = 100;
			}
			if (level <= 9)
			{
				array[1] = 5 * level;
			}
			else
			{
				array[3] = 3 * (level - 9);
			}
			break;
		case LevelType.Steering:
			if (level <= 19)
			{
				array[1] = level + 1;
				array[2] = level + 1;
				break;
			}
			if (level <= 48)
			{
				array[3] = level - 19;
			}
			else
			{
				array[3] = 100;
			}
			if (level <= 39)
			{
				array[4] = level - 19;
			}
			else if (level <= 48)
			{
				array[5] = level - 29;
			}
			else
			{
				array[6] = 5;
			}
			break;
		case LevelType.Cannon:
			if (level <= 9)
			{
				array[0] = 10 * (level + 1);
				array[2] = level + 1;
				break;
			}
			if (level <= 29)
			{
				array[3] = level - 9;
			}
			else
			{
				array[5] = level - 29;
			}
			if (level <= 44)
			{
				array[4] = level - 9;
			}
			else
			{
				array[6] = level - 44;
			}
			break;
		case LevelType.Fishing:
			array[1] = (int)((float)(4 * (level + 1)) + (float)((level + 1) * (level + 1)) * 0.3f);
			if (level <= 19)
			{
				array[2] = level + 1;
			}
			else if (level <= 29)
			{
				array[0] = 2000 * (level - 19) * (level - 19);
			}
			else if (level <= 39)
			{
				array[0] = 5000 * (level - 22);
			}
			else
			{
				array[0] = 5000 + 10000 * (level - 31);
			}
			break;
		}
		return array;
	}

	public void Upgrade()
	{
		if (level < maxLevel)
		{
			if (GM.isEnoughResource(Cost()))
			{
				switch (UpgradeType)
				{
				case LevelType.Body:
				{
					GameManager gM6 = GM;
					gM6.Level_Body = (int)gM6.Level_Body + 1;
					break;
				}
				case LevelType.Sail:
				{
					GameManager gM5 = GM;
					gM5.Level_Sail = (int)gM5.Level_Sail + 1;
					break;
				}
				case LevelType.Steering:
				{
					GameManager gM4 = GM;
					gM4.Level_Steering = (int)gM4.Level_Steering + 1;
					break;
				}
				case LevelType.Storage:
				{
					GameManager gM3 = GM;
					gM3.Level_Storage = (int)gM3.Level_Storage + 1;
					break;
				}
				case LevelType.Cannon:
				{
					GameManager gM2 = GM;
					gM2.Level_Cannon = (int)gM2.Level_Cannon + 1;
					break;
				}
				case LevelType.Fishing:
				{
					GameManager gM = GM;
					gM.Level_Fishing = (int)gM.Level_Fishing + 1;
					break;
				}
				}
				ShipUpgradeBtn[] array = Object.FindObjectsOfType(typeof(ShipUpgradeBtn)) as ShipUpgradeBtn[];
				ShipUpgradeBtn[] array2 = array;
				foreach (ShipUpgradeBtn shipUpgradeBtn in array2)
				{
					shipUpgradeBtn.init();
				}
				EventManager.TriggerEvent(MyEvent.Upgrade);
			}
			else
			{
				IM.popMarket();
			}
		}
		else
		{
			Debug.Log("최대 레벨 도달");
		}
	}
}
