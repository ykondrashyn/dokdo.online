using UnityEngine;

public class GE_OrbitCamera : MonoBehaviour
{
	public enum ZoomMethod
	{
		PinchZoom = 1,
		SlideZoom = 2
	}

	public Transform m_Target;

	private float m_Distance = 10f;

	public float m_MinDistance = 5f;

	public float m_MaxDistance = 25f;

	public float m_XSpeed = 250f;

	public float m_YSpeed = 120f;

	public float m_ZoomSpeed = 5f;

	public bool m_XInvert;

	public bool m_YInvert;

	public bool m_ZoomInvert;

	public float m_YMinLimit = -20f;

	public float m_YMaxLimit = 70f;

	private float m_X;

	private float m_Y;

	private float m_OrbitSpeedDelayTime;

	public float m_OrbitSpeedMultiplier = 2f;

	private Vector2 m_CurrTouch1 = Vector2.zero;

	private Vector2 m_LastTouch1 = Vector2.zero;

	private Vector2 m_CurrTouch2 = Vector2.zero;

	private Vector2 m_LastTouch2 = Vector2.zero;

	private float m_CurrDist;

	private float m_LastDist;

	public ZoomMethod m_ZoomMethod = ZoomMethod.PinchZoom;

	private void Start()
	{
		base.gameObject.transform.LookAt(m_Target);
		Vector3 eulerAngles = base.transform.eulerAngles;
		m_X = eulerAngles.y;
		m_Y = eulerAngles.x;
		if (m_MaxDistance < m_MinDistance)
		{
			m_MaxDistance = m_MinDistance + 1f;
		}
		if (m_MinDistance > m_MaxDistance)
		{
			m_MinDistance = m_MaxDistance + 1f;
		}
		bool flag = false;
		m_Distance = Vector3.Distance(base.transform.position, m_Target.transform.position);
		if (m_Distance < m_MinDistance)
		{
			m_Distance = m_MinDistance;
			flag = true;
		}
		else if (m_Distance > m_MaxDistance)
		{
			m_Distance = m_MaxDistance;
			flag = true;
		}
		if (flag)
		{
			UpdatePosition();
		}
		if ((bool)GetComponent<Rigidbody>())
		{
			GetComponent<Rigidbody>().freezeRotation = true;
		}
	}

