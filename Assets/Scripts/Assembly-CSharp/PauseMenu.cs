using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
	public Transform[] Pointer;

	public GameObject infoPop;

	public Image flag;

	public Text islandName;

	public GameObject isGot;

	public Sprite[] enemyFlag;

	public Sprite[] myFlag;

	public GameObject[] res;

	public Text[] prices;

	private GameManager GM;

	public Image soundbtn;

	public Sprite SoundOn;

	public Sprite SoundOff;

	public Text myN;

	private UIManager UM;

	[Header("World Map")]
	public GameObject[] Worlds;

	private int currentN;

	private void Awake()
	{
		GM = GameManager.Instance;
		UM = GameObject.Find("UI Manager").GetComponent<UIManager>();
	}

	private void OnEnable()
	{
		UM.LabelUpdate();
		GM.TempMuteBGM(false);
		Time.timeScale = 0f;
		init();
		infoPop.SetActive(false);
		GM.popSound();
		if (GM.FxOn)
		{
			soundbtn.sprite = SoundOn;
		}
		else
		{
			soundbtn.sprite = SoundOff;
		}
		WorldInit();
	}

	private void OnDisable()
	{
		GM.TempMuteBGM(true);
		Time.timeScale = 1f;
		GM.popSound();
	}

	private void init()
	{
		for (int i = 0; i < Pointer.Length; i++)
		{
			Pointer[i].Find("Done").gameObject.SetActive(GM.IslandDone[i]);
			Pointer[i].Find("Done").Find("flag").GetComponent<Image>()
				.sprite = myFlag[(int)GM.MyAmblemN];
			if (GM.TargetIslandNumber.Equals(i))
			{
				Pointer[i].Find("CheckPoint").gameObject.SetActive(true);
			}
			else
			{
				Pointer[i].Find("CheckPoint").gameObject.SetActive(false);
			}
		}
	}

	private void WorldInit()
	{
		for (int i = 0; i < Worlds.Length; i++)
		{
			Worlds[i].SetActive(i.Equals((int)GM.CurrentWorld));
		}
		int num = 0;
		int num2 = 0;
		switch (GM.CurrentWorld)
		{
		case WorldType.Dokdo:
			num = 0;
			num2 = 28;
			break;
		case WorldType.Italy:
			num = 28;
			num2 = 47;
			break;
		}
		int num3 = 0;
		for (int j = num; j < num2; j++)
		{
			if (GM.IslandDone[j])
			{
				num3++;
			}
		}
		myN.text = num3 + "/" + (num2 - num);
	}

	public void Soundinit()
	{
		GM.FxOn = !GM.FxOn;
		if (GM.FxOn)
		{
			soundbtn.sprite = SoundOn;
		}
		else
		{
			soundbtn.sprite = SoundOff;
		}
		GM.SetVol();
		GM.popSound();
	}

	public void OpenInfo(int islandN)
	{
		infoPop.SetActive(true);
		currentN = islandN;
		if (GM.IslandDone[islandN])
		{
			flag.sprite = myFlag[(int)GM.MyAmblemN];
			isGot.SetActive(true);
		}
		else
		{
			int num = GM.AmblemNumber(islandN);
			flag.sprite = enemyFlag[num];
			isGot.SetActive(false);
		}
		ShowPrice(islandN);
		ShowReward(islandN);
	}

	public void SetDes()
	{
		GM.TargetIslandNumber = currentN;
		init();
		infoPop.SetActive(false);
		GM.popSound();
	}

	private void ShowReward(int islandN)
	{
		islandName.text = GM.IslandName[islandN];
		for (int i = 0; i < Cost(islandN).Length; i++)
		{
			if (Cost(islandN)[i] > 0)
			{
				res[i].SetActive(true);
				res[i].GetComponent<Text>().text = Cost(islandN)[i].ToString("N0");
			}
			else
			{
				res[i].SetActive(false);
			}
		}
	}

	private void ShowPrice(int a)
	{
		int num = GM.SetTier(a);
		for (int i = 0; i < prices.Length; i++)
		{
			prices[i].text = GM.MarketPrice[num].p[i].ToString();
		}
	}

	private int[] Cost(int islandN)
	{
		int num = GM.SetTier(islandN);
		int[] array = new int[7];
		if (islandN.Equals(0))
		{
			array[0] = 500;
		}
		else
		{
			array[0] = num * 2500 + 300 * num * num;
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
		}
		return array;
	}
}
