using UnityEngine;
using System.Collections.Generic;

public class GameState : MonoBehaviour, INotifier
{
	[HideInInspector] public CubesMatrixManager matrixManager;
	
	private int playerScore				= 0;
	private int initialMoves			= 0;
    private int playerRemainingMoves	= 0;
	private int changePitchPercent		= 0;
	
	private List<IObserver> myObservers = new List<IObserver>();

	#region Getters/Setters
	public int PlayerRemainingMoves
	{
		get
		{
			return playerRemainingMoves;
		}
		set
		{
			initialMoves = value;
			playerRemainingMoves = value;
		}
	}

	public int PlayerScore
	{
		get
		{
			return playerScore;
		}
	}
	#endregion;

	void Start()
	{
		changePitchPercent = (25 * initialMoves) / 100;
	}

	public void SubtractAPlayerMove()
	{
		playerRemainingMoves--;
		playerRemainingMoves = Mathf.Clamp(playerRemainingMoves, 0, playerRemainingMoves + 1);

		foreach (IObserver observer in myObservers)
		{
			observer.OnNotify(TYPE_OF_NOTIFY.UpdateMoves);
		}

		if(playerRemainingMoves == changePitchPercent)
		{
			foreach (IObserver observer in myObservers)
			{
				observer.OnNotify(TYPE_OF_NOTIFY.ChangeMusicPitch);
			}
		}

		else if(playerRemainingMoves == 0)
		{
			foreach (IObserver observer in myObservers)
			{				
				observer.OnNotify(TYPE_OF_NOTIFY.ChangeMusicToNormalPitch);
			}
			matrixManager.RebuildGrid();
		}
	}

	public void AddPlayerScore(int numOfDestroyedCubes)
	{
		playerScore += CalculateFibonacciScore(numOfDestroyedCubes);
		
		foreach (IObserver observer in myObservers)
		{
			observer.OnNotify(TYPE_OF_NOTIFY.UpdateScore);
		}
	}

	public void SubscribeObserver(IObserver observer)
	{
		myObservers.Add(observer);
	}

	public void ResetAllStats()
	{
		playerScore = 0;
		playerRemainingMoves = initialMoves;

		foreach (IObserver observer in myObservers)
		{
			observer.OnNotify(TYPE_OF_NOTIFY.UpdateScore);
			observer.OnNotify(TYPE_OF_NOTIFY.UpdateMoves);
		}
	}

	private void TwentyFivePercentMovesRemaining()
	{
		foreach (IObserver observer in myObservers)
		{
			observer.OnNotify(TYPE_OF_NOTIFY.ChangeMusicPitch);
		}
	}

	private int CalculateFibonacciScore(int iterations)
	{
		int fibonacciResult = 1;
		int beforeNum = 0;
		int auxNum;

		for (int i = 1; i <= iterations; i++)
		{
			auxNum = fibonacciResult;
			fibonacciResult += beforeNum;
			beforeNum = auxNum;
		}

		return fibonacciResult;
	}

}
