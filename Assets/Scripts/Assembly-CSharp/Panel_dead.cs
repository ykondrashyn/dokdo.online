using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Panel_dead : MonoBehaviour
{
	public GameObject Parent;

	private GameManager GM;

	public ShipController SC;

	public Image BG;

	public Transform popup;

	public Text costLabel;

	public GameObject Store;

	private bool isdone;

	private UIManager UM;

	private void Awake()
	{
		GM = GameManager.Instance;
		UM = GameObject.Find("UI Manager").GetComponent<UIManager>();
	}

	private void OnEnable()
	{
		isdone = false;
		BG.DOKill();
		popup.DOKill();
		BG.DOFade(0f, 0f);
		BG.DOFade(0.5f, 2f);
		popup.localScale = Vector3.zero;
		UM.LabelUpdate();
		if (!GM.isRevived)
		{
			popup.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetDelay(2f);
		}
		else
		{
			isdone = true;
			Invoke("JustReborn", 2f);
		}
		GM.NegSound();
		costLabel.text = (5000 * (int)GM.Level_Body).ToString("N0");
	}

	private void JustReborn()
	{
		Time.timeScale = 1f;
		SC.Restart();
		BG.DOFade(0.5f, 0.5f).OnComplete(delegate
		{
			Parent.SetActive(false);
		});
	}

	public void Reset()
	{
		if (!isdone)
		{
			isdone = true;
			Time.timeScale = 1f;
			SC.Restart();
			popup.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack);
			BG.DOFade(0.5f, 0.5f).OnComplete(delegate
			{
				Parent.SetActive(false);
			});
		}
	}

	public void ReviveWithItem()
	{
		if (isdone)
		{
			return;
		}
		if ((int)GM.Revival > 0)
		{
			isdone = true;
			GM.AddRevival(-1);
			Time.timeScale = 1f;
			GM.FullHP();
			SC.Reborn();
			popup.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack);
			BG.DOFade(0.5f, 0.5f).OnComplete(delegate
			{
				Parent.SetActive(false);
			});
		}
		else
		{
			Store.SetActive(true);
		}
	}

	public void Revive(int Percent)
	{
		if (isdone)
		{
			return;
		}
		if (Percent < 100)
		{
			isdone = true;
			Time.timeScale = 1f;
			GM.hp30();
			SC.Reborn();
			popup.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack);
			BG.DOFade(0.5f, 0.5f).OnComplete(delegate
			{
				Parent.SetActive(false);
			});
			return;
		}
		int value = 5000 * (int)GM.Level_Body;
		if (GM.isEnoughMoney(value))
		{
			isdone = true;
			Time.timeScale = 1f;
			GM.FullHP();
			SC.Reborn();
			popup.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack);
			BG.DOFade(0.5f, 0.5f).OnComplete(delegate
			{
				Parent.SetActive(false);
			});
		}
		else
		{
			Store.SetActive(true);
		}
	}
}
