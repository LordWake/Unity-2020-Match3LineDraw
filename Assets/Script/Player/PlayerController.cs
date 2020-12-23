using UnityEngine;

public class PlayerController : MonoBehaviour
{
	[SerializeField] private CubesMatrixManager cubesMatrix;

	void Update()
	{
		CheckInputs();
	}

	private void CheckInputs()
	{
		if (Input.GetKey(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse0))
		{
			RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint
							   (Input.mousePosition), Vector2.zero);

			//We always know that the only thing with a Collider is gonna be a cube.
			if (hit.collider != null) 
			{
				cubesMatrix.PlayerStartSelectingCubes(hit.collider.gameObject);
			}
		}
		
		else if(Input.GetKeyUp(KeyCode.Mouse0))
		{
			cubesMatrix.PlayerStopSelectingCubes();
		}

		if (Input.GetKeyDown(KeyCode.Mouse1))
		{
			cubesMatrix.RefreshCubesColors();
		}
	}
}
