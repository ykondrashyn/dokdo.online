using UnityEngine;

public class DestroySelf : MonoBehaviour
{
	public float timeout = 0.5f;

	private float time;

	private void Update()
	{
		time += Time.deltaTime;
		if (time > timeout)
		{
			time = 0f;
			base.gameObject.SetActive(false);
		}
	}

	public void SetTimeOut(float t)
	{
		timeout = t;
	}

	public void ResetTime()
	{
		time = 0f;
	}
}
