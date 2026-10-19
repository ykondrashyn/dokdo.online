using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MNPopup
{
	public delegate void MNPopupAction();

	private const int MaxActions = 3;

	private readonly List<ActionEntry> actions = new List<ActionEntry>();

	private MNPopupAction dismissCallback;

	private readonly string title;

	private readonly string message;

	public string Title
	{
		get { return title; }
	}

	public string Message
	{
		get { return message; }
	}

	public MNPopup(string title, string message)
	{
		this.title = title ?? string.Empty;
		this.message = message ?? string.Empty;
	}

	public void AddAction(string actionTitle, MNPopupAction callback)
	{
		if (actions.Count >= MaxActions)
		{
			Debug.LogWarning("Action NOT added! Actions limit exceeded");
			return;
		}

		if (actions.Exists(entry => entry.Title == actionTitle))
		{
			Debug.LogWarning("Action NOT added! Action title already exists");
			return;
		}

		actions.Add(new ActionEntry(actionTitle, callback));
	}

	public void AddDismissListener(MNPopupAction callback)
	{
		dismissCallback = callback;
	}

	public void Show()
	{
		OfflinePopupView.ShowPopup(title, message, actions, dismissCallback);
	}

	public sealed class ActionEntry
	{
		public readonly string Title;

		public readonly MNPopupAction Callback;

		public ActionEntry(string title, MNPopupAction callback)
		{
			Title = title ?? string.Empty;
			Callback = callback;
		}
	}
}

public static class MNP
{
	public static void ShowPreloader(string title, string message)
	{
		OfflinePopupView.ShowPreloader(title, message);
	}

	public static void HidePreloader()
	{
		OfflinePopupView.HidePreloader();
	}
}

internal sealed class OfflinePopupView : MonoBehaviour
{
	private const string HostName = "DOKDO Offline Popup Host";

	private static OfflinePopupView instance;

	private GameObject popupRoot;

	private GameObject preloaderRoot;

	private MNPopup.MNPopupAction dismissCallback;

	public static void ShowPopup(string title, string message,
		IList<MNPopup.ActionEntry> actions, MNPopup.MNPopupAction onDismiss)
	{
		Instance().CreatePopup(title, message, actions, onDismiss);
	}

	public static void ShowPreloader(string title, string message)
	{
		Instance().CreatePreloader(title, message);
	}

	public static void HidePreloader()
	{
		if (instance != null && instance.preloaderRoot != null)
		{
			Destroy(instance.preloaderRoot);
			instance.preloaderRoot = null;
		}
	}

	private static OfflinePopupView Instance()
	{
		if (instance != null)
		{
			return instance;
		}

		GameObject host = new GameObject(HostName);
		DontDestroyOnLoad(host);
		instance = host.AddComponent<OfflinePopupView>();
		EnsureEventSystem();
		return instance;
	}

	private static void EnsureEventSystem()
	{
		if (FindObjectOfType<EventSystem>() != null)
		{
			return;
		}

		GameObject eventSystem = new GameObject(
			"DOKDO Offline Popup EventSystem", typeof(EventSystem),
			typeof(StandaloneInputModule));
		DontDestroyOnLoad(eventSystem);
	}

	private void CreatePopup(string title, string message,
		IList<MNPopup.ActionEntry> actions, MNPopup.MNPopupAction onDismiss)
	{
		ClosePopup(false);
		dismissCallback = onDismiss;
		popupRoot = CreateCanvas("DOKDO Popup", 32000);
		CreateOverlay(popupRoot.transform, true);
		RectTransform panel = CreatePanel(popupRoot.transform, new Vector2(620f, 430f));
		CreateText(panel, title, 34, FontStyle.Bold, new Vector2(0f, 120f),
			new Vector2(540f, 70f));
		CreateText(panel, message, 28, FontStyle.Normal, new Vector2(0f, 20f),
			new Vector2(540f, 130f));

		int count = Mathf.Max(1, actions.Count);
		float buttonWidth = Mathf.Min(170f, 500f / count);
		float spacing = 18f;
		float totalWidth = count * buttonWidth + (count - 1) * spacing;
		for (int i = 0; i < count; i++)
		{
			string actionTitle = actions.Count == 0 ? "Ok" : actions[i].Title;
			MNPopup.MNPopupAction action = actions.Count == 0 ? null :
				actions[i].Callback;
			float x = -totalWidth * 0.5f + buttonWidth * 0.5f +
				i * (buttonWidth + spacing);
			CreateButton(panel, actionTitle, new Vector2(x, -130f),
				new Vector2(buttonWidth, 74f), action);
		}
	}

