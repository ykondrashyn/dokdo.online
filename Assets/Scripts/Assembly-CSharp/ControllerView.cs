using UnityEngine;

public class ControllerView : MonoBehaviour
{
	public Camera mainUICamera;

	public GameObject handleArea;

	public GameObject handleMove;

	public GameObject handleStick;

	public bool pressBool;

	private float handleRadius = 0.45f;

	private float handleRadiusLimit = 2f;

	public Vector3 handlePress;

	private bool handleAreaRemoveReduceBool;

	private bool handleAreaRemoveAnimationBool;

	private bool CanDrag;

	private Animation ani;

	private Vector3 tempZero = new Vector3(-100f, 0f, 0f);

	public void OnBtnClick()
	{
		ani.Play("handleControl_click");
		handleAreaRemoveAnimationBool = true;
	}

	public void OnBtnDrag()
	{
		if (Input.touchCount >= 2 || !CanDrag)
		{
			return;
		}
		handleAreaRemoveReduceBool = false;
		handleAreaRemoveAnimationBool = false;
		Vector3 mousePosition = Input.mousePosition;
		mousePosition = mainUICamera.ScreenToWorldPoint(mousePosition);
		Vector3 vector = mousePosition - handlePress;
		if (vector.magnitude > handleRadiusLimit)
		{
			vector = vector.normalized * handleRadiusLimit;
			mousePosition = handlePress + vector;
		}
		handleStick.transform.position = mousePosition;
		if (vector.magnitude > handleRadius)
		{
			handleArea.transform.right = new Vector3(0f - vector.normalized.x, 0f - vector.normalized.y, vector.normalized.z);
			handleMove.transform.position = mousePosition;
			return;
		}
		handleArea.transform.right = new Vector3(0f - vector.normalized.x, 0f - vector.normalized.y, vector.normalized.z);
		if (!(vector.normalized == new Vector3(0f, 0f, 0f)))
		{
			handleMove.transform.position = handlePress + vector.normalized * handleRadius;
		}
	}

	public void OnBtnPress()
	{
		if (!pressBool)
		{
			if (handleAreaRemoveReduceBool)
			{
				handleMove.transform.localPosition = tempZero;
			}
			pressBool = true;
			Vector3 mousePosition = Input.mousePosition;
			Vector3 position = mainUICamera.ScreenToWorldPoint(mousePosition);
			handleArea.transform.position = position;
			handleStick.transform.position = position;
			handleStick.SetActive(true);
			handleArea.SetActive(true);
			if (ani.isPlaying)
			{
				ani.Stop();
			}
			ani.Play("handleControl_appear");
			handlePress = handleArea.transform.position;
			CanDrag = true;
		}
	}

	public void OnBtnRelease()
	{
		if (pressBool)
		{
			pressBool = false;
			CanDrag = false;
			handleStick.SetActive(false);
			ani.Play("handleControl_disappear");
			handleAreaRemoveAnimationBool = true;
			handleAreaRemoveReduceBool = true;
		}
	}

	private void Start()
	{
		pressBool = false;
		handleStick.SetActive(false);
		handleArea.SetActive(false);
		handleAreaRemoveReduceBool = true;
		handleAreaRemoveAnimationBool = false;
		ani = handleArea.GetComponent<Animation>();
		CanDrag = false;
	}

	private void Update()
	{
		if (handleAreaRemoveReduceBool)
		{
			handleMove.transform.localPosition = Vector3.Lerp(handleMove.transform.localPosition, tempZero, 20f * Time.deltaTime);
		}
		if (!ani.isPlaying && handleAreaRemoveAnimationBool && !pressBool)
		{
			handleAreaRemoveReduceBool = false;
			handleAreaRemoveAnimationBool = false;
		}
	}
}
