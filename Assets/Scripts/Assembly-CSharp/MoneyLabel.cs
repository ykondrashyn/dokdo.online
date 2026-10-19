using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MoneyLabel : MonoBehaviour
{
	public Text label;

	private GameManager GM;

	private UnityAction someListener;

	private void LateUpdate()
	{
		label.text = GM.myResource[0].value.ToString("N0");
	}

	private void Awake()
	{
		GM = GameManager.Instance;
		someListener = NoMoney;
	}

	private void OnEnable()
	{
		EventManager.StartListening(MyEvent.NoMoney, someListener);
	}

	private void OnDisable()
	{
		EventManager.StopListening(MyEvent.NoMoney, someListener);
	}

	private void NoMoney()
	{
		label.DOKill();
		label.color = Color.red;
		label.DOColor(Color.white, 0.5f);
	}
}
