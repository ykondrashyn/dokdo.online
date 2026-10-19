using System;
using UnityEngine;

[Serializable]
public class Red_animSpeedRandomizer : MonoBehaviour
{
	public float minSpeed;

	public float maxSpeed;

	public Red_animSpeedRandomizer()
	{
		minSpeed = 0.7f;
		maxSpeed = 1.5f;
	}

	public virtual void Start()
	{
		GetComponent<Animation>()[GetComponent<Animation>().clip.name].speed = UnityEngine.Random.Range(minSpeed, maxSpeed);
	}

	public virtual void Main()
	{
	}
}
