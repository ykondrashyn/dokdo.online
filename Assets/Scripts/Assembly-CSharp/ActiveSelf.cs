using UnityEngine;

public class ActiveSelf : MonoBehaviour
{
	private Transform[] childs;

	private void Awake()
	{
		childs = GetComponentsInChildren<Transform>();
	}

	private void OnEnable()
	{
		for (int i = 0; i < childs.Length; i++)
		{
			childs[i].gameObject.SetActive(true);
		}
		MonoBehaviour.print(childs.Length + "@@@@@@@");
	}
}
