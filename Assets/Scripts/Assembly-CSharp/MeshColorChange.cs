using UnityEngine;

public class MeshColorChange : MonoBehaviour
{
	private MeshRenderer mesh;

	private void Start()
	{
		mesh = GetComponent<MeshRenderer>();
		mesh.material.color = Color.blue;
	}

	private void Update()
	{
	}
}
