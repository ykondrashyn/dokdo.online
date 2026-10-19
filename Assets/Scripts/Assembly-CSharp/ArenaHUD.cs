using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ArenaHUD : MonoBehaviour
{
	private UnityAction arenakill;

	public Text TitleLabel;

	public Text KillLabel;

	public Transform killLableTrans;

	public Color defaultColor;

	public GameObject[] title;

	private GameManager GM;

	private void Awake()
	{
		GM = GameManager.Instance;
		arenakill = Addkill;
	}

	private void OnEnable()
	{
		EventManager.StartListening(MyEvent.ArenaKill, arenakill);
		for (int i = 0; i < title.Length; i++)
		{
			title[i].SetActive(false);
		}
		if (GM.CurrentWorld.Equals(WorldType.Dokdo))
		{
			TitleLabel.text = "ARENA";
			title[0].SetActive(true);
		}
		else
		{
			TitleLabel.text = "Colloseo";
			title[1].SetActive(true);
		}
	}

	private void OnDisable()
	{
		EventManager.StopListening(MyEvent.ArenaKill, arenakill);
	}

	private void Addkill()
	{
		GameManager gM = GM;
		gM.CurrentArenaKill = (int)gM.CurrentArenaKill + 1;
		KillLabel.DOKill();
		KillLabel.color = defaultColor;
		killLableTrans.DOKill();
		killLableTrans.localScale = Vector3.one;
		KillLabel.text = string.Concat(GM.CurrentArenaKill, " KILL");
		KillLabel.DOColor(Color.red, 0.3f).From();
		killLableTrans.DOScale(Vector3.one * 1.2f, 0.3f).From().SetEase(Ease.InBack);
	}
}