	private void CreatePreloader(string title, string message)
	{
		HidePreloader();
		preloaderRoot = CreateCanvas("DOKDO Preloader", 31900);
		CreateOverlay(preloaderRoot.transform, false);
		RectTransform panel = CreatePanel(
			preloaderRoot.transform, new Vector2(520f, 260f));
		CreateText(panel, title, 32, FontStyle.Bold, new Vector2(0f, 55f),
			new Vector2(450f, 65f));
		CreateText(panel, message, 26, FontStyle.Normal, new Vector2(0f, -40f),
			new Vector2(450f, 95f));
	}

	private GameObject CreateCanvas(string name, int sortingOrder)
	{
		GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas),
			typeof(CanvasScaler), typeof(GraphicRaycaster));
		root.transform.SetParent(transform, false);
		Canvas canvas = root.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = sortingOrder;
		CanvasScaler scaler = root.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(720f, 1280f);
		scaler.matchWidthOrHeight = 0.5f;
		return root;
	}

	private void CreateOverlay(Transform parent, bool dismissible)
	{
		GameObject overlay = new GameObject("Overlay", typeof(RectTransform),
			typeof(Image));
		overlay.transform.SetParent(parent, false);
		RectTransform rect = overlay.GetComponent<RectTransform>();
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
		overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
		if (dismissible)
		{
			Button button = overlay.AddComponent<Button>();
			button.transition = Selectable.Transition.None;
			button.onClick.AddListener(delegate { ClosePopup(true); });
		}
	}

	private static RectTransform CreatePanel(Transform parent, Vector2 size)
	{
		GameObject panelObject = new GameObject("Panel", typeof(RectTransform),
			typeof(Image));
		panelObject.transform.SetParent(parent, false);
		RectTransform panel = panelObject.GetComponent<RectTransform>();
		panel.anchorMin = new Vector2(0.5f, 0.5f);
		panel.anchorMax = new Vector2(0.5f, 0.5f);
		panel.sizeDelta = size;
		panel.anchoredPosition = Vector2.zero;
		panelObject.GetComponent<Image>().color = new Color32(28, 48, 73, 250);
		return panel;
	}

	private static Text CreateText(Transform parent, string value, int fontSize,
		FontStyle style, Vector2 position, Vector2 size)
	{
		GameObject textObject = new GameObject("Text", typeof(RectTransform),
			typeof(Text));
		textObject.transform.SetParent(parent, false);
		RectTransform rect = textObject.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = position;
		rect.sizeDelta = size;
		Text text = textObject.GetComponent<Text>();
		text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
		text.fontSize = fontSize;
		text.fontStyle = style;
		text.alignment = TextAnchor.MiddleCenter;
		text.color = Color.white;
		text.horizontalOverflow = HorizontalWrapMode.Wrap;
		text.verticalOverflow = VerticalWrapMode.Truncate;
		text.text = value;
		return text;
	}

	private void CreateButton(Transform parent, string title, Vector2 position,
		Vector2 size, MNPopup.MNPopupAction action)
	{
		GameObject buttonObject = new GameObject("Button " + title,
			typeof(RectTransform), typeof(Image), typeof(Button));
		buttonObject.transform.SetParent(parent, false);
		RectTransform rect = buttonObject.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = position;
		rect.sizeDelta = size;
		buttonObject.GetComponent<Image>().color = new Color32(65, 126, 181, 255);
		Button button = buttonObject.GetComponent<Button>();
		button.onClick.AddListener(delegate
		{
			ClosePopup(false);
			if (action != null)
			{
				action();
			}
		});
		Text label = CreateText(buttonObject.transform, title, 25, FontStyle.Bold,
			Vector2.zero, size);
		label.raycastTarget = false;
	}

	private void Update()
	{
		if (popupRoot != null && Input.GetKeyDown(KeyCode.Escape))
		{
			ClosePopup(true);
		}
	}

	private void ClosePopup(bool invokeDismiss)
	{
		MNPopup.MNPopupAction callback = dismissCallback;
		dismissCallback = null;
		if (popupRoot != null)
		{
			Destroy(popupRoot);
			popupRoot = null;
		}
		if (invokeDismiss && callback != null)
		{
			callback();
		}
	}
}
