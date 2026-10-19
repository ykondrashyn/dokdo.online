using UnityEngine;

public class AutoDisable : MonoBehaviour
{
	public float Timer;

	private float localT;

	private void OnEnable()
	{
		localT = Timer;
	}

	private void Update()
	{
		localT -= Time.deltaTime;
		if (localT <= 0f)
		{
			base.gameObject.SetActive(false);
		}
	}
}
