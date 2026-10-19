using DG.Tweening;
using Dokdo;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AutoFade : MonoBehaviour
{
	public Image img;

	public Text load;

	public Image img2;

	public GameObject offTarget;

	private void Start()
	{
		img.DOFade(1f, 0f);
		img.DOFade(0f, 1f);
		load.DOFade(0f, 0f);
	}

	public void StartGame()
	{
		load.DOFade(1f, 1f);
		img.DOFade(1f, 1f).OnComplete(delegate
		{
			SceneManager.LoadScene(SceneIds.Main);
		});
	}
}
