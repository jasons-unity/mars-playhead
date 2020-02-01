using UnityEngine;
using System.Runtime.InteropServices;
using System;
using AOT;

namespace ULSTrackerForUnity
{

	public static class Plugins
	{
		public const int MAX_TRACKER_POINTS = 66;
		public const int MAX_TRACKER_FACES = 4;

#if UNITY_STANDALONE || UNITY_EDITOR
		const string dll = "LibMultiTracker";
#else
	#if UNITY_IOS
		const string dll = "__Internal";
	#elif UNITY_ANDROID
		const string dll = "ulsCppMultiTracker_native";
	#endif
#endif

		private static Texture2D _PreviewTexture = null;
		private static bool WaitForFirstFrame = true;

		[MonoPInvokeCallback(typeof(RenderCallback))]
		private static void Render (int request) {
			Dispatch.Dispatch (() => {
				GL.IssuePluginEvent (NativeRendererCallback (), request);
			});
		}

		[MonoPInvokeCallback(typeof(UpdateCallback))]
		private static void Update (int width, int height, IntPtr RGBA32GPUPtr, int rotate) {
			//Dispatch on main thread
			Dispatch.Dispatch(() => {
				if (WaitForFirstFrame) {
					//Initialization
					#if UNITY_STANDALONE || UNITY_EDITOR
					if (!_PreviewTexture)
						_PreviewTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
					#else
					if (!_PreviewTexture)
						_PreviewTexture = Texture2D.CreateExternalTexture (width, height, TextureFormat.RGBA32, false, false, RGBA32GPUPtr);
					#endif

					WaitForFirstFrame = false;
					if (OnPreviewStart!=null)
						OnPreviewStart(_PreviewTexture, rotate);
				} else if (_PreviewTexture!=null) {
					#if UNITY_STANDALONE || UNITY_EDITOR
					if (_PreviewTexture.width != width || _PreviewTexture.height != height)
						return;

					_PreviewTexture.LoadRawTextureData(RGBA32GPUPtr,width*height*4);
					_PreviewTexture.Apply();
					#else
					_PreviewTexture.UpdateExternalTexture(RGBA32GPUPtr);
					#endif

					if (OnPreviewUpdate!=null) {
						OnPreviewUpdate(_PreviewTexture);
					}
				}
			});
		}

		#region ---Native Delegate Callbacks---
		public delegate void RenderCallback (int request);
		public delegate void UpdateCallback (int width, int height, IntPtr RGBA32GPUPtr, int rotate);
		#endregion

		#region --Events--
		public static Action<Texture, int> OnPreviewStart;
		public static Action<Texture> OnPreviewUpdate;
		#endregion

		private static CameraDispatch Dispatch = null;

        // import ULSee FaceTracker functions
#if UNITY_STANDALONE || UNITY_EDITOR

		[DllImport (dll)]
		private static extern int ULS_UnityTrackerInit(string pModelPath, string activateKey, string writable, int num_faces);
		[DllImport (dll)]
		private static extern void ULS_UnitySetSmoothing(bool use_smoothing);
		[DllImport (dll)]
		private static extern bool ULS_UnityGetSmoothing();
		[DllImport (dll)]
		private static extern void ULS_UnityTrackerRelease();

		[DllImport (dll)]
		public static extern IntPtr NativeRendererCallback();
		[DllImport (dll)]
		public static extern int ULS_UnityTrackerUpdate([In, Out] byte[] image, int width, int height);
		[DllImport (dll)]
		public static extern void ULS_UnityRegisterCallbacks (RenderCallback rCallback, UpdateCallback uCallback);
		[DllImport (dll)]
		public static extern int ULS_UnityCreateVideoCapture(int index, int width, int height, int fps, int rotation);//0:CCW, 1:flip, 2:CW
		[DllImport (dll)]
		public static extern void ULS_UnityUpdateVideoCapture();
		[DllImport (dll)]
		public static extern void ULS_UnityCloseVideoCapture();

