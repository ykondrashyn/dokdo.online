using UnityEngine;

public class DataImporter : MonoBehaviour
{
	private string[] stringList;

	private GameManager GM;

	private void Start()
	{
	}

	public void ImportLocalization()
	{
		LocalizeManager component = GetComponent<LocalizeManager>();
		int count = component.words.Count;
		TextAsset textAsset = (TextAsset)Resources.Load("local");
		string text = textAsset.text;
		stringList = text.Split('\n');
		component.words.Clear();
		int num = 0;
		for (int i = 0; i < stringList.Length; i++)
		{
			string[] array = stringList[i].Split(',');
			Word word = new Word();
			int num2 = 7;
			word.lang = new string[num2];
			for (int j = 0; j < num2; j++)
			{
				word.lang[j] = array[j];
			}
			num = array.Length;
			component.words.Add(word);
		}
		int count2 = component.words.Count;
		Debug.Log("번역 읽어오기 완료 > 기존 : " + count + "자 / 현재 : " + count2 + "자 " + (num - 1) + "개 국어");
	}
}
