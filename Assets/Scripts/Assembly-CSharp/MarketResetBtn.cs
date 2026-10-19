using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MarketResetBtn : MonoBehaviour
{
	public UnityEvent Event;

	public Image btn;

	public Text timer;

	private GameManager GM;

	public Color color;

	private bool isOn;

	private void Start()
	{
		GM = GameManager.Instance;
	}

	public void Work()
	{
		if (isOn)
		{
			Event.Invoke();
		}
	}

	private void LateUpdate()
	{
		if ((float)GM.MarketAdsTime > 0f)
		{
			isOn = false;
			btn.color = color;
			timer.text = showTime(GM.MarketAdsTime);
		}
		else
		{
			btn.color = Color.white;
			timer.text = string.Empty;
			isOn = true;
		}
	}

	private string showTime(float t)
	{
		int num = Mathf.FloorToInt(t / 60f);
		int num2 = Mathf.FloorToInt(t - (float)(num * 60));
		return string.Format("{0:0}:{1:00}", num, num2);
	}
}
