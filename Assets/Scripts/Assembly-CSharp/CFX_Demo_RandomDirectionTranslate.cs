using UnityEngine;

public class CFX_Demo_RandomDirectionTranslate : MonoBehaviour
{
	public float speed = 30f;

	public Vector3 baseDir = Vector3.zero;

	public Vector3 axis = Vector3.forward;

	public bool gravity;

	private Vector3 dir;

	private void Start()
	{
		dir = new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f)).normalized;
		dir.Scale(axis);
		dir += baseDir;
	}

	private void Update()
	{
		base.transform.Translate(dir * speed * Time.deltaTime);
		if (gravity)
		{
			base.transform.Translate(Physics.gravity * Time.deltaTime);
		}
	}
}
