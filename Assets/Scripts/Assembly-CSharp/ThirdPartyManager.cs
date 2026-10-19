using UnityEngine;

public class ThirdPartyManager : MonoBehaviour
{
	public static ThirdPartyManager instance;

	private GameManager GM;

	private void Awake()
	{
		if (instance != null)
		{
			return;
		}

		instance = this;
		GM = GameManager.Instance;
	}

	public void ShowInterstitialAds()
	{
	}

	public void ShowVideoAds(RewardType type)
	{
		Reward(type);
	}

	private void Reward(RewardType type)
	{
		switch (type)
		{
		case RewardType.Coin:
			GM.FreeCoinAds();
			break;
		case RewardType.HP100:
			GM.FullHP();
			break;
		case RewardType.HP30:
			Object.FindObjectOfType<Panel_dead>().Revive(30);
			break;
		case RewardType.MarketReset:
			GM.AdsMarketReset();
			break;
		case RewardType.AdsBox:
			GM.AdsBoxReward();
			GM.AddHP((int)((float)(int)GM.MyHP() * 0.3f));
			break;
		}

		GM.Save();
	}

	public void BuyProduct(string product)
	{
		Debug.Log("Purchases unavailable: " + product);
		ShowMessage("SYSTEM", "Purchases unavailable.");
	}

	public void Restore()
	{
		GM.noAds = true;
		GM.pickboat = true;
		EventManager.TriggerEvent(MyEvent.AssiUpdate);

		GameObject myPos = GameObject.Find("MyPos");
		if (myPos != null)
		{
			ShipController ship = myPos.GetComponent<ShipController>();
			if (ship != null)
			{
				ship.SetAssi();
			}
		}

		GM.Save();
		ShowMessage("SYSTEM", "Restore Complete");
	}

	private static void ShowMessage(string title, string message)
	{
		MNPopup popup = new MNPopup(title, message);
		popup.AddAction("Ok", delegate { });
		popup.Show();
	}
}
