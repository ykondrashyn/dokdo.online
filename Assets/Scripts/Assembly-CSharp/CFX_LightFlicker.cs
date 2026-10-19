using UnityEngine;

[RequireComponent(typeof(Light))]
public class CFX_LightFlicker : MonoBehaviour
{
	public bool loop;

	public float smoothFactor = 1f;

	public float addIntensity = 1f;

	private float minIntensity;

	private float maxIntensity;

	private float baseIntensity;

	private void Awake()
	{
		baseIntensity = GetComponent<Light>().intensity;
	}

	private void OnEnable()
	{
		minIntensity = baseIntensity;
		maxIntensity = minIntensity + addIntensity;
	}

	private void Update()
	{
		GetComponent<Light>().intensity = Mathf.Lerp(minIntensity, maxIntensity, Mathf.PerlinNoise(Time.time * smoothFactor, 0f));
	}
}