		//<summary>
		///Initialize face tracker and camera.
		///</summary>
		public static int ULS_UnityTrackerInit(Transform parent = null) {
			WaitForFirstFrame = true;
			_PreviewTexture = null;
			#if UNITY_EDITOR
			string path = Application.dataPath + "/Providers/ULSFaceTracker/Runtime/Plugins/model";
			#elif UNITY_STANDALONE_OSX
			string path = Application.dataPath + "/Plugins/"+dll+".bundle/Contents/Resources/model";
			#else //WINDOWS
			string path = Application.dataPath + "/Plugins/model";
			#endif
			ULS_UnityRegisterCallbacks(Render, Update);
			if (Dispatch == null) {
				Dispatch = new GameObject ("CameraDispatch").AddComponent<CameraDispatch> ();
			}

			Dispatch.transform.SetParent(parent, false);
			int ret = Plugins.ULS_UnityTrackerInit(path, Activation.Key, Application.temporaryCachePath, MAX_TRACKER_FACES);
			ULS_UnitySetSmoothing (true);
			return ret;
		}

		//<summary>
		///Change camera setting.
		///</summary>
		///<param name="width"> camera width </param>
		///<param name="height"> camera height </param>
		///<param name="fps"> frame rate per second </param>
		///<param name="frontal"> front camera or rear camera </param>
		public static void ULS_UnitySetupCamera(int width, int height, int fps, bool frontal) {
			if (0 == Plugins.ULS_UnityCreateVideoCapture (0, width, height, fps, -1)) {
				Dispatch.Clear ();
				_PreviewTexture = null;
				WaitForFirstFrame = true;
			}
		}

		//<summary>
		///Enable camera flash light.
		///</summary>
		///<param name="enable"> enable flash light </param>
		public static void ULS_UnitySetFlashLight(bool enable) {
		}

		//<summary>
		///Get status of camera flash light.
		/// True: flash light is enabled.
		/// False: flash light is disabled.
		///</summary>
		public static bool ULS_UnityGetFlashLight() {
			return false;
		}

		//<summary>
		///Pause camera feed.
		///</summary>
		///<param name="paused"> enable pausing </param>
		public static void ULS_UnityPauseCamera (bool paused) {
			Dispatch.SetRunning (!paused);
		}

		//<summary>
		///Terminate face tracker and camera.
		///</summary>
		public static void ULS_UnityTrackerTerminate() {
			ULS_UnityTrackerRelease();
			Dispatch.Clear();
			Dispatch = null;
			_PreviewTexture = null;
			WaitForFirstFrame = true;
		}

#elif UNITY_IOS

		[DllImport (dll)]
		private static extern void ULS_UnityRegisterCallbacks (RenderCallback rCallback, UpdateCallback uCallback);
		[DllImport (dll)]
		private static extern int ULS_UnityTrackerInitWithKey(string activateKey, int num_faces);
		[DllImport (dll)]
		private static extern void ULS_UnityDeleteTracker();
		[DllImport (dll)]
		private static extern void NativeCameraSetting(int w, int h, int fps, bool front);

		[DllImport(dll)]
		public static extern IntPtr NativeRendererCallback ();

		//<summary>
		///Initialize face tracker and camera.
		///</summary>
		public static int ULS_UnityTrackerInit(Transform parent = null) {
			if (Dispatch == null) {
				Dispatch = new GameObject ("CameraDispatch").AddComponent<CameraDispatch> ();
			}
			ULS_UnityRegisterCallbacks(Render, Update);
			Dispatch.transform.SetParent(parent, false);
			return ULS_UnityTrackerInitWithKey(Activation.Key, MAX_TRACKER_FACES);
		}

		//<summary>
		///Pause camera feed.
		///</summary>
		///<param name="enable"> enable pausing </param>
		[DllImport (dll)]
		public static extern void ULS_UnityPauseCamera(bool enable);

		//<summary>
		///Enable camera flash light.
		///</summary>
		///<param name="enable"> enable flash light </param>
		[DllImport (dll)]
		public static extern void ULS_UnitySetFlashLight(bool enable);

		//<summary>
		///Get status of camera flash light.
		/// True: flash light is enabled.
		/// False: flash light is disabled.
		///</summary>
		[DllImport (dll)]
		public static extern bool ULS_UnityGetFlashLight();

		//<summary>
		///Change camera setting.
		///</summary>
		///<param name="width"> camera width </param>
		///<param name="height"> camera height </param>
		///<param name="fps"> frame rate per second </param>
		///<param name="frontal"> front camera or rear camera </param>
		public static void ULS_UnitySetupCamera(int width, int height, int fps, bool frontal) {
			NativeCameraSetting(width, height, fps, frontal);
			Dispatch.Clear();
			_PreviewTexture = null;
			WaitForFirstFrame = true;
		}

