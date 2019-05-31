using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class RayMarchRenderer : MonoBehaviour
{
	public struct ShapeStruct
	{
		Vector3 position;
		Vector3 size;
		ShapeType id;

		public ShapeStruct(Vector3 pos, Vector3 sz, ShapeType t)
		{
			position = pos;
			size = sz;
			id = t;
		}
	}

	public struct RayStruct
	{
		Vector3 origin;
		Vector3 direction;

		public RayStruct(Vector3 o, Vector3 d)
		{
			origin = o;
			direction = d;
		}
	}

	public struct CameraStruct
	{
		Vector3 position;
		Vector3 right;
		Vector3 up;
		Vector3 forward;

		public CameraStruct(Vector3 pos, Vector3 r, Vector3 u, Vector3 f)
		{
			position = pos;
			right = r;
			up = u;
			forward = f;
		}
	}

	public ComputeShader shader;
	public Camera realCamera;
	public Camera screenCamera;
	public GameObject screen;

	int rayMarchIndex = -1;
	Shape[] shapes;
	Vector2Int screenSize;
	RenderTexture renderTexture;

	// Use this for initialization
	void Start()
	{
		// Get kernel indices
		rayMarchIndex = shader.FindKernel("RayMarch");
		
		// Get all shapes in the scene
		shapes = GameObject.FindObjectsOfType<Shape>();
	}

	// Update is called once per frame
	void Update()
	{
		InitRenderTexture();
		if (rayMarchIndex != -1)
		{
			DoRayMarch();
		}
	}

	void InitRenderTexture()
	{
		if (renderTexture == null || screenSize.x != Screen.width || screenSize.y != Screen.height)
		{
			// Delete old texture
			if (renderTexture)
			{
				renderTexture.Release();
			}

			// Screen size changed, update texture size
			screenSize = new Vector2Int(Screen.width, Screen.height);

			// Setup render texture
			renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
			renderTexture.enableRandomWrite = true;
			renderTexture.Create();
			
			// Give compute shader the render texture
			shader.SetTexture(rayMarchIndex, "outTexture", renderTexture);

			// Create screen material
			Material screenMaterial = new Material(Shader.Find("Unlit/Texture"));
			screenMaterial.SetTexture("_MainTex", renderTexture);
			screen.GetComponent<MeshRenderer>().material = screenMaterial;
		}
	}

	void DoRayMarch()
	{
		// Create buffer to send to shader
		ShapeStruct[] shapeStructs = new ShapeStruct[shapes.Length];
		for (int i = 0; i < shapes.Length; i++)
		{
			Shape shape = shapes[i];
			shapeStructs[i] = new ShapeStruct(shape.transform.position, shape.transform.localScale/2, shape.shapeType);
		}
		ComputeBuffer shapeBuffer = new ComputeBuffer(shapes.Length, Marshal.SizeOf(typeof(ShapeStruct)));
		shapeBuffer.SetData(shapeStructs);
		ComputeBuffer cameraBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(CameraStruct)));
		CameraStruct camStruct = new CameraStruct(realCamera.transform.position, realCamera.transform.right, realCamera.transform.up, realCamera.transform.forward);
		cameraBuffer.SetData(new CameraStruct[1] { camStruct });
		
		// CPU calculations
		float tan = Mathf.Tan(realCamera.fieldOfView * Mathf.Deg2Rad / 2);

		// Set shader data
		shader.SetBuffer(rayMarchIndex, "shapes", shapeBuffer);
		shader.SetBuffer(rayMarchIndex, "camera", cameraBuffer);
		shader.SetInt("shapeCount", shapes.Length);
		shader.SetInts("screenSize", Screen.width, Screen.height);
		shader.SetFloat("tangent", tan);
		shader.SetFloat("aspect", realCamera.aspect);

		// Run shader
		shader.Dispatch(rayMarchIndex, (Screen.width+31)/32, (Screen.height+15)/16, 1);

		shapeBuffer.Dispose();
		cameraBuffer.Dispose();
	}
}
