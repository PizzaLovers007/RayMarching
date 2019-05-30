using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenQuad : MonoBehaviour
{
	public new Camera camera;

	// Update is called once per frame
	void Update()
	{
		transform.localScale = new Vector3(camera.orthographicSize * camera.aspect * 2, camera.orthographicSize * 2, 1);
	}
}
