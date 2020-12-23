using UnityEngine;

public class ParticlesVFX : MonoBehaviour
{
	public delegate void ParticlePoolHandler(ParticleSystem thisParticle, PARTICLE_EFFECT particleType);
	public event ParticlePoolHandler OnParticleDeath = delegate { };

	[SerializeField] private PARTICLE_EFFECT thisType;

	private ParticleSystem myParticle = default(ParticleSystem);

	void Awake()
	{
		myParticle = this.GetComponent<ParticleSystem>();
	}

	void Update()
	{
		if (myParticle.isStopped)
		{
			OnParticleDeath(myParticle, thisType);
		}
	}
}
