using DG.Tweening;
using UnityEngine;

public class DayAndNight : MonoBehaviour
{
	public Transform sun;

	public float DayTimeSpeed;

	private void Start()
	{
		sun.DOLocalRotate(new Vector3(360f, 0f, 0f), DayTimeSpeed, RotateMode.WorldAxisAdd).SetLoops(-1, LoopType.Incremental);
	}
}
