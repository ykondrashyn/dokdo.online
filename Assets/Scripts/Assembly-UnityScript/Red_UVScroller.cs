using System;
using UnityEngine;

[Serializable]
public class Red_UVScroller : MonoBehaviour
{
	public int targetMaterialSlot;

	public float speedY;

	public float speedX;

	private float timeWentX;

	private float timeWentY;

	public Red_UVScroller()
	{
		speedY = 0.5f;
	}

	public virtual void Start()
	{
	}

	public virtual void Update()
	{
		timeWentY += Time.deltaTime * speedY;
		timeWentX += Time.deltaTime * speedX;
		GetComponent<Renderer>().materials[targetMaterialSlot].SetTextureOffset("_MainTex", new Vector2(timeWentX, timeWentY));
	}

	public virtual void OnEnable()
	{
		GetComponent<Renderer>().materials[targetMaterialSlot].SetTextureOffset("_MainTex", new Vector2(0f, 0f));
		timeWentX = 0f;
		timeWentY = 0f;
	}

	public virtual void Main()
	{
	}
}
