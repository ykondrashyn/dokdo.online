using UnityEngine;

public class AutoIntensity : MonoBehaviour
{
	public float DayTime;

	[Range(0f, 300f)]
	public float localT;

	public Gradient nightDayColor;

	public Gradient CamColor;

	public float maxIntensity = 1f;

	public float minIntensity;

	public float maxAmbient = 1f;

	public float minAmbient;

	public float dayAtmosphereThickness = 2f;

	public float nightAtmosphereThickness;

	public Camera CAM;

	private Light mainLight;

	private Skybox sky;

	private Material skyMat;

	[SerializeField]
	private float dot;

	private bool c;

	private ShipController SC;

	private void Start()
	{
		SC = GameObject.Find("MyPos").GetComponent<ShipController>();
		mainLight = GetComponent<Light>();
		skyMat = RenderSettings.skybox;
		localT = DayTime;
		c = true;
	}

	private void Update()
	{
		if (SC.isParking)
		{
			localT = Mathf.Lerp(localT, DayTime, Time.deltaTime);
		}
		else if (c)
		{
			localT -= Time.deltaTime;
			if (localT <= 0f)
			{
				c = false;
			}
		}
		else
		{
			localT += Time.deltaTime;
			if (localT >= DayTime)
			{
				c = true;
			}
		}
		dot = localT / DayTime;
		mainLight.intensity = (maxIntensity - minIntensity) * dot + minIntensity;
		RenderSettings.ambientIntensity = (maxAmbient - minAmbient) * dot + minAmbient;
		mainLight.color = nightDayColor.Evaluate(dot);
		CAM.backgroundColor = CamColor.Evaluate(dot);
		RenderSettings.ambientLight = mainLight.color;
		skyMat.SetFloat("_AtmosphereThickness", (dayAtmosphereThickness - nightAtmosphereThickness) * dot + nightAtmosphereThickness);
	}
}
