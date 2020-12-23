using UnityEngine;

public class ChangeValuesManager : MonoBehaviour
{
	[SerializeField] private CubesMatrixManager myCubesMatrixManager = null;
	[SerializeField] private GameState myGameState					 = null;

	[SerializeField, Range(2, 12)] private int gridWidth		= 0;
	[SerializeField, Range(2, 6)]  private int gridHeight		= 0;
	[SerializeField, Range(1, 10)] private int minCubesToMatch	= 0;
	[SerializeField, Range(1, 99)] private int movesPerGame		= 0;

	[SerializeField, Header("Max amount of cubes in game (you can remove sprites)")] 
	private Sprite[] allSprites = new Sprite[0];

	void Awake()
	{
		CheckNulls();
		SetParameters();
	}

	private void SetParameters()
	{
		myCubesMatrixManager.GridWidth = gridWidth;
		myCubesMatrixManager.GridHeight = gridHeight;
		myCubesMatrixManager.MinCubesToMatch = minCubesToMatch;

		foreach (Sprite thisSprite in allSprites)
		{
			myCubesMatrixManager.AddSprites(thisSprite);
		}

		myGameState.PlayerRemainingMoves = movesPerGame;
	}

	private void CheckNulls()
	{
		if (!myCubesMatrixManager)
		{
			throw new System.NullReferenceException("MY CUBES MATRIX IS NULL ON CHANGE VALUES MANAGER!");
		}
		
		if (!myGameState)
		{
			throw new System.NullReferenceException("MY GAME STATE IS NULL ON CHANGE VALUES MANAGER!");
		}
	}
}
