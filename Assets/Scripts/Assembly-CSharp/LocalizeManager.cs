using System.Collections.Generic;
using Dokdo;
using UnityEngine;

public class LocalizeManager : MonoBehaviour
{
	public static LocalizeManager Instance;

	private static int COUNT;

	private int index;

	public string Language;

	public List<Word> words = new List<Word>();

	private Word basedata = new Word();

	public Dictionary<string, string> localized = new Dictionary<string, string>();

	public int currentLangNum;

	private void Awake()
	{
		index = COUNT;
		COUNT++;
		if (index == 0)
		{
			Instance = this;
			init();
		}
	}

	public void SaveLang()
	{
		if (SaveRuntime.Current != null)
		{
			SaveRuntime.Current.language = Language;
		}
		if (GameManager.Instance != null)
		{
			GameManager.Instance.Save();
		}
		init();
	}

	public void AddNew()
	{
		words.Add(basedata);
	}

	public void Remove()
	{
		words.RemoveAt(words.Count - 1);
	}

	public void init()
	{
		localized.Clear();
		Language = SaveRuntime.Current == null ||
			string.IsNullOrWhiteSpace(SaveRuntime.Current.language)
			? Application.systemLanguage.ToString()
			: SaveRuntime.Current.language;
		currentLangNum = 2;
		int num = 7;
		for (int i = 1; i < num; i++)
		{
			Lang lang = (Lang)i;
			string value = lang.ToString();
			if (Language.Equals(value))
			{
				currentLangNum = i;
			}
		}
		for (int j = 0; j < words.Count; j++)
		{
			localized.Add(words[j].lang[0], words[j].lang[currentLangNum]);
		}
	}

	public string translate(string id)
	{
		int num = indexOf(id);
		if (num.Equals(-1))
		{
			Debug.Log("Invalid word ID");
			return string.Empty;
		}
		return getTranslated(num);
	}

	private int indexOf(string id)
	{
		for (int i = 0; i < words.Count; i++)
		{
			if (words[i].lang[0].Equals(id))
			{
				return i;
			}
		}
		return -1;
	}

	private string getTranslated(int index)
	{
		Word word = words[index];
		return word.lang[currentLangNum];
	}
}
