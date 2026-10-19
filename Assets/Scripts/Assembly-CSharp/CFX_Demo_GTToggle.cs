using UnityEngine;
using UnityEngine.UI;

public class CFX_Demo_GTToggle : MonoBehaviour
{
	public Texture Normal;

	public Texture Hover;

	public Color NormalColor = new Color32(128, 128, 128, 128);

	public Color DisabledColor = new Color32(128, 128, 128, 48);

	public bool State = true;

	public string Callback;

	public GameObject Receiver;

	private bool Over;

	private RawImage ToggleImage;

	private Text Label;

	private void Awake()
	{
		ToggleImage = GetComponent<RawImage>();
		Label = GetComponentInChildren<Text>();
		UpdateTexture();
	}

	private void Update()
	{
		if (RectTransformUtility.RectangleContainsScreenPoint(ToggleImage.rectTransform, Input.mousePosition))
		{
			Over = true;
			if (Input.GetMouseButtonDown(0))
			{
				OnClick();
			}
		}
		else
		{
			Over = false;
			ToggleImage.color = NormalColor;
		}
		UpdateTexture();
	}

	private void OnClick()
	{
		State = !State;
		Receiver.SendMessage(Callback);
	}

	private void UpdateTexture()
	{
		Color color = ((!State) ? DisabledColor : NormalColor);
		if (Over)
		{
			ToggleImage.texture = Hover;
		}
		else
		{
			ToggleImage.texture = Normal;
		}
		ToggleImage.color = color;
		if (Label != null)
		{
			Label.color = color * 1.75f;
		}
	}
}
