using UnityEngine;

public class EffectList : MonoBehaviour
{
	public GameObject[] FxList;

	private void OnEnable()
	{
		for (int i = 0; i < FxList.Length; i++)
		{
			FxList[i].SetActive(false);
		}
	}

	public void init(float p)
	{
		if (p <= 0.7f)
		{
			FxList[0].SetActive(true);
		}
		else
		{
			FxList[0].SetActive(false);
		}
		if (p <= 0.5f)
		{
			FxList[1].SetActive(true);
		}
		else
		{
			FxList[1].SetActive(false);
		}
		if (p <= 0.3f)
		{
			FxList[2].SetActive(true);
		}
		else
		{
			FxList[2].SetActive(false);
		}
	}
}
