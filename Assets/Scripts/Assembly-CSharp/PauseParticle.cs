using UnityEngine;

public class PauseParticle : MonoBehaviour
{
	private ParticleSystem fx;

	private void Start()
	{
		fx = GetComponent<ParticleSystem>();
		fx.Pause();
	}

	private void Update()
	{
	}
}
