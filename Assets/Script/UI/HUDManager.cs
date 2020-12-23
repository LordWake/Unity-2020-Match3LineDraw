using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour, IObserver
{
	[SerializeField] private CubesMatrixManager myCubesMatrix	= default(CubesMatrixManager);
	[SerializeField] private GameState			myGameState		= default(GameState);

	[SerializeField] private Image onWaitText	= default(Image);
	[SerializeField] private Image rebuildText	= default(Image);
	[SerializeField] private Image exitBG		= default(Image);

	[SerializeField] private Text scoreText		= default(Text);
	[SerializeField] private Text movesText		= default(Text);

	private bool playerIsMovingCubes	= false;
	private bool rebuildingGrid			= false;

	void Start()
	{
		myCubesMatrix.SubscribeObserver(this);
		myGameState.SubscribeObserver(this);

		movesText.text = myGameState.PlayerRemainingMoves.ToString();
		scoreText.text = "0";

		onWaitText.enabled	= false;
		rebuildText.enabled = false;
		exitBG.gameObject.SetActive(false);
	}

	public void OnNotify(TYPE_OF_NOTIFY typeOfNotify)
	{
		switch(typeOfNotify)
		{
			case TYPE_OF_NOTIFY.UpdateMoves:
				movesText.text = myGameState.PlayerRemainingMoves.ToString();
				break;

			case TYPE_OF_NOTIFY.UpdateScore:
				scoreText.text = myGameState.PlayerScore.ToString();
				break;
			
			case TYPE_OF_NOTIFY.OnMoveCubesStart:	
				if(rebuildingGrid)
				{
					return;
				}

				playerIsMovingCubes = true;
				onWaitText.enabled = true;
				StartCoroutine(WhileMovingCubesHUDFeedBack());
				break;
			
			case TYPE_OF_NOTIFY.OnMoveCubesEnd:
				if (rebuildingGrid)
				{
					return;
				}

				playerIsMovingCubes = false;
				onWaitText.enabled = false;
				StopAllCoroutines();
				break;

			case TYPE_OF_NOTIFY.OnRebuildStart:
				rebuildingGrid		= true;
				onWaitText.enabled = false;

				playerIsMovingCubes = false;
				rebuildText.enabled = true;

				StopAllCoroutines();
				StartCoroutine(WhileRebuildingGrid());
				break;

			case TYPE_OF_NOTIFY.OnRebuildEnd:
				rebuildingGrid = false;
				StopAllCoroutines();
				rebuildText.enabled = false;
				break;
		}
	}

	#region Buttons Voids
	public void OnExitButtonClick()
	{
		exitBG.gameObject.SetActive(true);
	}

	public void OnExitConfirmationButtonClick()
	{
		Application.Quit();
	}

	public void OnExitCancelButtonClick()
	{
		exitBG.gameObject.SetActive(false);
	}
	#endregion;

	IEnumerator WhileMovingCubesHUDFeedBack()
	{
		while(playerIsMovingCubes)
		{
			yield return new WaitForSeconds(0.5f);
			onWaitText.enabled = false;
			yield return new WaitForSeconds(0.25f);
			onWaitText.enabled = true;
		}
	}

	IEnumerator WhileRebuildingGrid()
	{
		while (rebuildingGrid)
		{
			yield return new WaitForSeconds(0.5f);
			rebuildText.enabled = false;
			yield return new WaitForSeconds(0.25f);
			rebuildText.enabled = true;
		}
	}
}
