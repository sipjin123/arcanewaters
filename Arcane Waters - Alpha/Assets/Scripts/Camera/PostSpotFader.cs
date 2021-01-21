﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;
using System;

[ExecuteAlways]
public class PostSpotFader : ClientMonoBehaviour, IScreenFader
{
   #region Public Variables

   // The singleton instance
   public static PostSpotFader self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;

      _effectProgress = 1;
      _effectProgressPropertyID = Shader.PropertyToID("_Progress");
      _spotPositionPropertyID = Shader.PropertyToID("_SpotPosition");
      _screenSizePropertyID = Shader.PropertyToID("_ScreenSize");
      _pixelSizePropertyID = Shader.PropertyToID("_PixelSize");
   }

#if UNITY_EDITOR
   private void OnEnable () {
      // In the editor only, don't subscribe to the CameraManager event unless we're in Play Mode
      if (!Application.isPlaying) {
         updatePixelSize();         
      }
   }
#endif

   private void Start () {
#if UNITY_EDITOR
      // In the editor only, don't subscribe to the CameraManager event unless we're in Play Mode
      if (!Application.isPlaying) {         
         return;
      }
#endif

      updateScreenSize();

      // Make sure to update the screen size in the shader when it changes
      CameraManager.self.resolutionChanged += updateScreenSize;
   }

   private void updatePixelSize () {
      // We'll update the pixel size using the new camera size
      int pixelSize = CameraManager.self != null ? CameraManager.getCurrentPPUScale() : DEFAULT_PIXEL_SIZE;

      // Make sure we have a valid pixelSize value
      pixelSize = pixelSize > 0 ? pixelSize : DEFAULT_PIXEL_SIZE;
            
      _material.SetFloat(_pixelSizePropertyID, pixelSize);
   }

   private void updateScreenSize () {
      updatePixelSize();
      _material.SetVector(_screenSizePropertyID, new Vector4(Screen.width, Screen.height));      
   }

   private void OnDestroy () {
      if (CameraManager.self != null) {
         CameraManager.self.resolutionChanged -= updateScreenSize;
      }
   }

#if UNITY_EDITOR
   private void Update () {
      // Update these values every frame in the Editor for easier tweaking
      if (!Application.isPlaying || _enableEditInPlaymode) {
         _material.SetFloat("_Progress", _effectProgress);
         _material.SetVector("_SpotPosition", _spotScreenPosition);
         _material.SetVector("_ScreenSize", new Vector4(Screen.width, Screen.height));
         _material.SetFloat("_DitherPercent", _ditherAmount);
      }
   }
#endif

   private void OnRenderImage (RenderTexture source, RenderTexture destination) {
      recalibrateSpotPosition();
      updatePixelSize();
      _material.SetFloat(_effectProgressPropertyID, _effectProgress);
      _material.SetVector(_screenSizePropertyID, new Vector4(Screen.width, Screen.height));
      Graphics.Blit(source, destination, _material);
   }

   public float fadeIn () {
      _fadeTween?.Kill();

      recalibrateSpotPosition();

      _fadeTween = DOTween.To(() => _effectProgress, (x) => _effectProgress = x, 1, _fadeInDuration);

      return _fadeInDuration;
   }

   public float fadeOut () {
      _fadeTween?.Kill();

      recalibrateSpotPosition();

      _fadeTween = DOTween.To(() => _effectProgress, (x) => _effectProgress = x, 0, _fadeOutDuration).SetEase(Ease.OutSine);

      return _fadeOutDuration;
   }

   public void recalibrateSpotPosition () {
      // If we have a player, close towards the player position. Otherwise, close towards the center of the screen.
      if (Global.player != null) {
         setSpotWorldPosition(Global.player.sortPoint.transform.position);
      } else if (CharacterScreen.self != null && CharacterScreen.self.isShowing() && CharacterSpot.lastInteractedSpot != null && CharacterSpot.lastInteractedSpot.character != null) {
         setSpotWorldPosition(CharacterSpot.lastInteractedSpot.character.transform.position);
      } else {
         setSpotPositionToCenter();
      }
   }

   private void setSpotWorldPosition (Vector3 worldPosition) {
      if (!Camera.main) return; // skip if no main camera found
      
      _spotScreenPosition = Camera.main.WorldToViewportPoint(worldPosition);
      _material.SetVector(_spotPositionPropertyID, _spotScreenPosition);
   }

   private void setSpotPositionToCenter () {
      _spotScreenPosition = new Vector2(0.5f, 0.5f);
      _material.SetVector(_spotPositionPropertyID, _spotScreenPosition);
   }

   #region Private Variables

   // The progress of the effect
   [SerializeField, Range(0, 1)]
   private float _effectProgress = 0;

   // What percentage of the spot is dithered
   [SerializeField, Range(0f, 2.0f)]
   private float _ditherAmount = 0.2f;

   // The screen position of the spot
   [SerializeField]
   private Vector2 _spotScreenPosition;

   // The time for the circle to close
   [SerializeField]
   private float _fadeOutDuration = 0.5f;

   // The time for the circle to open
   [SerializeField]
   private float _fadeInDuration = 0.5f;

   // The material used for the effect
   [SerializeField]
   private Material _material;

#if UNITY_EDITOR
   // Whether it's possible to change values within Unity in Play Mode 
   [SerializeField]
   private bool _enableEditInPlaymode = false;
#endif

   // The fade tween
   private Tween _fadeTween;

   // The property ID of the "_EffectProgress" property
   private int _effectProgressPropertyID;

   // The property ID of the "_SpotPosition" property
   private int _spotPositionPropertyID;

   // The property ID of the "_ScreenSize" property
   private int _screenSizePropertyID;

   // The property ID of the "_PixelSize" property
   private int _pixelSizePropertyID;

   // The default pixel size if an invalid PPU scale is provided
   public const int DEFAULT_PIXEL_SIZE = 4;

   #endregion
}