		//<summary>
		///Terminate face tracker and camera.
		///</summary>
		public static void ULS_UnityTrackerTerminate() {
			ULS_UnityDeleteTracker();
			Dispatch.Clear();
			_PreviewTexture = null;
			WaitForFirstFrame = true;
		}

#elif UNITY_ANDROID

		[DllImport("ulsCppMultiTracker_unity")]
		private static extern IntPtr NativeRendererCallback ();

		[DllImport("ulsCppMultiTracker_unity")]
		private static extern void RegisterCallbacks(RenderCallback cb, UpdateCallback cb2);

		[DllImport("ulsCppMultiTracker_unity")]
		public static extern void TrackerConfiguration(bool PredictPupils, bool HighPrecision, bool FaceGlue);

		public static AndroidJavaObject A {
			get {
				if (NCNA == null) {
					using (AndroidJavaClass NCNAClass = new AndroidJavaClass("com.uls.multifacetrackerlib.UlsCamera")) {
						if (NCNAClass != null) {
							NCNA = NCNAClass.CallStatic<AndroidJavaObject>("instance");
						}
					}
				}
				return NCNA;
			}
		}

		private static AndroidJavaObject NCNA;
		private static int tracker_init = 0;

		//<summary>
		///Pause camera feed.
		///</summary>
		///<param name="paused"> enable pausing </param>
		public static void ULS_UnityPauseCamera (bool paused) {
			if (tracker_init == 0)
				return;
			if (paused) A.Call("SuspendProcess");
			else A.Call("ResumeProcess", true);
		}

		//<summary>
		///Initialize face tracker and camera.
		///</summary>
		public static int ULS_UnityTrackerInit(Transform parent = null) {
			if (Dispatch == null) {
				Dispatch = new GameObject ("CameraDispatch").AddComponent<CameraDispatch> ();
				Dispatch.SetApplicationFocus = (ULS_UnityPauseCamera);
			}

			// change default camera setting
			//A.Call<bool> ("SetupCamera", 1280, 720, 30, true);

			Dispatch.Clear();
			_PreviewTexture = null;
			WaitForFirstFrame = true;
			RegisterCallbacks (Render, Update);
			tracker_init = A.Call<int> ("initialize", Activation.Key, MAX_TRACKER_FACES);

			Dispatch.transform.SetParent(parent, false);

			A.Call ("PlayPreview");
			return tracker_init;
		}

		//<summary>
		///Change camera setting.
		///</summary>
		public static void ULS_UnitySetupCamera(int width, int height, int fps, bool frontal) {
			Dispatch.Dispatch (() => {
				if (A.Call<bool> ("SetupCamera", width, height, fps, frontal)) {
					if (OnPreviewStart!=null)
						OnPreviewStart(Texture2D.whiteTexture, 0);
					Dispatch.Clear();
					_PreviewTexture = null;
					WaitForFirstFrame = true;
					A.Call ("PlayPreview");
				}
			});
		}

		//<summary>
		///Terminate face tracker and camera.
		///</summary>
		public static void ULS_UnityTrackerTerminate() {
			Dispatch.Dispatch (() => {
				A.Call("SuspendProcess");
				if (OnPreviewStart!=null)
					OnPreviewStart(Texture2D.whiteTexture, 0);
				Dispatch.Clear();
				_PreviewTexture = null;
				WaitForFirstFrame = true;
				A.Call("TerminateOperations");
				NCNA = null;
			});
		}

		//<summary>
		///Enable camera flash light.
		///</summary>
		///<param name="enable"> enable flash light </param>
		public static void ULS_UnitySetFlashLight(bool enable) {
			A.Call("SetFlashLight",enable);
		}

		//<summary>
		///Get status of camera flash light.
		/// True: flash light is enabled.
		/// False: flash light is disabled.
		///</summary>
		public static bool ULS_UnityGetFlashLight() {
			return A.Call<bool>("GetFlashLight");
		}

#endif

        [DllImport (dll)]
		public static extern int ULS_UnityGetPoints([In, Out] float[] points2d, [In] int index);

		[DllImport (dll)]
		public static extern int ULS_UnityGetPoints3D([In, Out] float[] points3d, [In] int index);

		[DllImport (dll)]
		public static extern int ULS_UnityGetConfidence([In, Out] float[] conf, [In] int index);

