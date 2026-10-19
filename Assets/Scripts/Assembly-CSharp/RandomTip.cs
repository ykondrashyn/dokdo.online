using UnityEngine;
using UnityEngine.UI;

public class RandomTip : MonoBehaviour
{
	public Text label;

	public string[] Tipids;

	private void OnEnable()
	{
		string id = Tipids[Random.Range(0, Tipids.Length)];
		label.text = "! : " + LocalizeManager.Instance.translate(id);
	}
}
