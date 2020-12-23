using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PARTICLE_EFFECT
{
	FixCubeColorVFX, KillCubeVFX, SpawnCubeVFX
};

public enum SOUND_TYPE
{
	SelectedCube, UnSelectedCube, Match, 
	NoMatch, OnLevelComplete
};

public class PlayerModelView : MonoBehaviour, IObserver
{
	[SerializeField] private LineRenderer myLine			= new LineRenderer();

	[SerializeField] private Transform particlesContainer	= default(Transform);

	[SerializeField] private ParticleSystem[] myVFXs		= new ParticleSystem[3];

	private Pool<ParticleSystem> fixCubeVFXPool			= new Pool<ParticleSystem>(default(int), null, null);
	private Pool<ParticleSystem> killCubeVFXPool		= new Pool<ParticleSystem>(default(int), null, null);
	private Pool<ParticleSystem> spawnCubeVFXPool		= new Pool<ParticleSystem>(default(int), null, null);

	private List<Vector3> allPositionsToDrawLine		= new List<Vector3>();
	[SerializeField]private List<AudioClip> audioClips	= new List<AudioClip>();

	private AudioSource myAudioSrc		= default(AudioSource);
	private AudioSource cameraAudioSrc	= default(AudioSource);

	private const int initialParticlesAmount = 5;

	void Awake()
	{
		FindObjectOfType<GameState>().SubscribeObserver(this);
		
		myAudioSrc = GetComponent<AudioSource>();
		cameraAudioSrc = Camera.main.GetComponent<AudioSource>();

		fixCubeVFXPool	 = new Pool<ParticleSystem>(initialParticlesAmount, GetFixCubeVFX, OnGetParticle);
		killCubeVFXPool  = new Pool<ParticleSystem>(initialParticlesAmount, GetKillCubeVFX, OnGetParticle);
		spawnCubeVFXPool = new Pool<ParticleSystem>(initialParticlesAmount, GetSpawnCubeVFX, OnGetParticle);
	}

	public void DrawLineMatch(Vector3 cubePos)
	{
		allPositionsToDrawLine.Add(cubePos);
		myLine.positionCount = allPositionsToDrawLine.Count;
		Vector3 fixedVector = new Vector3(0, allPositionsToDrawLine[allPositionsToDrawLine.IndexOf(cubePos)].y, allPositionsToDrawLine[allPositionsToDrawLine.IndexOf(cubePos)].x);
		myLine.SetPosition(allPositionsToDrawLine.IndexOf(cubePos), fixedVector);
	}

	public void RemoveLastPointInDrawLine()
	{
		allPositionsToDrawLine.RemoveAt(allPositionsToDrawLine.Count - 1);
		myLine.positionCount = allPositionsToDrawLine.Count;
	}

	public void StopDrawing()
	{
		allPositionsToDrawLine.Clear();
		myLine.positionCount = allPositionsToDrawLine.Count;
	}

	public void SpawnVFX(PARTICLE_EFFECT vfx, Vector3 spawnPos)
	{
		ParticleSystem tempParticle = default(ParticleSystem);
		
		switch(vfx)
		{
			case PARTICLE_EFFECT.FixCubeColorVFX	: tempParticle = fixCubeVFXPool.GetObject();	break;
			case PARTICLE_EFFECT.KillCubeVFX		: tempParticle = killCubeVFXPool.GetObject();	break;
			case PARTICLE_EFFECT.SpawnCubeVFX		: tempParticle = spawnCubeVFXPool.GetObject();	break;
		}

		if(tempParticle != null)
		{
			tempParticle.transform.position = spawnPos;
		}
	}

	public void PlaySoundSFX(SOUND_TYPE thisSound)
	{
		myAudioSrc.Stop();
		myAudioSrc.PlayOneShot(audioClips[(int)thisSound]);
	}


	public void OnNotify(TYPE_OF_NOTIFY typeOfNotify)
	{
		switch(typeOfNotify)
		{
			case TYPE_OF_NOTIFY.ChangeMusicPitch:
				StartCoroutine(ChangeBackgroundMusicPitch(true));
				break;

			case TYPE_OF_NOTIFY.ChangeMusicToNormalPitch:
				StartCoroutine(ChangeBackgroundMusicPitch(false));
				break;
		}
	}

	IEnumerator ChangeBackgroundMusicPitch(bool higherPitch)
	{
		if(higherPitch)
		{
			while(cameraAudioSrc.pitch < 1.5f)
			{
				cameraAudioSrc.pitch += 0.1f;
				yield return new WaitForSeconds(0.25f);
			}
		}
		else
		{
			while (cameraAudioSrc.pitch > 1.0f)
			{
				cameraAudioSrc.pitch -= 0.1f;
				yield return new WaitForSeconds(0.25f);
			}
		}
	}

	#region POOL VOIDS
	private ParticleSystem GetFixCubeVFX()
	{
		ParticleSystem thisVFX = Instantiate(myVFXs[0]);
		thisVFX.transform.parent = particlesContainer;
		thisVFX.Stop();
		thisVFX.gameObject.SetActive(false);
		return thisVFX;
	}

	private ParticleSystem GetKillCubeVFX()
	{
		ParticleSystem thisVFX = Instantiate(myVFXs[1]);
		thisVFX.transform.parent = particlesContainer;
		thisVFX.Stop();
		thisVFX.gameObject.SetActive(false);
		return thisVFX;
	}

	private ParticleSystem GetSpawnCubeVFX()
	{
		ParticleSystem thisVFX = Instantiate(myVFXs[2]);
		thisVFX.transform.parent = particlesContainer;
		thisVFX.Stop();
		thisVFX.gameObject.SetActive(false);
		return thisVFX;
	}

	private void OnReleaseParticle(ParticleSystem thisVFX, PARTICLE_EFFECT typeOfVFX)
	{
		thisVFX.Stop();
		thisVFX.gameObject.GetComponent<ParticlesVFX>().OnParticleDeath -= OnReleaseParticle;
		thisVFX.gameObject.SetActive(false);

		switch (typeOfVFX)
		{
			case PARTICLE_EFFECT.FixCubeColorVFX	: fixCubeVFXPool.ReleaseObject(thisVFX);	break;
			case PARTICLE_EFFECT.KillCubeVFX		: killCubeVFXPool.ReleaseObject(thisVFX);	break;
			case PARTICLE_EFFECT.SpawnCubeVFX		: spawnCubeVFXPool.ReleaseObject(thisVFX);	break;
		}
	}

	private void OnGetParticle(ParticleSystem thisVFX)
	{
		thisVFX.gameObject.SetActive(true);
		thisVFX.Play();
		thisVFX.gameObject.GetComponent<ParticlesVFX>().OnParticleDeath += OnReleaseParticle;
	}
	#endregion;
}
