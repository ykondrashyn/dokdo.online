using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventManager : MonoBehaviour
{
	private Dictionary<MyEvent, UnityEvent> eventDictionary;

	private static EventManager eventManager;

	public static EventManager instance
	{
		get
		{
			if (!eventManager)
			{
				eventManager = Object.FindObjectOfType(typeof(EventManager)) as EventManager;
				if (!eventManager)
				{
					Debug.LogError("There needs to be one active EventManger script on a GameObject in your scene.");
				}
				else
				{
					eventManager.Init();
				}
			}
			return eventManager;
		}
	}

	private void Init()
	{
		if (eventDictionary == null)
		{
			eventDictionary = new Dictionary<MyEvent, UnityEvent>();
		}
	}

	public static void StartListening(MyEvent eventName, UnityAction listener)
	{
		UnityEvent value = null;
		if (instance.eventDictionary.TryGetValue(eventName, out value))
		{
			value.AddListener(listener);
			return;
		}
		value = new UnityEvent();
		value.AddListener(listener);
		instance.eventDictionary.Add(eventName, value);
	}

	public static void StopListening(MyEvent eventName, UnityAction listener)
	{
		if (!(eventManager == null))
		{
			UnityEvent value = null;
			if (instance.eventDictionary.TryGetValue(eventName, out value))
			{
				value.RemoveListener(listener);
			}
		}
	}

	public static void TriggerEvent(MyEvent eventName)
	{
		UnityEvent value = null;
		if (instance.eventDictionary.TryGetValue(eventName, out value))
		{
			value.Invoke();
		}
	}
}
