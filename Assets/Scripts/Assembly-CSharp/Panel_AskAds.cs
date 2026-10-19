using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Panel_AskAds : MonoBehaviour
{
	public Image BG;

	public Transform popup;

	public GameObject Parent;

	public Text coinLabel;

	public void Show()
	{
		Parent.SetActive(true);
		BG.DOKill();
		popup.DOKill();
		BG.DOFade(0f, 0f);
		BG.DOFade(0.5f, 0.5f).SetDelay(1f);
		popup.localScale = Vector3.zero;
		popup.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetDelay(1f)
			.OnComplete(delegate
			{
				Time.timeScale = 0f;
			});
		coinLabel.text = (500 * (int)GameManager.Instance.Level_Body).ToString("N0");
	}

	public void Close()
	{
		Time.timeScale = 1f;
		popup.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack);
		BG.DOFade(0f, 0.5f).OnComplete(delegate
		{
			Parent.SetActive(false);
		});
	}
}
