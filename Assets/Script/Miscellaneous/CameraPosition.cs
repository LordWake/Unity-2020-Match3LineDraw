using UnityEngine;

public class CameraPosition : MonoBehaviour
{
    public void SetCameraPosition(Vector3 position)
	{
		transform.position = new Vector3(position.x, position.y, -10);
	}
}
