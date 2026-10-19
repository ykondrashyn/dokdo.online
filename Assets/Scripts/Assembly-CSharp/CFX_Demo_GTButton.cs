using UnityEngine;
using UnityEngine.UI;

public class CFX_Demo_GTButton : MonoBehaviour
{
	public Color NormalColor = new Color32(128, 128, 128, 128);

	public Color HoverColor = new Color32(128, 128, 128, 128);

	public string Callback;

	public GameObject Receiver;

	private RawImage ButtonImage;

	private void Awake()
	{
		ButtonImage = GetComponent<RawImage>();
	}

	private void Update()
	{
		if (RectTransformUtility.RectangleContainsScreenPoint(ButtonImage.rectTransform, Input.mousePosition))
		{
			ButtonImage.color = HoverColor;
			if (Input.GetMouseButtonDown(0))
			{
				OnClick();
			}
		}
		else
		{
			ButtonImage.color = NormalColor;
		}
	}

	private void OnClick()
	{
		Receiver.SendMessage(Callback);
	}
}
