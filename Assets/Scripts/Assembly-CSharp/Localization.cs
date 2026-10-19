using UnityEngine;
using UnityEngine.UI;

public class Localization : MonoBehaviour
{
	public string id;

	private Text label;

	private LocalizeManager LM;

	private string str;

	private void Awake()
	{
		label = GetComponent<Text>();
		LM = LocalizeManager.Instance;
	}

	private void OnEnable()
	{
		LM.localized.TryGetValue(id, out str);
		label.text = str;
	}
}
