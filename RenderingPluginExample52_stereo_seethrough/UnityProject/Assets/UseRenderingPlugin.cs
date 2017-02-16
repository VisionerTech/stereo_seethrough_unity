// on OpenGL ES there is no way to query texture extents from native texture id
#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
	#define UNITY_GLES_RENDERER
#endif


using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;


public class UseRenderingPlugin : MonoBehaviour
{
	public static event Action initSuccessEvent;
	// Native plugin rendering events are only called if a plugin is used
	// by some script. This means we have to DllImport at least
	// one function in some active script.
	// For this example, we'll call into plugin's SetTimeFromUnity
	// function and pass the current time so the plugin can animate.

#if UNITY_IPHONE && !UNITY_EDITOR
	[DllImport ("__Internal")]
#else
	[DllImport ("RenderingPlugin")]
#endif
	private static extern void SetTimeFromUnity(float t);

	//native function for open web cam.
#if UNITY_IPHONE && !UNITY_EDITOR
	[DllImport ("__Internal")]
#else
	[DllImport ("RenderingPlugin")]
#endif
	private static extern Boolean OpenCamera();

	//native function call for destroying web cam
#if UNITY_IPHONE && !UNITY_EDITOR
	[DllImport ("__Internal")]
#else
	[DllImport ("RenderingPlugin")]
#endif
	private static extern void DestroyWebCam();

//native function for open web cam.
#if UNITY_IPHONE && !UNITY_EDITOR
[DllImport ("__Internal")]
#else
	[DllImport ("RenderingPlugin", EntryPoint="get_map_x1")]
#endif
	private static extern IntPtr get_map_x1();

//native function for open web cam.
#if UNITY_IPHONE && !UNITY_EDITOR
[DllImport ("__Internal")]
#else
[DllImport ("RenderingPlugin", EntryPoint="get_map_y1")]
#endif
private static extern IntPtr get_map_y1();


	// We'll also pass native pointer to a texture in Unity.
	// The plugin will fill texture data from native code.
#if UNITY_IPHONE && !UNITY_EDITOR
	[DllImport ("__Internal")]
#else
	[DllImport ("RenderingPlugin")]
#endif
#if UNITY_GLES_RENDERER
	private static extern void SetTextureFromUnity(System.IntPtr texture, int w, int h);
#else
	private static extern void SetTextureFromUnity(System.IntPtr texture);
#endif


#if UNITY_IPHONE && !UNITY_EDITOR
	[DllImport ("__Internal")]
#else
	[DllImport("RenderingPlugin")]
#endif
	private static extern void SetUnityStreamingAssetsPath([MarshalAs(UnmanagedType.LPStr)] string path);


#if UNITY_IPHONE && !UNITY_EDITOR
	[DllImport ("__Internal")]
#else
	[DllImport("RenderingPlugin")]
#endif
	private static extern IntPtr GetRenderEventFunc();

	private GameObject StereoCamera;

	private int width = 1080;
	private int height = 1080;

	private Texture2D texture;
	private float[] map_x1;
	private float[] map_y1;

	public Boolean init_success = false;
	public UseRenderingPlugin_right render_sc_right;
	
	IEnumerator Start()
	{
		StereoCamera = GameObject.Find ("StereoCamera");

		if (OpenCamera()) {
			Debug.Log("init success!");

			//making a new texture to shader
			texture = new Texture2D (width, height, TextureFormat.RGFloat, false);
			Material mat = GetComponent<Renderer>().material;
			mat.SetTexture ("_Texture2", texture);
			
			//copying the calib params from opencv dll
			map_x1 = new float[width * height];
			map_y1 = new float[width * height];

			IntPtr ptr =  get_map_x1 ();
			Marshal.Copy (ptr, map_x1, 0, width * height);

			ptr = get_map_y1 ();
			Marshal.Copy (ptr, map_y1, 0, width * height);

			Debug.Log("map x1 0: "+map_x1[1000]);
			Debug.Log("map x1 1: "+map_x1[1001]);
			Debug.Log("map x1 2: "+map_x1[2]);
			Debug.Log("map x1 3: "+map_x1[3]);
			Debug.Log("map y1 0: "+map_y1[1000]);
			Debug.Log("map y1 1: "+map_y1[1001]);
			Debug.Log("map y1 2: "+map_y1[2]);
			Debug.Log("map y1 3: "+map_y1[3]);

			for (int i=0; i<width; ++i) {
				for(int j=0; j<height; ++j){
					Color color;
					color.r = map_x1[j*width+i]/(float)width;
					color.g = map_y1[j*width+i]/(float)height;
					//				color.r = ((float)i-0.0f)/(float)width;
					//				color.g = ((float)j-0.0f)/(float)height;
					color.b = 0.0f;
					color.a = 1.0f;
					texture.SetPixel(i,j,color);
				}
			}
			
			texture.Apply ();

			init_success = true;

			render_sc_right.enabled = true;

			if (initSuccessEvent != null)
				initSuccessEvent ();
			
		} else {
			Debug.Log("init false");
		}

		SetUnityStreamingAssetsPath(Application.streamingAssetsPath);

		CreateTextureAndPassToPlugin();
		yield return StartCoroutine("CallPluginAtEndOfFrames");


	}

	private void CreateTextureAndPassToPlugin()
	{
		// Create a texture
		Texture2D tex = new Texture2D(width,height,TextureFormat.ARGB32,false);
		tex.wrapMode = TextureWrapMode.Repeat;
		// Set point filtering just so we can see the pixels clearly
		tex.filterMode = FilterMode.Bilinear;
		// Call Apply() so it's actually uploaded to the GPU
		tex.Apply();

		// Set texture onto our matrial
		GetComponent<Renderer>().material.mainTexture = tex;

		// Pass texture pointer to the plugin
	#if UNITY_GLES_RENDERER
		SetTextureFromUnity (tex.GetNativeTexturePtr(), tex.width, tex.height);
	#else
		SetTextureFromUnity (tex.GetNativeTexturePtr());
	#endif
	}

	private IEnumerator CallPluginAtEndOfFrames()
	{
		while (true) {

//			StereoCamera.transform.Rotate(Vector3.up * 50f * Time.deltaTime);

//			float time1 = Time.realtimeSinceStartup;

			// Wait until all frame rendering is done
			yield return new WaitForEndOfFrame();

			// Set time for the plugin
			SetTimeFromUnity (Time.timeSinceLevelLoad);

			// Issue a plugin event with arbitrary integer identifier.
			// The plugin can distinguish between different
			// things it needs to do based on this ID.
			// For our simple plugin, it does not matter which ID we pass here.
			GL.IssuePluginEvent(GetRenderEventFunc(), 0);

//			float time2 = Time.realtimeSinceStartup;
//			float interval = time2 - time1;
//			Debug.Log("time_left: "+interval*1000+"ms.");
        }
	}

	void OnApplicationQuit()
	{
		DestroyWebCam ();
		Debug.Log("quit");
	}
}
