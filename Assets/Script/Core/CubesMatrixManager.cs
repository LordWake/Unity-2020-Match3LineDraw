using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubesMatrixManager : MonoBehaviour, INotifier
{
	private enum TYPE_OF_CUBE
	{
		CubeOne, CubeTwo, CubeThree, CubeFour, CubeFive, CubeSix,
		CubeSeven, CubeEight, CubeNine, CubeTen, CubeEleven
	};

	public delegate void OnSelectedCubes();
	public event OnSelectedCubes OnPlayerSelected = delegate { };

	private Pool<GameObject> pool									= new Pool<GameObject>(default(int), null, null);
	[SerializeField] private PlayerModelView modelView				= default(PlayerModelView);
	[SerializeField] private GameState		 myGameState			= default(GameState);
	
	private IObserver myUIObserver									= default(IObserver);

	private Dictionary<GameObject, TYPE_OF_CUBE> allCubesAndTypes	= new Dictionary<GameObject, TYPE_OF_CUBE>();

	private List<Sprite> allSpritesList								= new List<Sprite>();
	private List<GameObject> selectedCubes							= new List<GameObject>();
	private GameObject[,] cubesMatrix								= new GameObject[0, 0];

	[SerializeField] private GameObject gridBackGround				= default(GameObject);
	[SerializeField] private GameObject cubeSprite					= default(GameObject);

	[SerializeField] private Transform gridBGCointainer				= default(Transform);
	[SerializeField] private Transform coloredCubesContainer		= default(Transform);

	private Vector3 midPoint	= new Vector3(0, 0, 0);

	private int gridWidth		= 0;
	private int gridHeight		= 0;
	private int cubeNum			= 0;
	private int minCubesToMatch = 0;

	private const int initialCubesAmount = 10;

	private const float distanceBetweenCubes	= 1.4f;
	private const float movementSpeed			= 30.0f;
	private const float rotationSpeed			= 500.0f;

	private bool onFixingGrid	= false;
	private bool rebuildingGrid = false;

	#region Getters / Setters
	public int GridWidth
	{
		set
		{
			gridWidth = value;
		}
	}

	public int GridHeight
	{
		set
		{
			gridHeight = value;
		}
	}

	public int MinCubesToMatch
	{
		set
		{
			minCubesToMatch = value;
		}
	}

	public void AddSprites(Sprite newSprite)
	{
		allSpritesList.Add(newSprite);
	}
	#endregion;

	void Awake()
	{ 
		pool = new Pool<GameObject>(initialCubesAmount, GetCube, OnGetCube);
		myGameState.matrixManager = this;
	}

	void Start()
	{
		SpawnAllCubesGridBackground(distanceBetweenCubes);
		ChangeCameraPosition();
		FixCubesColors();
	}

	void Update()
	{
		OnPlayerSelected();
	}

	public void PlayerStartSelectingCubes(GameObject cube)
	{
		if (onFixingGrid)
		{
			return;
		}

		if (selectedCubes.Count == 0)
		{
			selectedCubes.Add(cube);
			modelView.DrawLineMatch(cube.transform.position);
			modelView.PlaySoundSFX(SOUND_TYPE.SelectedCube);
			OnPlayerSelected += MoveSelectedCubesFeedBack;
		}

		else
		{
			int yIndex			= 0;
			int oldCube_Y_Index = 0;

			int oldCube_X_Index = ReturnMeCubeIndex(selectedCubes[selectedCubes.Count - 1], out oldCube_Y_Index);
			int xIndex			= ReturnMeCubeIndex(cube, out yIndex);

			if (!selectedCubes.Contains(cube))
			{
				//Check adjacent in Matrix.
				if (xIndex == oldCube_X_Index - 1 && yIndex == oldCube_Y_Index || xIndex == oldCube_X_Index + 1 && yIndex == oldCube_Y_Index ||
					yIndex == oldCube_Y_Index - 1 && xIndex == oldCube_X_Index || yIndex == oldCube_Y_Index + 1 && xIndex == oldCube_X_Index)
				{
					//All right, we don't have add this cube before and is a neighbor. Now, we check if this new cube is the same type 
					//as the last one.
					if (allCubesAndTypes[cube] == allCubesAndTypes[selectedCubes[selectedCubes.Count - 1]])
					{
						selectedCubes.Add(cube);
						modelView.PlaySoundSFX(SOUND_TYPE.SelectedCube);
						modelView.DrawLineMatch(cube.transform.position);
					}
				}
			}

			else
			{
				if (selectedCubes.Count > 1)
				{
					if (cube == selectedCubes[selectedCubes.Count - 2])
					{
						selectedCubes[selectedCubes.Count - 1].transform.eulerAngles = new Vector3(0, 0, 0);
						selectedCubes.RemoveAt(selectedCubes.Count - 1);
						modelView.RemoveLastPointInDrawLine();
						modelView.PlaySoundSFX(SOUND_TYPE.UnSelectedCube);
					}
				}
			}
		}
	}

	public void PlayerStopSelectingCubes()
	{
		OnPlayerSelected -= MoveSelectedCubesFeedBack;
		
		foreach (GameObject cube in selectedCubes)
		{
			cube.transform.eulerAngles = new Vector3(0, 0, 0);
		}

		if (selectedCubes.Count >= minCubesToMatch)
		{
			foreach (GameObject cube in selectedCubes)
			{
				OnReleaseCube(cube);
			}

			modelView.PlaySoundSFX(SOUND_TYPE.Match);
			
			myGameState.SubtractAPlayerMove();
			myGameState.AddPlayerScore(selectedCubes.Count);

			StartCoroutine(ReOrderAllCubes());
		}

		else
		{
			modelView.PlaySoundSFX(SOUND_TYPE.NoMatch);
		}

		selectedCubes.Clear();
		modelView.StopDrawing();
	}

	public void RefreshCubesColors()
	{
		//Called from PlayerController.
		if (onFixingGrid)
		{
			return;
		}
		
		allCubesAndTypes.Clear();
		for (int i = 0; i < cubesMatrix.GetLength(0); i++)
		{
			for (int j = 0; j < cubesMatrix.GetLength(1); j++)
			{
				int randomValue = Random.Range(0, allSpritesList.Count);
				cubesMatrix[i, j].GetComponent<SpriteRenderer>().sprite = allSpritesList[randomValue];
				allCubesAndTypes.Add(cubesMatrix[i, j], (TYPE_OF_CUBE)randomValue);
			}
		}

		FixCubesColors();
	}

	public void SubscribeObserver(IObserver observer)
	{
		myUIObserver = observer;
	}

	public void RebuildGrid()
	{
		rebuildingGrid = true;
		onFixingGrid = true;
		
		StopAllCoroutines();
		
		myUIObserver.OnNotify(TYPE_OF_NOTIFY.OnRebuildStart);
		modelView.PlaySoundSFX(SOUND_TYPE.OnLevelComplete);

		StartCoroutine(RebuildTheWholeGrid());
	}

	private void SpawnAllCubesGridBackground(float distBetween)
	{
		cubesMatrix		= new GameObject[gridWidth, gridHeight];
		float xPosition = 0.0f;
		float yPosition = 0.0f;
		cubeNum			= 0;

		for (int i = 0; i < gridWidth; i++)
		{
			for (int j = 0; j < gridHeight; j++)
			{
				GameObject tempGridBG = Instantiate(gridBackGround,
													transform.position + new Vector3(xPosition, yPosition - j * distBetween, 0),
													Quaternion.identity);

				cubeNum++;
				tempGridBG.transform.parent = gridBGCointainer;
				tempGridBG.gameObject.name = "Background " + cubeNum;

				SpawnCube(tempGridBG.transform.position, i, j);

				midPoint += tempGridBG.transform.position;
			}
			xPosition += distBetween;
		}
	}

	private void SpawnCube(Vector3 cubePosition, int i, int j)
	{
		int randomValue = Random.Range(0, allSpritesList.Count);

		GameObject tempCube			= pool.GetObject();
		tempCube.transform.position = cubePosition;
		tempCube.name				= "Cube " + cubeNum.ToString();
		cubesMatrix[i,j]			= tempCube;

		tempCube.GetComponent<SpriteRenderer>().sprite = allSpritesList[randomValue];
		
		allCubesAndTypes.Add(tempCube, (TYPE_OF_CUBE)randomValue);
	}

	private void FixCubesColors()
	{
		#region Horizontal Colors
		for (int i = 0; i < gridWidth; i++)
		{
			for (int j = 0; j < gridHeight; j++)
			{
				if (i + 1 < gridWidth - 1)
				{
					if (allCubesAndTypes[cubesMatrix[i, j]]		== allCubesAndTypes[cubesMatrix[i + 1, j]] && 
						allCubesAndTypes[cubesMatrix[i + 1, j]] == (allCubesAndTypes[cubesMatrix[i + 2, j]]))
					
					{
						int newRandomValue = GiveMeANewColor((int)allCubesAndTypes[cubesMatrix[i, j]]);
						cubesMatrix[i, j].gameObject.GetComponent<SpriteRenderer>().sprite = allSpritesList[newRandomValue];
						allCubesAndTypes[cubesMatrix[i, j]] = (TYPE_OF_CUBE)newRandomValue;
						modelView.SpawnVFX(PARTICLE_EFFECT.FixCubeColorVFX, cubesMatrix[i, j].transform.position);
					}
				}
			}
		}
		#endregion;

		#region Vertical Colors
		for (int i = 0; i < gridWidth; i++)
		{
			for (int j = 0; j < gridHeight; j++)
			{
				if (j + 1 < gridHeight - 1)
				{
					if (allCubesAndTypes[cubesMatrix[i, j]]		== allCubesAndTypes[cubesMatrix[i, j + 1]] && 
						allCubesAndTypes[cubesMatrix[i, j + 1]] == (allCubesAndTypes[cubesMatrix[i, j + 2]]))
					
					{
						int newRandomValue = GiveMeANewColor((int)allCubesAndTypes[cubesMatrix[i, j]]);
						cubesMatrix[i, j].gameObject.GetComponent<SpriteRenderer>().sprite = allSpritesList[newRandomValue];
						allCubesAndTypes[cubesMatrix[i, j]] = (TYPE_OF_CUBE)newRandomValue;
						modelView.SpawnVFX(PARTICLE_EFFECT.FixCubeColorVFX, cubesMatrix[i, j].transform.position);
					}
				}
			}
		}
		#endregion;
	}

	private void ChangeCameraPosition()
	{
		Camera.main.GetComponent<CameraPosition>().SetCameraPosition(midPoint /= cubesMatrix.Length);
	}

	private void CheckForNewCombinations()
	{
		if(rebuildingGrid)
		{
			return;
		}

		bool foundACombination = false;

		for (int i = 1; i < gridWidth - 1; i++)
		{
			for (int j = 1; j < gridHeight - 1; j++)
			{
				GameObject currentCube = cubesMatrix[i, j];

				GameObject nextCube = cubesMatrix[i + 1, j];
				GameObject beforeCube = cubesMatrix[i - 1, j];

				GameObject aboveCube = cubesMatrix[i, j + 1];
				GameObject belowCube = cubesMatrix[i, j - 1];

				#region Horizontal Check
				if (currentCube.activeInHierarchy && nextCube.activeInHierarchy && beforeCube.activeInHierarchy)
				{
					if (allCubesAndTypes[currentCube] == allCubesAndTypes[beforeCube] &&
						allCubesAndTypes[beforeCube] == allCubesAndTypes[nextCube])
					{
						foundACombination = true;
						for (int k = i - 1; k <= i + 1; k++)
						{
							OnReleaseCube(cubesMatrix[k, j]);
						}
						modelView.PlaySoundSFX(SOUND_TYPE.Match);
						myGameState.AddPlayerScore(3);
					}
				}
				#endregion;

				#region Vertical Check
				if (currentCube.activeInHierarchy && aboveCube.activeInHierarchy && belowCube.activeInHierarchy)
				{
					if (allCubesAndTypes[currentCube] == allCubesAndTypes[aboveCube] &&
						allCubesAndTypes[aboveCube] == allCubesAndTypes[belowCube])
					{
						foundACombination = true;
						for (int k = j - 1; k <= j + 1; k++)
						{
							OnReleaseCube(cubesMatrix[i, k]);
						}
						modelView.PlaySoundSFX(SOUND_TYPE.Match);
						myGameState.AddPlayerScore(3);
					}
				}
				#endregion;
			}
		}

		#region Floor Check
		for (int i = 1; i < gridWidth - 1; i++)
		{
			GameObject currentCube = cubesMatrix[i, gridHeight - 1];

			GameObject nextCube = cubesMatrix[i + 1, gridHeight - 1];
			GameObject beforeCube = cubesMatrix[i - 1, gridHeight - 1];

			if (currentCube.activeInHierarchy && nextCube.activeInHierarchy && beforeCube.activeInHierarchy)
			{
				if (allCubesAndTypes[currentCube] == allCubesAndTypes[beforeCube] &&
					allCubesAndTypes[beforeCube] == allCubesAndTypes[nextCube])
				{
					foundACombination = true;
					for (int k = i - 1; k <= i + 1; k++)
					{
						OnReleaseCube(cubesMatrix[k, gridHeight - 1]);
					}
					modelView.PlaySoundSFX(SOUND_TYPE.Match);
					myGameState.AddPlayerScore(3);
				}
			}
		}
		#endregion;

		if (foundACombination)
		{
			StartCoroutine(ReOrderAllCubes());
		}

		else
		{
			StartCoroutine(ReFillEmptySpaces());
		}
	}

	private void MoveSelectedCubesFeedBack()
	{
		foreach (GameObject cube in selectedCubes)
		{
			cube.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
		}
	}

	private int ReturnMeCubeIndex(GameObject cube, out int secondIndex)
	{
		for (int i = 0; i < cubesMatrix.GetLength(0); i++)
		{
			for (int j = 0; j < cubesMatrix.GetLength(1); j++)
			{
				if(cubesMatrix[i,j] == cube)
				{
					secondIndex = j;
					return i;
				}
			}		
		}

		secondIndex = 0;
		return 0;
	}

	private int GiveMeANewColor(int doNotReturnThisValue)
	{
		int randomInt = Random.Range(0, allSpritesList.Count);

		int returnThisValue = randomInt == doNotReturnThisValue ? GiveMeANewColor(randomInt) : randomInt;
		
		return returnThisValue;
	}

	IEnumerator ReOrderAllCubes()
	{
		myUIObserver.OnNotify(TYPE_OF_NOTIFY.OnMoveCubesStart);
		onFixingGrid = true;
		
		//We repeat this "X" times with the Matrix height to be sure that we've re-order all cubes.
		for (int i = 0; i < gridHeight; i++)
		{
			for (int j = gridHeight - 1; j > 0; j--)
			{
				for (int k = 0; k < gridWidth; k++)
				{
					if (!cubesMatrix[k, j].gameObject.activeInHierarchy)
					{
						if (cubesMatrix[k, j - 1].gameObject.activeInHierarchy)
						{
							//We swap between the cube above and the one that's been destroyed.
							GameObject destroyedCube = cubesMatrix[k, j];
							GameObject aboveCube = cubesMatrix[k, j - 1];

							Vector3 destroyedCubePosition = destroyedCube.transform.position;
							Vector3 aboveCubePosition = aboveCube.transform.position;
							
							while(cubesMatrix[k, j - 1].gameObject.transform.position != destroyedCubePosition)
							{
								//Smooth Movement effect.
								Vector3 currenPos = cubesMatrix[k, j - 1].gameObject.transform.position;
								
								cubesMatrix[k, j - 1].gameObject.transform.position = Vector3.MoveTowards(currenPos, 
																						destroyedCubePosition, 
																						movementSpeed * Time.deltaTime);
								yield return new WaitForEndOfFrame();
							}

							cubesMatrix[k, j].gameObject.transform.position = aboveCubePosition;

							cubesMatrix[k, j] = aboveCube;
							cubesMatrix[k, j - 1] = destroyedCube;
							
							yield return new WaitForSeconds(0.05f);
						}
					}
				}
			}
		}

		yield return new WaitForEndOfFrame();
		CheckForNewCombinations();
	}
	
	IEnumerator ReFillEmptySpaces()
	{
		for (int i = 0; i < cubesMatrix.GetLength(0); i++)
		{
			for (int j = 0; j < cubesMatrix.GetLength(1); j++)
			{
				if (!cubesMatrix[i, j].activeInHierarchy)
				{
					modelView.SpawnVFX(PARTICLE_EFFECT.SpawnCubeVFX, cubesMatrix[i, j].transform.position);
					cubesMatrix[i, j].SetActive(true);
					int randomValue = Random.Range(0, allSpritesList.Count);
					cubesMatrix[i, j].GetComponent<SpriteRenderer>().sprite = allSpritesList[randomValue];
					allCubesAndTypes[cubesMatrix[i, j]] = (TYPE_OF_CUBE)randomValue;
				}

				yield return new WaitForSeconds(0.01f);
			}
		}

		yield return new WaitForEndOfFrame();
		
		FixCubesColors();
		
		yield return new WaitForSeconds(0.1f);
		
		onFixingGrid = false;
		myUIObserver.OnNotify(TYPE_OF_NOTIFY.OnMoveCubesEnd);
	}
	
	IEnumerator RebuildTheWholeGrid()
	{
		yield return new WaitForSeconds(1.5f);
		allCubesAndTypes.Clear();

		for (int i = 0; i < cubesMatrix.GetLength(0); i++)
		{
			for (int j = 0; j < cubesMatrix.GetLength(1); j++)
			{
				cubesMatrix[i, j].gameObject.SetActive(false);
				modelView.SpawnVFX(PARTICLE_EFFECT.KillCubeVFX, cubesMatrix[i, j].gameObject.transform.position);
				yield return new WaitForSeconds(0.05f);
			}		
		}

		for (int i = 0; i < cubesMatrix.GetLength(0); i++)
		{
			for (int j = 0; j < cubesMatrix.GetLength(1); j++)
			{
				int randomValue = Random.Range(0, allSpritesList.Count);
				modelView.SpawnVFX(PARTICLE_EFFECT.SpawnCubeVFX, cubesMatrix[i, j].gameObject.transform.position);
				
				cubesMatrix[i, j].gameObject.SetActive(true);
				cubesMatrix[i, j].GetComponent<SpriteRenderer>().sprite = allSpritesList[randomValue];
				
				allCubesAndTypes.Add(cubesMatrix[i, j], (TYPE_OF_CUBE)randomValue);
				
				yield return new WaitForSeconds(0.05f);
			}
		}

		FixCubesColors();
		
		yield return new WaitForSeconds(0.5f);

		myUIObserver.OnNotify(TYPE_OF_NOTIFY.OnRebuildEnd);
		myGameState.ResetAllStats();
		
		rebuildingGrid	= false;
		onFixingGrid	= false;
	}

	#region POOL VOIDS
	private GameObject GetCube()
	{
		GameObject thisCube = Instantiate(cubeSprite);
		thisCube.transform.parent = coloredCubesContainer;
		thisCube.gameObject.SetActive(false);
		return thisCube;
	}

	private void OnReleaseCube(GameObject cube)
	{
		cube.gameObject.SetActive(false);
		modelView.SpawnVFX(PARTICLE_EFFECT.KillCubeVFX, cube.transform.position);
		pool.ReleaseObject(cube);
	}

	private void OnGetCube(GameObject cube)
	{
		cube.gameObject.SetActive(true);
	}
	#endregion;
}
