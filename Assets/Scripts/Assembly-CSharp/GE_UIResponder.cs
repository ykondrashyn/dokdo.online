using UnityEngine;
using UnityEngine.UI;

public class GE_UIResponder : MonoBehaviour
{
	public string m_PackageTitle = "-";

	public string m_TargetURL = "www.unity3d.com";

	private void Start()
	{
		GameObject gameObject = GameObject.Find("Text Package Title");
		if (gameObject != null)
		{
			Text component = gameObject.GetComponent<Text>();
			component.text = m_PackageTitle;
		}
	}

	private void Update()
	{
	}

	public void OnButton_AssetName()
	{
		// External links are disabled in the standalone game.
	}
}
