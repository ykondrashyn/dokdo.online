using System;
using UnityEngine;

[Serializable]
public class Red_trailFade : MonoBehaviour
{
	public float fadeInTime;

	public float stayTime;

	public float fadeOutTime;

	public TrailRenderer thisTrail;

	private float timeElapsed;

	private float timeElapsedLast;

	private float percent;

	public Red_trailFade()
	{
		fadeInTime = 0.1f;
		stayTime = 1f;
		fadeOutTime = 0.7f;
	}

	public virtual void Start()
	{
		thisTrail.material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 1f));
		if (!(fadeInTime >= 0.01f))
		{
			fadeInTime = 0.01f;
		}
		percent = timeElapsed / fadeInTime;
	}

	public virtual void Update()
	{
		timeElapsed += Time.deltaTime;
		if (!(timeElapsed > fadeInTime))
		{
			percent = timeElapsed / fadeInTime;
			thisTrail.material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, percent));
		}
		if (!(timeElapsed <= fadeInTime) && !(timeElapsed >= fadeInTime + stayTime))
		{
			thisTrail.material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 1f));
		}
		if (!(timeElapsed < fadeInTime + stayTime) && !(timeElapsed >= fadeInTime + stayTime + fadeOutTime))
		{
			timeElapsedLast += Time.deltaTime;
			percent = 1f - timeElapsedLast / fadeOutTime;
			thisTrail.material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, percent));
		}
	}

	public virtual void OnEnable()
	{
		thisTrail.material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 1f));
		timeElapsed = 0f;
		timeElapsedLast = 0f;
		percent = timeElapsed / fadeInTime;
	}

	public virtual void Main()
	{
	}
}
