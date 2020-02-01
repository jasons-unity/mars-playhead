#define DRAW_MARKERS

using UnityEngine;
using System.Collections.Generic;
using ULSTrackerForUnity;
using UnityEngine.SceneManagement;

class FaceMask : MonoBehaviour
{

#if DRAW_MARKERS
	public GameObject marker = null;
	List<GameObject> _marks = new List<GameObject>();
	bool drawMarkers = false;
#endif

	public GameObject faceMask = null;
	MeshFilter[] _faceMeshes = null;

	private Vector3[] _vertices;

	float[] _trackPoints = new float[Plugins.MAX_TRACKER_POINTS*2];

	public Texture2D[] _masks = null;
	int maskTextureIndex = 0;

	bool initDone = false;

	void createMesh() {
		_faceMeshes = new MeshFilter[Plugins.MAX_TRACKER_FACES];
		for (int i = 0; i < Plugins.MAX_TRACKER_FACES; ++i) {
			var g = Instantiate (faceMask);
			_faceMeshes[i] = g.GetComponent<MeshFilter>();
			g.transform.parent = transform.parent;
			g.SetActive (false);
		}
		_vertices = _faceMeshes [0].mesh.vertices;
	}

	void Start ()
	{
		//Debug.Log(SceneManager.GetActiveScene().name);

#if DRAW_MARKERS
		for (int i=0; i< Plugins.MAX_TRACKER_POINTS; ++i) {
			var g = Instantiate (marker);
			g.transform.parent = transform.parent;
			g.SetActive (false);
			_marks.Add (g);
		}
#endif

		InitializeTrackerAndCheckKey();
		createMesh ();
		Application.targetFrameRate = 60;
	}

	// Initialize tracker and check activation key
	void InitializeTrackerAndCheckKey ()
	{
		Plugins.OnPreviewStart = initCameraTexture;
		//Plugins.OnPreviewUpdate = previewUpdate;

		int initTracker = Plugins.ULS_UnityTrackerInit();
		if (initTracker < 0) {
			Debug.Log ("Failed to initialize tracker.");
		} else {
			Debug.Log ("Tracker initialization succeeded");
		}
	}
		
	void initCameraTexture (Texture preview,int rotate) {
		int w = preview.width;
		int h = preview.height;
		GetComponent<Renderer> ().material.mainTexture = preview;
	
		// adjust scale and position to map tracker points
#if UNITY_STANDALONE || UNITY_EDITOR
		// adjust scale and position to map tracker points
		transform.localScale = new Vector3 (w, h, 1);
		transform.localPosition = new Vector3 (w/2, h/2, 0);
		transform.parent.localScale = new Vector3 (-1, -1, 1);
		transform.parent.localPosition = new Vector3 (w/2, h/2, 0);
		Camera.main.orthographicSize = h / 2;

#elif UNITY_IOS || UNITY_ANDROID
		int orthographicSize = w / 2;
		if (Screen.orientation == ScreenOrientation.LandscapeLeft || Screen.orientation == ScreenOrientation.LandscapeRight) {
			orthographicSize = h / 2;
		}

		transform.localScale = new Vector3 (w, h, 1);
		transform.localPosition = new Vector3 (w/2, h/2, 1);//anchor: left-bottom
		transform.parent.localPosition = Vector3.zero;
		transform.parent.eulerAngles = new Vector3 (0, 0, rotate);//orientation
		transform.parent.localPosition = transform.parent.TransformPoint(-transform.localPosition); //move to center
		Camera.main.orthographicSize = orthographicSize;
#endif

		initDone = true;
	}

	void scaleJawine(int start, int end, int jaw, int eye, int check, Vector3[] points) {
		if (points [check].y > points [eye].y)
			return;

		float jawY = points [jaw].y;
		float scale = (points [eye].y - jawY) / (points [check].y - jawY);

		for (int j = start; j <= end; ++j) {
			points[j].y = (points[j].y - jawY)*scale + jawY;
		}
	}

	void Update () {
		if (!initDone)
			return;

        // Show tracking result
		for (int i = 0; i < Plugins.MAX_TRACKER_FACES; ++i) {
			if (0 >= Plugins.ULS_UnityGetPoints (_trackPoints, i)) {
				_faceMeshes [i].gameObject.SetActive (false);
				continue;
			}
			for (int j = 0; j < Plugins.MAX_TRACKER_POINTS; ++j) {
				_vertices [j] = new Vector3 (_trackPoints [j * 2], _trackPoints [j * 2 + 1], 0);
			}
			/*
			scaleJawine (0, 2, 3, 36, 0, _vertices);
			scaleJawine (14, 16, 13, 45, 16, _vertices);
			//*/
			_faceMeshes [i].mesh.MarkDynamic ();
			_faceMeshes [i].mesh.vertices = _vertices;
			_faceMeshes [i].mesh.RecalculateBounds ();
			_faceMeshes [i].gameObject.SetActive (true);
		}
#if DRAW_MARKERS
		for(int j=0;j<Plugins.MAX_TRACKER_POINTS;++j) {
			_marks[j].transform.localPosition = _vertices[j];
			_marks[j].SetActive(drawMarkers);
		}
#endif
	}

	bool frontal = true;
	bool flashLight = false;
	bool pause = false;
	bool enableTracker = true;

	void OnGUI() {
#if DRAW_MARKERS
		if (GUILayout.Button ("Show Markers", GUILayout.Height (80))) {
			drawMarkers ^= true;
		}
		GUILayout.Space (8);
#endif

		if (GUILayout.Button ("Next Mask", GUILayout.Height (80))) {
			maskTextureIndex = (maskTextureIndex + 1) % _masks.Length;
			faceMask.GetComponent<Renderer> ().material.mainTexture = _masks [maskTextureIndex];
		}

		GUILayout.Space (8);
		if (GUILayout.Button ("Toggle Tracker", GUILayout.Height (100))) {
			enableTracker ^= true;
			Plugins.ULS_UnityTrackerEnable (enableTracker);
		}

		GUILayout.Space (8);
		if (GUILayout.Button ("Switch Camera", GUILayout.Height (80))) {
			frontal = !frontal;
			if(frontal)
				Plugins.ULS_UnitySetupCamera (640, 480, 30, true);
			else
				Plugins.ULS_UnitySetupCamera (1280, 720, 60, false);
		}

		GUILayout.Space (8);
		if (GUILayout.Button ("Toogle FlashLight", GUILayout.Height (80))) {
			flashLight = !flashLight;
			Plugins.ULS_UnitySetFlashLight (flashLight);
		}

		GUILayout.Space (8);
		if (GUILayout.Button ("Change Scene", GUILayout.Height (80))) {
			Plugins.ULS_UnityTrackerTerminate ();
			SceneManager.LoadScene ("Object3D");
		}

		GUILayout.Space (8);
		if (GUILayout.Button ("Pause camera", GUILayout.Height (80))) {
			pause = !pause;
			Plugins.ULS_UnityPauseCamera (pause);
		}

		GUILayout.Label ("FlashLight:" + Plugins.ULS_UnityGetFlashLight ());

		GUILayout.Label ("Faces:" + Plugins.ULS_UnityFaceGetActiveCount ());
	}
}