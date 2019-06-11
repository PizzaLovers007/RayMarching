using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
	[Tooltip("Movement speed in units per second.")]
	public float moveSpeed = 2;

	[Tooltip("Turning speed in degrees per second.")]
	public float turnSpeed = 70;

	// Update is called once per frame
	void Update()
	{
		Vector3 moveDirection = Vector3.zero;
		if (Input.GetKey(KeyCode.W))
		{
			moveDirection += transform.forward;
		}
		if (Input.GetKey(KeyCode.S))
		{
			moveDirection -= transform.forward;
		}
		if (Input.GetKey(KeyCode.D))
		{
			moveDirection += transform.right;
		}
		if (Input.GetKey(KeyCode.A))
		{
			moveDirection -= transform.right;
		}
		if (Input.GetKey(KeyCode.E))
		{
			moveDirection += transform.up;
		}
		if (Input.GetKey(KeyCode.Q))
		{
			moveDirection -= transform.up;
		}

		if (Input.GetKey(KeyCode.LeftShift))
		{
			transform.position += moveDirection.normalized * 2 * moveSpeed * Time.deltaTime;
		}
		else
		{
			transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;
		}

		if (Input.GetKey(KeyCode.I))
		{
			transform.Rotate(transform.right, -turnSpeed * Time.deltaTime, Space.World);
		}
		if (Input.GetKey(KeyCode.K))
		{
			transform.Rotate(transform.right, turnSpeed * Time.deltaTime, Space.World);
		}
		if (Input.GetKey(KeyCode.L))
		{
			transform.Rotate(Vector3.up, turnSpeed * Time.deltaTime, Space.World);
		}
		if (Input.GetKey(KeyCode.J))
		{
			transform.Rotate(Vector3.up, -turnSpeed * Time.deltaTime, Space.World);
		}

		Debug.Log(transform.right);
	}
}
