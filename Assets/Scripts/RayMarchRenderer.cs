using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class RayMarchRenderer : MonoBehaviour
{
	public struct ShapeStruct
	{
		Matrix4x4 translateRotateMat;
		Matrix4x4 translateRotateMatInv;
		Vector3 size;
		Color color;
		ShapeType id;
		AlterationType alterId;
		int reflective;

		public ShapeStruct(Matrix4x4 trm, Matrix4x4 trmi, Vector3 sz, Color col, ShapeType t, AlterationType at, int r)
		{
			translateRotateMat = trm;
			translateRotateMatInv = trmi;
			color = col;
			size = sz;
			id = t;
			alterId = at;
			reflective = r;
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

	public struct LightStruct
	{
		Vector3 position;
		Vector3 direction;
		Color color;
		float range;
		float cosSpotAngle;
		LightType id;

		public LightStruct(Vector3 pos, Vector3 dir, Color c, float r, float csa, LightType t)
		{
			position = pos;
			direction = dir;
			color = c;
			range = r;
			cosSpotAngle = csa;
			id = t;
		}
	}

	public ComputeShader shader;
	public Camera realCamera;
	public Camera screenCamera;
	public GameObject screen;
	public float epsilon = 0.0001f;
	public float delta = 0.0001f;
	public float farPlane = 1000;
	public float fogDistance = 500;
	public Color fogColor = new Color(0.2f, 0.2f, 0.2f, 1);
	public int maxSteps = 128;
	public int maxBounces = 4;

	int rayMarchIndex = -1;
	Shape[] shapes;
	Light sceneLight;
	Vector2Int screenSize;
	RenderTexture renderTexture;
	ComputeBuffer shapeBuffer;
	ComputeBuffer cameraBuffer;
	ComputeBuffer lightBuffer;

	// Use this for initialization
	void Start()
	{
		// Get kernel indices
		rayMarchIndex = shader.FindKernel("RayMarch");
		
		// Get all shapes in the scene
		shapes = GameObject.FindObjectsOfType<Shape>();

		// Get all lights in the scene
		sceneLight = GameObject.FindObjectOfType<Light>();
	}

	// Update is called once per frame
	void Update()
	{
		if (rayMarchIndex != -1)
		{
			InitRenderTexture();
			DoRayMarch();
		}
	}

	void OnDestroy()
	{
		if (renderTexture)
		{
			renderTexture.Release();
			
			shapeBuffer.Dispose();
			cameraBuffer.Dispose();
			lightBuffer.Dispose();
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
			
				shapeBuffer.Dispose();
				cameraBuffer.Dispose();
				lightBuffer.Dispose();
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
			
			shapeBuffer = new ComputeBuffer(shapes.Length, Marshal.SizeOf(typeof(ShapeStruct)));
			cameraBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(CameraStruct)));
			lightBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(LightStruct)));
		}
	}

	void DoRayMarch()
	{
		// Send shapes to GPU
		ShapeStruct[] shapeStructs = new ShapeStruct[shapes.Length];
		for (int i = 0; i < shapes.Length; i++)
		{
			Shape shape = shapes[i];
			Matrix4x4 translateScaleMat = Matrix4x4.Translate(shape.transform.position) * Matrix4x4.Rotate(shape.transform.rotation);
			shapeStructs[i] = new ShapeStruct(
				translateScaleMat,
				Matrix4x4.Inverse(translateScaleMat),
				shape.transform.localScale/2,
				shape.color,
				shape.shapeType,
				shape.alterationType,
				shape.reflective ? 1 : 0);
		}
		shapeBuffer.SetData(shapeStructs);

		// Send camera to GPU
		CameraStruct camStruct = new CameraStruct(realCamera.transform.position, realCamera.transform.right, realCamera.transform.up, realCamera.transform.forward);
		cameraBuffer.SetData(new CameraStruct[1] { camStruct });

		// Send lights to GPU
		LightStruct lightStruct = new LightStruct(
			sceneLight.transform.position,
			sceneLight.transform.forward,
			sceneLight.color,
			sceneLight.range,
			Mathf.Cos(sceneLight.spotAngle * Mathf.Deg2Rad),
			sceneLight.type);
		lightBuffer.SetData(new LightStruct[1] { lightStruct });
		
		// CPU calculations
		float tan = Mathf.Tan(realCamera.fieldOfView * Mathf.Deg2Rad / 2);

		// Set shader data
		shader.SetBuffer(rayMarchIndex, "shapes", shapeBuffer);
		shader.SetBuffer(rayMarchIndex, "camera", cameraBuffer);
		shader.SetBuffer(rayMarchIndex, "light", lightBuffer);
		shader.SetInt("shapeCount", shapes.Length);
		shader.SetInts("screenSize", Screen.width, Screen.height);
		shader.SetFloat("tangent", tan);
		shader.SetFloat("aspect", realCamera.aspect);
		shader.SetFloat("epsilon", epsilon);
		shader.SetFloat("delta", delta);
		shader.SetFloat("farPlane", farPlane);
		shader.SetFloat("fogDistance", fogDistance);
		shader.SetFloats("fogColor", fogColor.r, fogColor.g, fogColor.b);
		shader.SetInt("maxSteps", maxSteps);
		shader.SetInt("maxBounces", maxBounces);

		// Run shader
		shader.Dispatch(rayMarchIndex, (Screen.width+31)/32, (Screen.height+15)/16, 1);
	}
}