		[DllImport (dll)]
		public static extern float ULS_UnityGetPitchRadians ([In] int index);

		[DllImport (dll)]
		public static extern float ULS_UnityGetYawRadians ([In] int index);

		[DllImport (dll)]
		public static extern float ULS_UnityGetRollRadians ([In] int index);

		[DllImport (dll)]
		public static extern float ULS_UnityGetScaleInImage([In] int index);

		[DllImport (dll)]
		public static extern bool ULS_UnityGetLeftPupil([In, Out] float[] x, [In, Out] float[] y, [In] int index);
		[DllImport (dll)]
		public static extern bool ULS_UnityGetRightPupil([In, Out] float[] x, [In, Out] float[] y, [In] int index);
		[DllImport (dll)]
		public static extern bool ULS_UnityGetLeftGaze([In, Out] float[] x, [In, Out] float[] y, [In, Out] float[] z, [In] int index);
		[DllImport (dll)]
		public static extern bool ULS_UnityGetRightGaze([In, Out] float[] x, [In, Out] float[] y, [In, Out] float[] z, [In] int index);

		//<summary>
		///Get current tracker points. When no face is detected, return 0.
		///</summary>
		[DllImport (dll)]
		public static extern int  ULS_UnityGetTrackerPointNum();

		//<summary>
		///Get number of tracking faces. When no face is tracked, return 0.
		///</summary>
		[DllImport (dll)]
		public static extern int ULS_UnityFaceGetActiveCount();

		// return value
		//  1: alive
		//  0: non detected
		// -1: failed
		[DllImport (dll)]
		public static extern int ULS_UnityFaceIsAlive(int index);

		//Get the index of the face that is closer to the given locations
		[DllImport (dll)]
		public static extern int ULS_UnityFaceGetIndexByPosition(int x,  int y);

		[DllImport (dll)]
		public static extern int ULS_UnityFaceGetIndexByRectangle(int top,  int left,  int width,  int height);


		//<summary>
		///Get transform matrix to align 3D tracking points with 2D tracking points.
		///</summary>
		///<param name="transform"> Output Trnasform matrix</param>
		///<param name="intrinsic_camera_matrix">
		/// Input intrinsic Camera Matrix,
		/// [focal_length_x, 0, image_center_x,
		///  0, focal_length_y, image_center_y,
		///  0,              0,              1]
		///</param>
		///<param name="distort_coeffs">
		/// Input vector of distortion coefficients (k_1, k_2, p_1, p_2[, k_3[, k_4, k_5, k_6]]) of 4, 5, or 8 elements.
		/// If the vector is NULL/empty, the zero distortion coefficients are assumed.
		///</param>
		[DllImport (dll)]
		public static extern void  ULS_UnityGetTransform([In, Out] float [] transform, [In] float[] intrinsic_camera_matrix, [In] float[] distort_coeffs, [In] int index);

		//<summary>
		///Get 3d scale vector compare to original 3d model of tracker
		///</summary>
		///<param name="x"> X axis of scale vector. </param>
		///<param name="y"> Y axis of scale vector. </param>
		///<param name="z"> Z axis of scale vector. </param>
		[DllImport (dll)]
		public static extern void ULS_UnityGetScale3D([In, Out] float [] x, [In, Out] float [] y, [In, Out] float [] z, [In] int index);

		//<summary>
		///Get Field of View.
		///</summary>
		///<param name="intrinsic_camera_matrix">
		/// Input intrinsic Camera Matrix,
		/// [focal_length_x, 0, image_center_x,
		///  0, focal_length_y, image_center_y,
		///  0,              0,              1]
		///</param>
		///<param name="image_width"> Input image width. </param>
		///<param name="image_height"> Input image height. </param>
		///<param name="fovx"> Output field of view in degrees along the horizontal sensor axis. </param>
		///<param name="fovy"> Output field of view in degrees along the vertical sensor axis. </param>
		[DllImport (dll)]
		public static extern void ULS_UnityCalibration([In] float[] intrinsic_camera_matrix, float image_width, float image_height,
		[In, Out] float[] fovx, [In, Out] float[] fovy);

		//<summary>
		///Enable Tracker.
		///</summary>
		///<param name="isTracking"> Input enable tracker. </param>
		[DllImport (dll)]
		public static extern void ULS_UnityTrackerEnable ([In] bool isTracking);
	}
}
