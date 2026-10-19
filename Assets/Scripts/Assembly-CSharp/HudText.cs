using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class HudText : MonoBehaviour
{
	private Text label;

	private Transform myT;

	private Image type;

	private GameManager GM;

	private void Awake()
	{
		GM = GameManager.Instance;
		myT = base.transform;
		label = GetComponent<Text>();
		type = base.transform.Find("type").GetComponent<Image>();
	}

	public void SetTypeValue(int N, itemType Type)
	{
		if (GM == null)
		{
			GM = GameManager.Instance;
		}
		type.sprite = GM.ResourceType[(int)Type];
		label.text = N.ToString("N0");
	}

	private void OnEnable()
	{
		myT.DOKill();
		label.DOKill();
		myT.localScale = Vector3.one;
		label.DOFade(0f, 0f);
		type.DOFade(0f, 0f);
		myT.DOLocalMoveX(0f, 0f);
		myT.DOLocalMoveY(100f, 0f);
		myT.DOLocalMoveY(200f, 3f);
		label.DOFade(1f, 1f).SetLoops(2, LoopType.Yoyo).OnComplete(delegate
		{
			base.gameObject.SetActive(false);
		});
		type.DOFade(1f, 1f).SetLoops(2, LoopType.Yoyo);
	}
}
