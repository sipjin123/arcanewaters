using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System;

public class CameraManager : ClientMonoBehaviour {
   #region Public Variables

   // The default camera
   public static DefaultCamera defaultCamera;

   // The battle camera
   public static BattleCamera battleCamera;

   // Self
   public static CameraManager self;

   // Reference to the main gui canvas
   public Canvas guiCanvas;

   // Resolution reference that caps the ortho size
   public List<ResolutionOrthoClamp> resolutionList;

   // List of objects to reset
   public List<GameObject> resetObjectList;

   #endregion

   protected override void Awake () {
      base.Awake();

      // Look up the two cameras
      defaultCamera = GameObject.FindObjectOfType<DefaultCamera>();
      battleCamera = GameObject.FindObjectOfType<BattleCamera>();
      _baseCameras = new List<BaseCamera>();

      foreach (BaseCamera baseCam in GameObject.FindObjectsOfType<BaseCamera>()) {
         _baseCameras.Add(baseCam);
      }

      // Store a reference
      self = this;
   }

   void Start () {
      _quakeEffect = GetComponent<CameraFilterPack_FX_EarthQuake>();
      _screenResolution = new Vector2(Screen.width, Screen.height);
      _isFullscreen = Screen.fullScreen;
   }

   private void LateUpdate () {
      // Update the orthographic size of the cameras if the screen resolution changes
      if (_isFullscreen != Screen.fullScreen || _screenResolution.x != Screen.width || _screenResolution.y != Screen.height) {
         _screenResolution = new Vector2(Screen.width, Screen.height);
         _isFullscreen = Screen.fullScreen;
         onResolutionChanged();
      }
   }

   public void registerCamera (MyCamera newSceneCamera) {
      _baseCameras.Add(newSceneCamera);
   }

   private IEnumerator CO_ResetObjects () {
      foreach (GameObject obj in resetObjectList) {
         obj.SetActive(false);
      }
      yield return new WaitForSeconds(.5f);
      foreach (GameObject obj in resetObjectList) {
         obj.SetActive(true);
      }
   }

   public void onResolutionChanged () {
      Debug.Log("Updating cam size");

      foreach (BaseCamera baseCam in _baseCameras) {
         baseCam.onResolutionChanged();
      }

      StartCoroutine(CO_ResetObjects());
   }

   public static void shakeCamera (float duration = .25f) {
      self.StartCoroutine(self.CO_ShakeCamera(duration));
   }

   private IEnumerator CO_ShakeCamera (float duration) {
      _quakeEffect.enabled = true;

      // Let the effect take place for the specified number of seconds
      yield return new WaitForSeconds(duration);

      _quakeEffect.enabled = false;
   }

   public static void enableBattleDisplay () {
      self.StartCoroutine(self.CO_EnableBattleDisplay());
   }

   public static void disableBattleDisplay () {
      // Show the pixel fade effect
      self.StartCoroutine(self.CO_DisableBattleDisplay());
   }

   public static bool isShowingBattle () {
      if (battleCamera == null || defaultCamera == null) {
         return false;
      }

      return battleCamera.getDepth() > defaultCamera.getDepth();
   }

   protected IEnumerator CO_EnableBattleDisplay () {
      // Start the fade to black effect
      defaultCamera.getPixelFadeEffect().fadeOut();
      battleCamera.getPixelFadeEffect().fadeOut();

      // Play a sound effect
      SoundManager.play2DClip(SoundManager.Type.Battle_Intro, 0f);

      // Play the Battle music
      SoundManager.setBackgroundMusic(SoundManager.Type.Battle_Music);

      // Wait for it to finish
      yield return new WaitForSeconds(1f);

      // Enable the Battle Camera
      battleCamera.getCamera().enabled = true;
      defaultCamera.setDepth(-2);
      battleCamera.setDepth(-1);

      // Switch the audio listener
      defaultCamera.GetComponent<AudioListener>().enabled = false;
      battleCamera.GetComponent<AudioListener>().enabled = true;

      // Start the fade in effect
      //defaultCamera.getPixelFadeEffect().fadeIn();
      battleCamera.getPixelFadeEffect().fadeIn();
   }

   protected IEnumerator CO_DisableBattleDisplay () {
      // Start the fade to black effect
      defaultCamera.getPixelFadeEffect().fadeOut();
      battleCamera.getPixelFadeEffect().fadeOut();

      // Play a sound effect
      SoundManager.play2DClip(SoundManager.Type.Battle_Outro, 0f);

      // End the Battle music
      SoundManager.setBackgroundMusic(SoundManager.previousMusicType);

      // Wait for it to finish
      yield return new WaitForSeconds(1f);

      // Disable the Battle Camera
      defaultCamera.setDepth(-1);
      battleCamera.setDepth(-2);
      battleCamera.getCamera().enabled = false;

      // Switch the audio listener
      defaultCamera.GetComponent<AudioListener>().enabled = true;
      battleCamera.GetComponent<AudioListener>().enabled = false;

      // Start the fade in effect
      defaultCamera.getPixelFadeEffect().fadeIn();
      battleCamera.getPixelFadeEffect().fadeIn();
   }

   #region Private Variables

   // The Camera quake effect
   protected CameraFilterPack_FX_EarthQuake _quakeEffect;

   // The current screen resolution
   protected Vector2 _screenResolution;

   // Whether the game is in fullscreen
   protected bool _isFullscreen;

   // All the BaseCameras
   [SerializeField]
   protected List<BaseCamera> _baseCameras;

   #endregion
}

[Serializable]
public class ResolutionOrthoClamp {
   public string resolutionName;
   public float resolutionWidth;
   public float orthoCap;
}