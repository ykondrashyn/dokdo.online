using UnityEngine;
using UnityEngine.UI;

public class Panel_Store : MonoBehaviour
{
	private GameManager GM;

	public Text freecoin;

	public Text freecoinTime;

	public GameObject AdsDone;

	public GameObject FreeDone;

	private void Awake()
	{
		GM = GameManager.Instance;
	}

	private void OnEnable()
	{
		GM.popSound();
		freecoin.text = (250 * (int)GM.Level_Body).ToString("N0");
	}

	private void OnDisable()
	{
		GM.popSound();
	}

	private void Start()
	{
	}

	private void LateUpdate()
	{
		if ((float)GM.freeCoinTime > 0f)
		{
			FreeDone.SetActive(true);
			freecoinTime.text = showTime(GM.freeCoinTime);
		}
		else
		{
			FreeDone.SetActive(false);
		}
		AdsDone.SetActive(GM.noAds);
	}

	private string showTime(float t)
	{
		int num = Mathf.FloorToInt(t / 60f);
		int num2 = Mathf.FloorToInt(t - (float)(num * 60));
		return string.Format("{0:0}:{1:00}", num, num2);
	}

	public void Buy_IAP(string productID)
	{
		ThirdPartyManager.instance.BuyProduct(productID);
	}

	public void Restore()
	{
		ThirdPartyManager.instance.Restore();
	}
}
