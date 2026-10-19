using System;
using System.Linq;
using CodeStage.AntiCheat.Detectors;
using CodeStage.AntiCheat.ObscuredTypes;
using Dokdo;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance;

	private static int COUNT;

	private int index;

	[Header("Status")]
	public ObscuredInt MyCurrnetHP;

	public ObscuredInt MyAmblemN;

	public bool isHidden;

	public bool isTuto;

	public bool noAds = true;

	public ObscuredInt[] AssistantShip_Lv;

	public ObscuredInt[] Seagull_Lv;

	public ObscuredInt Nessy_Lv;

	public WorldType CurrentWorld;

	[Header("Data")]
	public ObscuredInt Revival;

	public ObscuredInt Level_Body = 1;

	public ObscuredInt Level_Sail = 1;

	public ObscuredInt Level_Steering = 1;

	public ObscuredInt Level_Storage = 1;

	public ObscuredInt Level_Cannon = 1;

	public ObscuredInt Level_Fishing = 1;

	public bool isAllIsland;

	public ObscuredInt ReRewardT;

	public ObscuredInt DeadNum;

	public ObscuredInt CurrentTier;

	public ObscuredInt CurrentArenaKill;

	public ObscuredInt BestArenaKill1;

	public ObscuredInt BestColloseoKill;

	[Header("Balance")]
	public ObscuredFloat baseSpeed;

	public ObscuredFloat SpeedCoeff;

	public ObscuredFloat baseSteer;

	public ObscuredFloat steerCoeff;

	public ObscuredFloat baseMass;

	public ObscuredFloat massCoeff;

	public ObscuredFloat rigidAngleDrag;

	public ObscuredInt baseHP;

	public ObscuredInt hpCoeff;

	public ObscuredInt hpCoeff2;

	public ObscuredInt baseAtk;

	public ObscuredInt AtkCoeff;

	public string coinDay;

	[Header("Resources")]
	public Item[] myResource;

	[Header("Other")]
	public Sprite[] ResourceType;

	public AudioSource audios;

	public AudioClip coin1;

	public AudioClip coin2;

	public AudioClip Nocoin;

	public AudioClip pop;

	public AudioClip neg;

	public AudioClip positive;

	public AudioClip positive2;

	public AudioClip positive3;

	public bool FxOn;

	public bool BgmOn;

	public AudioMixer Mixer;

	[Header("Island Stautus")]
	public IslandChecker CurrentIsland;

	public ObscuredInt LastIslandNumber;

	public ObscuredInt WorldLast1;

	public ObscuredInt WorldLast2;

	public ObscuredInt TargetIslandNumber;

	public ObscuredFloat ResetMarketTime;

	public ObscuredFloat ResetMarketPeriod;

	public ObscuredFloat MarketAdsTime;

	public ObscuredFloat freeCoinTime;

	public ObscuredFloat freeCoinResetPeriod;

	public bool[] IslandDone;

	public IslandGood[] IslandGoods;

	public IslandGood[] IslandGoodsDefault;

	public bool isNearIsland;

	public Material[] Sail_Flag_Player;

	public Material[] Sail_Flag_Enemy;

	public prices[] MarketPrice;

	public string[] IslandName;

	public bool isRevived;

	public bool pickboat = true;

	public ObscuredInt currentday;

	private int[] green0 = new int[13]
	{
		0, 1, 4, 6, 8, 11, 20, 21, 27, 28,
		33, 38, 43
	};

	private int[] blue1 = new int[8] { 2, 9, 18, 24, 29, 34, 39, 44 };

	private int[] desert2 = new int[10] { 3, 7, 12, 13, 19, 26, 30, 35, 40, 45 };

	private int[] ice3 = new int[9] { 15, 16, 22, 23, 25, 31, 36, 41, 46 };

	private int[] pink4 = new int[7] { 5, 10, 14, 17, 32, 37, 42 };

	public ObscuredInt MyHP()
	{
		int num = (int)((float)((int)baseHP + ((int)Level_Body - 1) * (int)hpCoeff + (int)hpCoeff2 * (int)Level_Body * (int)Level_Body) * (1f + (float)(int)Nessy_Lv * 0.02f));
		return num;
	}

	public ObscuredInt MyAtk()
	{
		int num = (int)baseAtk + (int)Level_Cannon * (int)AtkCoeff;
		return num;
	}

	private void Awake()
	{
		index = COUNT;
		COUNT++;
		if (index != 0)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		Instance = this;
		Screen.sleepTimeout = -1;
		BalData();
		Load();
	}

	private void Start()
	{
		if ((int)MyCurrnetHP < 0)
		{
			MyCurrnetHP = MyHP();
		}
		SpeedHackDetector.StartDetection(OnSpeedHackDetected);
	}

	private void OnSpeedHackDetected()
	{
		MNPopup mNPopup = new MNPopup("SYSTEM", "Cheating Detected");
		mNPopup.AddAction("Ok", delegate
		{
			SceneManager.LoadScene(SceneIds.Intro);
		});
		mNPopup.AddDismissListener(delegate
		{
			Debug.Log("dismiss listener");
		});
		mNPopup.Show();
	}

	public void FreeCoinAds()
	{
		int value = 250 * (int)Level_Body;
		freeCoinTime = freeCoinResetPeriod;
		AddMoney(value);
	}

	public void AdsBoxReward()
	{
		popSound();
		AddMoney(500 * (int)Level_Body);
		UnityEngine.Object.FindObjectOfType<Panel_AskAds>().Close();
	}

	public void AdsMarketReset()
	{
		popSound();
		MarketAdsTime = ResetMarketPeriod;
		ResetMarket();
	}

	public void SetVol()
	{
		if (FxOn)
		{
			Mixer.SetFloat("FxVol", 0f);
		}
		else
		{
			Mixer.SetFloat("FxVol", -80f);
		}
		if (BgmOn)
		{
			Mixer.SetFloat("BgmVol", 0f);
		}
		else
		{
			Mixer.SetFloat("BgmVol", -80f);
		}
	}

	public void TempMuteBGM(bool ison)
	{
		if (ison)
		{
			if (FxOn)
			{
				Mixer.SetFloat("FxVol", 0f);
			}
		}
		else if (FxOn)
		{
			Mixer.SetFloat("FxVol", -80f);
		}
	}

	public void hp30()
	{
		popSound();
		MyCurrnetHP = (int)(0.3f * (float)(int)MyHP());
		EventManager.TriggerEvent(MyEvent.HpUpdate);
	}

	public void AddResource(itemType type, int value)
	{
		Item obj = myResource[(int)type];
		obj.value = (int)obj.value + value;
		EventManager.TriggerEvent(MyEvent.ResourceUpdate);
	}

	public bool isEnoughMoney(int value)
	{
		bool flag = false;
		if ((int)myResource[0].value >= value)
		{
			flag = true;
			Item obj = myResource[0];
			obj.value = (int)obj.value - value;
			audios.PlayOneShot(coin2);
			AddStringToMonitor("구매 성공");
			EventManager.TriggerEvent(MyEvent.ResourceUpdate);
		}
		else
		{
			flag = false;
			EventManager.TriggerEvent(MyEvent.NoMoney);
			AddStringToMonitor("코인이 부족합니다.");
			audios.PlayOneShot(Nocoin);
		}
		return flag;
	}

	public bool isEnoughResource(int[] res)
	{
		bool flag = false;
		bool[] array = new bool[7];
		int num = 0;
		for (int i = 0; i < 7; i++)
		{
			array[i] = (int)myResource[i].value >= res[i];
			if (!array[i])
			{
				num++;
			}
		}
		if (num.Equals(0))
		{
			for (int j = 0; j < 7; j++)
			{
				Item obj = myResource[j];
				obj.value = (int)obj.value - res[j];
			}
			audios.PlayOneShot(coin2);
			AddStringToMonitor("구매 성공");
			EventManager.TriggerEvent(MyEvent.ResourceUpdate);
			flag = true;
		}
		else
		{
			flag = false;
			AddStringToMonitor("자원이 부족합니다.");
			audios.PlayOneShot(Nocoin);
		}
		return flag;
	}

	public void AddMoney(int Value)
	{
		CoinSound();
		Item obj = myResource[0];
		obj.value = (int)obj.value + Value;
		EventManager.TriggerEvent(MyEvent.ResourceUpdate);
	}

	public void CoinSound()
	{
		audios.PlayOneShot(coin1);
	}

	public void AddRevival(int value)
	{
		popSound();
		Revival = (int)Revival + value;
	}

	public void AddHP(int Value)
	{
		MyCurrnetHP = (int)MyCurrnetHP + Value;
		if (Value < 0)
		{
			EventManager.TriggerEvent(MyEvent.Attacked);
		}
		EventManager.TriggerEvent(MyEvent.HpUpdate);
		if ((int)MyCurrnetHP <= 0)
		{
			MyCurrnetHP = 0;
			EventManager.TriggerEvent(MyEvent.Dead);
		}
		if ((int)MyCurrnetHP > (int)MyHP())
		{
			MyCurrnetHP = MyHP();
		}
	}

	public void FullHP()
	{
		popSound();
		MyCurrnetHP = MyHP();
		EventManager.TriggerEvent(MyEvent.HpUpdate);
	}

	public void GetIsland(int Num)
	{
		IslandDone[Num] = true;
		if (TargetIslandNumber.Equals(Num) && (int)TargetIslandNumber < 27)
		{
			TargetIslandNumber = 0;
			while (IslandDone[(int)TargetIslandNumber])
			{
				TargetIslandNumber = (int)TargetIslandNumber + 1;
				if ((int)TargetIslandNumber > 27)
				{
					TargetIslandNumber = 0;
					break;
				}
			}
		}
		AddStringToMonitor(Num + "번 섬을 정복했다");
		Save();
	}

	public ObscuredInt TotalIsland()
	{
		int num = 0;
		for (int i = 0; i < IslandDone.Length; i++)
		{
			if (IslandDone[i])
			{
				num++;
			}
		}
		return num;
	}

	public ObscuredInt TotalDokdoIsland()
	{
		int num = 0;
		for (int i = 0; i < 28; i++)
		{
			if (IslandDone[i])
			{
				num++;
			}
		}
		return num;
	}

	public void NegSound()
	{
		audios.PlayOneShot(neg);
	}

	public void PosiSound(int n)
	{
		switch (n)
		{
		case 0:
			audios.PlayOneShot(positive);
			break;
		case 1:
			audios.PlayOneShot(positive2);
			break;
		case 2:
			audios.PlayOneShot(positive3);
			break;
		}
	}

	public void popSound()
	{
		audios.PlayOneShot(pop);
	}

	public void AddStringToMonitor(string content)
	{
	}

	public ObscuredInt HealPoint()
	{
		int num = 0;
		num = (int)((float)(int)MyHP() / (float)(30 * (int)Level_Body));
		return num;
	}

	private void ResetMarket()
	{
		ResetMarketTime = (float)ResetMarketPeriod * 2f;
		for (int i = 0; i < IslandGoods.Length; i++)
		{
			for (int j = 0; j < 6; j++)
			{
				IslandGoods[i].goods[j].value = IslandGoodsDefault[i].goods[j].value;
			}
		}
		AddStringToMonitor("마켓초기화");
		EventManager.TriggerEvent(MyEvent.ReMarket);
	}

	private void Update()
	{
		if ((float)MarketAdsTime > 0f)
		{
			MarketAdsTime = (float)MarketAdsTime - Time.deltaTime;
		}
		if ((float)ResetMarketTime < 0f)
		{
			ResetMarket();
		}
		else
		{
			ResetMarketTime = (float)ResetMarketTime - Time.deltaTime;
		}
		if ((float)freeCoinTime > 0f)
		{
			freeCoinTime = (float)freeCoinTime - Time.deltaTime;
		}
	}

	[ContextMenu("reset")]
	public void Reset()
	{
		SaveRuntime.Store.Reset();
		SaveDataV1 fresh = CreateFreshSaveData();
		ApplySaveData(fresh);
		SaveRuntime.Current = fresh;
		Debug.Log("Reset");
	}

	public void Save()
	{
		try
		{
			SaveDataV1 data = CreateSaveData(SaveRuntime.Clock.UtcNow);
			SaveRuntime.Store.Save(data);
			SaveRuntime.Current = data;
			Debug.Log("세이브");
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}

	public string ExportSave()
	{
		Save();
		return SaveRuntime.Store.Export();
	}

	public bool ImportSave(string envelopeJson, out string error)
	{
		if (!SaveRuntime.Store.TryImport(envelopeJson, out SaveDataV1 data,
			out error))
		{
			return false;
		}

		ApplySaveData(data);
		SaveRuntime.Current = data;
		return true;
	}

	private SaveDataV1 CreateFreshSaveData()
	{
		string language = Application.systemLanguage.ToString();
		if (LocalizeManager.Instance != null &&
			!string.IsNullOrWhiteSpace(LocalizeManager.Instance.Language))
		{
			language = LocalizeManager.Instance.Language;
		}

		SaveDataV1 data = SaveDataV1.CreateFresh(language,
			SaveRuntime.Clock.UtcNow);
		for (int i = 0; i < data.islandGoods.Length; i++)
		{
			for (int j = 0; j < data.islandGoods[i].goods.Length; j++)
			{
				data.islandGoods[i].goods[j] =
					(int)IslandGoodsDefault[i].goods[j].value;
			}
		}

		return data;
	}

	private SaveDataV1 CreateSaveData(DateTime utcNow)
	{
		SaveDataV1 data = SaveDataV1.CreateFresh(CurrentLanguage(), utcNow);
		data.pickboat = pickboat;
		data.hiddenUnlocked = isHidden;
		data.tutorialComplete = isTuto;
		data.noAds = noAds;
		data.allIslands = isAllIsland;
		Copy(IslandDone, data.islandDone);

		data.levelBody = Level_Body;
		data.levelCannon = Level_Cannon;
		data.levelFishing = Level_Fishing;
		data.levelSail = Level_Sail;
		data.levelSteering = Level_Steering;
		for (int i = 0; i < data.resources.Length; i++)
		{
			data.resources[i] = Mathf.Max(0, (int)myResource[i].value);
		}
		data.lastIslandNumber = LastIslandNumber;
		data.targetIslandNumber = TargetIslandNumber;
		data.emblemNumber = MyAmblemN;
		data.currentHp = Mathf.Max(0, (int)MyCurrnetHP);
		data.deathCount = Mathf.Max(0, (int)DeadNum);
		data.revivalCount = Mathf.Max(0, (int)Revival);
		data.bestArenaKill = Mathf.Max(0, (int)BestArenaKill1);
		data.bestColosseumKill = Mathf.Max(0, (int)BestColloseoKill);
		data.currentWorld = (int)CurrentWorld;
		data.worldLastDokdo = WorldLast1;
		data.worldLastItaly = WorldLast2;

		for (int i = 0; i < data.islandGoods.Length; i++)
		{
			for (int j = 0; j < data.islandGoods[i].goods.Length; j++)
			{
				data.islandGoods[i].goods[j] = Mathf.Max(0,
					(int)IslandGoods[i].goods[j].value);
			}
		}
		for (int i = 0; i < data.assistantLevels.Length; i++)
		{
			data.assistantLevels[i] = Mathf.Max(0, (int)AssistantShip_Lv[i]);
		}
		for (int i = 0; i < data.seagullLevels.Length; i++)
		{
			data.seagullLevels[i] = Mathf.Max(0, (int)Seagull_Lv[i]);
		}
		data.nessyLevel = Mathf.Max(0, (int)Nessy_Lv);
		data.marketAdCooldown = Mathf.Max(0f, MarketAdsTime);
		data.marketResetCooldown = Mathf.Max(0f, ResetMarketTime);
		data.freeCoinCooldown = Mathf.Max(0f, freeCoinTime);

		SaveDataV1 previous = SaveRuntime.Current;
		data.dailyRewardDay = Mathf.Clamp((int)currentday, 0, 10);
		data.lastDailyRewardDateUtc = previous == null
			? string.Empty
			: previous.lastDailyRewardDateUtc;
		data.freeCoinAvailableAtUtc = NormalizeUtcOrEpoch(coinDay);
		return data;
	}

	private void ApplySaveData(SaveDataV1 data)
	{
		pickboat = data.pickboat;
		isHidden = data.hiddenUnlocked;
		isTuto = data.tutorialComplete;
		noAds = data.noAds;
		isAllIsland = data.allIslands;
		Copy(data.islandDone, IslandDone);
		IslandDone[0] = true;
		IslandDone[28] = true;

		Level_Body = data.levelBody;
		Level_Cannon = data.levelCannon;
		Level_Fishing = data.levelFishing;
		Level_Sail = data.levelSail;
		Level_Steering = data.levelSteering;
		for (int i = 0; i < data.resources.Length; i++)
		{
			myResource[i].value = data.resources[i];
		}
		LastIslandNumber = data.lastIslandNumber;
		TargetIslandNumber = data.targetIslandNumber;
		MyAmblemN = data.emblemNumber;
		MyCurrnetHP = data.currentHp;
		DeadNum = data.deathCount;
		Revival = data.revivalCount;
		BestArenaKill1 = data.bestArenaKill;
		BestColloseoKill = data.bestColosseumKill;
		CurrentWorld = (WorldType)data.currentWorld;
		WorldLast1 = data.worldLastDokdo;
		WorldLast2 = data.worldLastItaly;

		for (int i = 0; i < data.islandGoods.Length; i++)
		{
			for (int j = 0; j < data.islandGoods[i].goods.Length; j++)
			{
				IslandGoods[i].goods[j].value = data.islandGoods[i].goods[j];
			}
		}
		for (int i = 0; i < data.assistantLevels.Length; i++)
		{
			AssistantShip_Lv[i] = data.assistantLevels[i];
		}
		for (int i = 0; i < data.seagullLevels.Length; i++)
		{
			Seagull_Lv[i] = data.seagullLevels[i];
		}
		Nessy_Lv = data.nessyLevel;
		MarketAdsTime = data.marketAdCooldown;
		ResetMarketTime = data.marketResetCooldown;
		freeCoinTime = data.freeCoinCooldown;
		currentday = data.dailyRewardDay;
		coinDay = data.freeCoinAvailableAtUtc;
	}

	private int ElapsedSinceSave(int maximumSeconds)
	{
		SaveDataV1 data = SaveRuntime.Current;
		if (data == null || !SaveTime.TryParseUtc(data.savedAtUtc,
			out DateTime previousUtc))
		{
			return 0;
		}

		return SaveTime.ElapsedSeconds(SaveRuntime.Clock.UtcNow, previousUtc,
			maximumSeconds);
	}

	private static void Copy(bool[] source, bool[] destination)
	{
		Array.Copy(source, destination, Mathf.Min(source.Length, destination.Length));
	}

	private static string NormalizeUtcOrEpoch(string value)
	{
		if (SaveTime.TryParseUtc(value, out DateTime utc))
		{
			return utc.ToString("O");
		}

		if (DateTime.TryParse(value, out DateTime legacy))
		{
			return legacy.ToUniversalTime().ToString("O");
		}

		return DateTime.UnixEpoch.ToString("O");
	}

	private static string CurrentLanguage()
	{
		if (LocalizeManager.Instance != null &&
			!string.IsNullOrWhiteSpace(LocalizeManager.Instance.Language))
		{
			return LocalizeManager.Instance.Language;
		}

		if (SaveRuntime.Current != null &&
			!string.IsNullOrWhiteSpace(SaveRuntime.Current.language))
		{
			return SaveRuntime.Current.language;
		}

		return Application.systemLanguage.ToString();
	}

	private void BalData()
	{
		baseSpeed = 500f;
		SpeedCoeff = 25f;
		baseSteer = 300f;
		steerCoeff = 70f;
		baseMass = 500f;
		massCoeff = 100f;
		rigidAngleDrag = 4f;
		baseHP = 30;
		hpCoeff = 50;
		hpCoeff2 = 10;
		baseAtk = 5;
		AtkCoeff = 2;
		ResetMarketPeriod = 300f;
		freeCoinResetPeriod = 300f;
		ObscuredInt[] p = new ObscuredInt[6] { 6, 36, 110, 480, 2100, 20000 };
		ObscuredInt[] p2 = new ObscuredInt[6] { 5, 44, 128, 480, 2400, 22000 };
		ObscuredInt[] p3 = new ObscuredInt[6] { 5, 40, 120, 410, 2400, 24000 };
		ObscuredInt[] p4 = new ObscuredInt[6] { 5, 40, 105, 440, 2400, 24000 };
		ObscuredInt[] p5 = new ObscuredInt[6] { 4, 40, 120, 480, 2200, 26000 };
		ObscuredInt[] p6 = new ObscuredInt[6] { 4, 46, 120, 410, 2100, 24000 };
		MarketPrice[0].p = p;
		MarketPrice[1].p = p2;
		MarketPrice[2].p = p3;
		MarketPrice[3].p = p4;
		MarketPrice[4].p = p5;
		MarketPrice[5].p = p6;
		IdAndInt[] goods = new IdAndInt[6]
		{
			new IdAndInt(200),
			new IdAndInt(100),
			new IdAndInt(35),
			new IdAndInt(20),
			new IdAndInt(5),
			new IdAndInt(1)
		};
		IdAndInt[] goods2 = new IdAndInt[6]
		{
			new IdAndInt(100),
			new IdAndInt(140),
			new IdAndInt(40),
			new IdAndInt(20),
			new IdAndInt(10),
			new IdAndInt(2)
		};
		IdAndInt[] goods3 = new IdAndInt[6]
		{
			new IdAndInt(100),
			new IdAndInt(150),
			new IdAndInt(35),
			new IdAndInt(15),
			new IdAndInt(10),
			new IdAndInt(2)
		};
		IdAndInt[] goods4 = new IdAndInt[6]
		{
			new IdAndInt(100),
			new IdAndInt(150),
			new IdAndInt(25),
			new IdAndInt(15),
			new IdAndInt(11),
			new IdAndInt(3)
		};
		IdAndInt[] goods5 = new IdAndInt[6]
		{
			new IdAndInt(90),
			new IdAndInt(150),
			new IdAndInt(35),
			new IdAndInt(13),
			new IdAndInt(10),
			new IdAndInt(3)
		};
		IdAndInt[] goods6 = new IdAndInt[6]
		{
			new IdAndInt(90),
			new IdAndInt(120),
			new IdAndInt(35),
			new IdAndInt(10),
			new IdAndInt(5),
			new IdAndInt(3)
		};
		for (int i = 0; i < IslandGoodsDefault.Length; i++)
		{
			if (i <= 3)
			{
				IslandGoodsDefault[i].goods = goods;
			}
			else if (i <= 8)
			{
				IslandGoodsDefault[i].goods = goods2;
			}
			else if (i <= 13)
			{
				IslandGoodsDefault[i].goods = goods3;
			}
			else if (i <= 16)
			{
				IslandGoodsDefault[i].goods = goods4;
			}
			else if (i <= 20)
			{
				IslandGoodsDefault[i].goods = goods5;
			}
			else if (i <= 27)
			{
				IslandGoodsDefault[i].goods = goods6;
			}
			else if (i <= 30)
			{
				IslandGoodsDefault[i].goods = goods;
			}
			else if (i <= 35)
			{
				IslandGoodsDefault[i].goods = goods2;
			}
			else if (i <= 38)
			{
				IslandGoodsDefault[i].goods = goods3;
			}
			else if (i <= 42)
			{
				IslandGoodsDefault[i].goods = goods4;
			}
			else if (i <= 45)
			{
				IslandGoodsDefault[i].goods = goods5;
			}
			else
			{
				IslandGoodsDefault[i].goods = goods6;
			}
		}
		green0 = new int[13]
		{
			0, 1, 4, 6, 8, 11, 20, 21, 27, 28,
			33, 38, 43
		};
		blue1 = new int[8] { 2, 9, 18, 24, 29, 34, 39, 44 };
		desert2 = new int[10] { 3, 7, 12, 13, 19, 26, 30, 35, 40, 45 };
		ice3 = new int[9] { 15, 16, 22, 23, 25, 31, 36, 41, 46 };
		pink4 = new int[7] { 5, 10, 14, 17, 32, 37, 42 };
	}

	public int SetTier(int i)
	{
		int num = 0;
		if (i <= 3)
		{
			return 0;
		}
		if (i <= 8)
		{
			return 1;
		}
		if (i <= 13)
		{
			return 2;
		}
		if (i <= 16)
		{
			return 3;
		}
		if (i <= 20)
		{
			return 4;
		}
		if (i <= 27)
		{
			return 5;
		}
		if (i <= 30)
		{
			return 0;
		}
		if (i <= 35)
		{
			return 1;
		}
		if (i <= 38)
		{
			return 2;
		}
		if (i <= 42)
		{
			return 3;
		}
		if (i <= 45)
		{
			return 4;
		}
		if (i <= 47)
		{
			return 5;
		}
		return 6;
	}

	public void Load()
	{
		SaveDataV1 data;
		if (!SaveRuntime.Store.TryLoad(out data, out string error))
		{
			data = CreateFreshSaveData();
			if (!error.Contains("does not exist"))
			{
				Debug.LogWarning("Starting a fresh save: " + error);
			}
		}
		else if (!string.IsNullOrEmpty(error))
		{
			Debug.LogWarning(error);
		}

		ApplySaveData(data);
		SaveRuntime.Current = data;
		Debug.Log("로드");
	}

	public void ReCalTime()
	{
		int num = ElapsedSinceSave(int.MaxValue);
		ReRewardT = 0;
		if (num > 300)
		{
			ReRewardT = Mathf.Clamp(num, 300, 43200);
			EventManager.TriggerEvent(MyEvent.ReReward);
		}
		if (num > 0)
		{
			MarketAdsTime = (float)MarketAdsTime - (float)num;
			ResetMarketTime = (float)ResetMarketTime - (float)num;
			freeCoinTime = (float)freeCoinTime - (float)num;
			if ((float)MarketAdsTime < 0f)
			{
				MarketAdsTime = 0f;
			}
			if ((float)ResetMarketTime < 0f)
			{
				ResetMarketTime = 0f;
			}
			if ((float)freeCoinTime < 0f)
			{
				freeCoinTime = 0f;
			}
		}
	}

	public void ReCalHP()
	{
		int num = ElapsedSinceSave(600);
		if (num > 0)
		{
			int value = (int)((float)((int)HealPoint() * num) * 0.5f);
			AddHP(value);
			Save();
		}
	}

	public bool isNewDay()
	{
		DateTime today = SaveRuntime.Clock.UtcNow.Date;
		SaveDataV1 data = SaveRuntime.Current ?? CreateFreshSaveData();
		currentday = data.dailyRewardDay;
		bool result = false;
		if (isTuto)
		{
			bool rewardedToday = SaveTime.TryParseUtc(
				data.lastDailyRewardDateUtc, out DateTime lastReward) &&
				lastReward.Date == today;
			if (!rewardedToday)
			{
				currentday = (int)currentday + 1;
				if ((int)currentday > 10)
				{
					currentday = 1;
				}
				data.dailyRewardDay = currentday;
				data.lastDailyRewardDateUtc = today.ToString("O");
				SaveRuntime.Current = data;
				Save();
				result = true;
			}
			else
			{
				result = false;
			}
		}
		return result;
	}

	public void DailyReward(int day)
	{
		PosiSound(1);
		switch (day)
		{
		case 1:
			AddResource(itemType.wood, 30);
			break;
		case 2:
			AddRevival(1);
			break;
		case 3:
			AddResource(itemType.iron, 100);
			break;
		case 4:
			AddRevival(3);
			break;
		case 5:
			AddResource(itemType.silver, 50);
			break;
		case 6:
			AddRevival(5);
			break;
		case 7:
			AddResource(itemType.gold, 20);
			break;
		case 8:
			AddRevival(8);
			break;
		case 9:
			AddResource(itemType.diamond, 5);
			break;
		case 10:
			AddRevival(15);
			break;
		}
	}

	public int AmblemNumber(int islandNum)
	{
		int result = 0;
		if (green0.Contains(islandNum))
		{
			result = 0;
		}
		else if (blue1.Contains(islandNum))
		{
			result = 1;
		}
		else if (desert2.Contains(islandNum))
		{
			result = 2;
		}
		else if (ice3.Contains(islandNum))
		{
			result = 3;
		}
		else if (pink4.Contains(islandNum))
		{
			result = 4;
		}
		else
		{
			Debug.Log("나라 없음");
		}
		return result;
	}
}
