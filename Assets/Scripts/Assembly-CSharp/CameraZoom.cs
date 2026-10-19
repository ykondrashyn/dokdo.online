using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class CameraZoom : MonoBehaviour
{
	private Camera camera0;

	public float orthoZoomSpeed = 0.03f;

	public float MaxSizePortrait;

	public float MaxSizeLandScape;

	public float DefaultSize;

	public float MinSize;

	private bool isPort;

	private ShipController SC;

	private Transform myT;

	private Vector3 firstPos;

	private UnityAction someListener;

	private void Awake()
	{
		camera0 = GetComponent<Camera>();
		SC = GameObject.Find("MyPos").GetComponent<ShipController>();
		myT = base.transform;
		firstPos = myT.localPosition;
		someListener = Shake;
	}

	private void OnEnable()
	{
		EventManager.StartListening(MyEvent.Attacked, someListener);
	}

	private void OnDisable()
	{
		EventManager.StopListening(MyEvent.Attacked, someListener);
	}

	private void Shake()
	{
		myT.DOKill();
		myT.localPosition = firstPos;
		myT.DOPunchPosition(Vector3.one * 0.3f, 0.5f);
	}

	private void LateUpdate()
	{
		if (!SC.isParking && Input.touchCount == 2 && Input.GetTouch(0).phase == TouchPhase.Moved && Input.GetTouch(1).phase == TouchPhase.Moved)
		{
			Touch touch = Input.GetTouch(0);
			Touch touch2 = Input.GetTouch(1);
			Vector2 vector = touch.position - touch.deltaPosition;
			Vector2 vector2 = touch2.position - touch2.deltaPosition;
			float magnitude = (vector - vector2).magnitude;
			float magnitude2 = (touch.position - touch2.position).magnitude;
			float num = magnitude - magnitude2;
			if (camera0.orthographic)
			{
				camera0.orthographicSize += num * orthoZoomSpeed;
				camera0.orthographicSize = Mathf.Clamp(camera0.orthographicSize, MinSize, MaxSizePortrait);
			}
		}
	}
}
