using System.Collections;
using UnityEngine;

public class FPSDisplayScript : MonoBehaviour
{
	private float fps = 60f;

	public string currentfps;

	private GUIStyle guiStyle = new GUIStyle();

	private int numberOfDataPoints;

	private float currentAverageFps = 60f;

	private bool isLow;

	private float localT;

	public bool Show;

	private WaitForSeconds sec = new WaitForSeconds(0.1f);

	private void Start()
	{
		isLow = false;
		fps = 60f;
		localT = 0f;
		currentAverageFps = 60f;
		GameManager.Instance.AddStringToMonitor("해상도 : " + Screen.width + "x" + Screen.height);
		Application.targetFrameRate = 60;
		StartCoroutine(calFps());
	}

	private IEnumerator calFps()
	{
		while (true)
		{
			fps = UpdateCumulativeAverageFPS(1f / Time.deltaTime) + 1f;
			currentfps = ((int)fps/*cast due to .constrained prefix*/).ToString();
			yield return sec;
		}
	}

	private void Update()
	{
		if (localT <= 120f)
		{
			localT += Time.deltaTime;
		}
		if (localT > 5f && localT < 30f && !isLow && fps < 26f)
		{
			isLow = true;
			int num = (int)((float)Screen.height * 0.5f);
			if (num < 1280)
			{
				num = 1280;
			}
			int num2 = (int)((float)num * (float)Screen.width / (float)Screen.height);
			Screen.SetResolution(num2, num, true);
			numberOfDataPoints = 0;
			currentAverageFps = 60f;
			QualitySettings.SetQualityLevel(0);
			GameManager.Instance.AddStringToMonitor("저사양 모드 -> 해상도 : " + num2 + "x" + num);
		}
	}

	private float UpdateCumulativeAverageFPS(float newFPS)
	{
		numberOfDataPoints++;
		currentAverageFps += (newFPS - currentAverageFps) / (float)numberOfDataPoints;
		return currentAverageFps;
	}

	private void OnGUI()
	{
		if (Show)
		{
			guiStyle.fontSize = 20;
			guiStyle.normal.textColor = Color.white;
			guiStyle.alignment = TextAnchor.LowerRight;
			GUI.Label(new Rect(Screen.width - 100, Screen.height - 50, 100f, 50f), currentfps + "fps", guiStyle);
		}
	}
}