	private void Update()
	{
		if (!m_Target)
		{
			return;
		}
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		if (Input.touchCount == 2)
		{
			if (m_ZoomMethod == ZoomMethod.PinchZoom)
			{
				if (Input.touchCount == 2)
				{
					flag3 = true;
					flag = true;
					for (int i = 0; i < Input.touchCount; i++)
					{
						Touch touch = Input.GetTouch(i);
						if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
						{
							if (i == 0)
							{
								m_CurrTouch1 = touch.position;
								m_LastTouch1 = m_CurrTouch1 - touch.deltaPosition;
							}
							else
							{
								m_CurrTouch2 = touch.position;
								m_LastTouch2 = m_CurrTouch2 - touch.deltaPosition;
							}
						}
					}
					m_CurrDist = Vector2.Distance(m_CurrTouch1, m_CurrTouch2);
					m_LastDist = Vector2.Distance(m_LastTouch1, m_LastTouch2);
					float num = m_LastDist - m_CurrDist;
					if (m_ZoomInvert)
					{
						num *= -1f;
					}
					m_Distance += num * m_ZoomSpeed * 0.01f;
					if (m_Distance < m_MinDistance)
					{
						m_Distance = m_MinDistance;
					}
					else if (m_Distance > m_MaxDistance)
					{
						m_Distance = m_MaxDistance;
					}
				}
				else if (Input.touchCount < 2)
				{
					m_CurrDist = 0f;
					m_LastDist = 0f;
				}
			}
			else if (m_ZoomMethod == ZoomMethod.SlideZoom)
			{
				if (Input.touchCount == 2)
				{
					flag3 = true;
					flag = true;
					for (int j = 0; j < Input.touchCount; j++)
					{
						Touch touch2 = Input.GetTouch(j);
						if (touch2.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Stationary)
						{
							if (j == 0)
							{
								m_CurrTouch1 = touch2.position;
								m_LastTouch1 = m_CurrTouch1 - touch2.deltaPosition;
							}
							else
							{
								m_CurrTouch2 = touch2.position;
								m_LastTouch2 = m_CurrTouch2 - touch2.deltaPosition;
							}
						}
					}
					float num2 = Vector2.Distance(m_LastTouch1, m_LastTouch2) - Vector2.Distance(m_CurrTouch1, m_CurrTouch2);
					if (num2 > -2f && num2 < 2f)
					{
						flag3 = true;
						flag = true;
						m_CurrDist = (m_CurrTouch1.y + m_CurrTouch2.y) / 2f;
						m_LastDist = (m_LastTouch1.y + m_LastTouch2.y) / 2f;
						float num3 = m_LastDist - m_CurrDist;
						if (m_ZoomInvert)
						{
							num3 *= -1f;
						}
						m_Distance += num3 * m_ZoomSpeed * 0.01f;
						if (m_Distance < m_MinDistance)
						{
							m_Distance = m_MinDistance;
						}
						else if (m_Distance > m_MaxDistance)
						{
							m_Distance = m_MaxDistance;
						}
					}
				}
				else if (Input.touchCount < 2)
				{
					m_CurrDist = 0f;
					m_LastDist = 0f;
				}
			}
		}
		float num4 = 0.01f;
		if (!flag3 && (Input.GetAxis("Mouse ScrollWheel") < 0f - num4 || Input.GetAxis("Mouse ScrollWheel") > num4))
		{
			flag = true;
			float num5 = Input.GetAxis("Mouse ScrollWheel");
			if (m_ZoomInvert)
			{
				num5 *= -1f;
			}
			m_Distance -= num5 * m_ZoomSpeed;
			if (m_Distance < m_MinDistance)
			{
				m_Distance = m_MinDistance;
			}
			else if (m_Distance > m_MaxDistance)
			{
				m_Distance = m_MaxDistance;
			}
		}
		bool flag4 = false;
		if (Input.touchCount == 1)
		{
			flag4 = true;
			Touch touch3 = Input.GetTouch(0);
			if (touch3.phase == TouchPhase.Began)
			{
				m_OrbitSpeedDelayTime = 0f;
			}
			else if (touch3.phase == TouchPhase.Ended || touch3.phase == TouchPhase.Canceled)
			{
				m_OrbitSpeedDelayTime = 0f;
			}
			else if (touch3.phase == TouchPhase.Moved)
			{
				flag2 = true;
				float x = touch3.deltaPosition.x;
				float y = touch3.deltaPosition.y;
				if (m_XInvert)
				{
					x *= -1f;
				}
				if (m_YInvert)
				{
					y *= -1f;
				}
				m_X += touch3.deltaPosition.x * m_XSpeed * 0.005f * m_OrbitSpeedDelayTime;
				m_Y -= touch3.deltaPosition.y * m_YSpeed * 0.005f * m_OrbitSpeedDelayTime;
			}
		}
		if (!flag4)
		{
			if (Input.GetMouseButtonDown(0))
			{
				m_OrbitSpeedDelayTime = 0f;
			}
			else if (Input.GetMouseButtonUp(0))
			{
				m_OrbitSpeedDelayTime = 0f;
			}
			if (Input.GetMouseButton(0))
			{
				flag2 = true;
				float num6 = Input.GetAxis("Mouse X");
				float num7 = Input.GetAxis("Mouse Y");
				if (m_XInvert)
				{
					num6 *= -1f;
				}
				if (m_YInvert)
				{
					num7 *= -1f;
				}
				m_X += num6 * m_XSpeed * 0.02f * m_OrbitSpeedDelayTime;
				m_Y -= num7 * m_YSpeed * 0.02f * m_OrbitSpeedDelayTime;
			}
		}
		if (flag || flag2)
		{
			UpdatePosition();
		}
	}

	private void UpdatePosition()
	{
		m_OrbitSpeedDelayTime += Time.deltaTime * m_OrbitSpeedMultiplier;
		if (m_OrbitSpeedDelayTime > 1f)
		{
			m_OrbitSpeedDelayTime = 1f;
		}
		m_Y = ClampAngle(m_Y, m_YMinLimit, m_YMaxLimit);
		Quaternion quaternion = Quaternion.Euler(m_Y, m_X, 0f);
		Vector3 vector = new Vector3(0f, 0f, 0f - m_Distance);
		Vector3 position = quaternion * vector + m_Target.position;
		base.transform.rotation = quaternion;
		base.transform.position = position;
	}

	private static float ClampAngle(float angle, float min, float max)
	{
		if (angle < -360f)
		{
			angle += 360f;
		}
		if (angle > 360f)
		{
			angle -= 360f;
		}
		return Mathf.Clamp(angle, min, max);
	}

	public void SetZoomMethod(ZoomMethod zoomMethod)
	{
		m_ZoomMethod = zoomMethod;
	}
}
