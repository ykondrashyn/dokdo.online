using System;
using UnityEngine;

[Serializable]
public class Red_rotateThis : MonoBehaviour
{
	public float rotationSpeedX;

	public float rotationSpeedY;

	public float rotationSpeedZ;

	public bool local;

	private Vector3 rotationVector;

	public Red_rotateThis()
	{
		rotationSpeedX = 90f;
		local = true;
		rotationVector = new Vector3(rotationSpeedX, rotationSpeedY, rotationSpeedZ);
	}

	public virtual void Update()
	{
		if (local)
		{
			transform.Rotate(new Vector3(rotationSpeedX, rotationSpeedY, rotationSpeedZ) * Time.deltaTime);
		}
		if (!local)
		{
			transform.Rotate(new Vector3(rotationSpeedX, rotationSpeedY, rotationSpeedZ) * Time.deltaTime, Space.World);
		}
	}

	public virtual void Main()
	{
	}
}
