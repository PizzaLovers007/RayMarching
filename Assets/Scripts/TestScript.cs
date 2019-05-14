using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{
	public ComputeShader shader;

	int kernelIndex;

	// Use this for initialization
	void Start()
	{
		// Get kernel index
		kernelIndex = shader.FindKernel("CSMain");

		// Setup render texture
		RenderTexture renderTexture = new RenderTexture(256, 256, 24);
		renderTexture.enableRandomWrite = true;
		renderTexture.Create();
		
		// Give compute shader the render texture
		shader.SetTexture(kernelIndex, "Result", renderTexture);

		// Setup material for the object
		Material material = new Material(Shader.Find("Standard"));
		material.SetTexture("_MainTex", renderTexture);

		// Set the object's material
		GetComponent<MeshRenderer>().material = material;
	}

	// Update is called once per frame
	void Update()
	{
		shader.SetFloat("Time", Time.time);
		shader.SetInts("Size", 256, 256);
		shader.Dispatch(kernelIndex, 256/16, 256/16, 1);
	}
}
