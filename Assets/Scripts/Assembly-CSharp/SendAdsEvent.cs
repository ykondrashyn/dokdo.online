using UnityEngine;

public class SendAdsEvent : MonoBehaviour
{
	public RewardType Type;

	public void Send()
	{
		GameManager.Instance.popSound();
		ThirdPartyManager.instance.ShowVideoAds(Type);
	}
}
