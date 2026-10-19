using Dokdo;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FirstLoad : MonoBehaviour
{
	private void Start()
	{
		Invoke(nameof(LoadIntro), 1f);
	}

	private void LoadIntro()
	{
		SceneManager.LoadScene(SceneIds.Intro);
	}
}
