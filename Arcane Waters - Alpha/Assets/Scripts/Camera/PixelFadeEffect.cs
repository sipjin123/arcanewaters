using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Events;

public class PixelFadeEffect : MonoBehaviour {
   #region Public Variables

   // If this is set to true, at the start it will perform a fade in effect
   // (currently used for the main camera, for the main menu when starting the game.)
   public bool startWithFade = false;

   // The Camera effect
   public CameraFilterPack_Pixel_Pixelisation camFX_Pixel;

   // Custom events that will be used whenever we have finished a pixel fade event
   [HideInInspector] public UnityEvent onFadeInEnd = new UnityEvent();
   [HideInInspector] public UnityEvent onFadeOutEnd = new UnityEvent();

   #endregion

   private void Start () {
      camFX_Pixel.initialize();

      if (startWithFade) {
         fadeIn();
      }
   }

   void Update () {

      // If we do not want to fade in or fade out we do not execute the code (Optimization)
      if (!_willFadeIn && !_willFadeOut) { return; }

      // How long has passed since we started fading?
      float timePassed = Time.time - _fadeStartTime;

      // Lerp towards the target pixel amount
      float newPixelAmount = Mathf.Lerp(camFX_Pixel._Pixelisation, _targetPixelAmount, (timePassed / _fadeDuration));
      setNewPixelAmount(newPixelAmount);

      // If our target is the minimum and we've reached that, we can disable the cam FX
      if (_targetPixelAmount <= MIN_PIXEL_AMOUNT && getPixelAmount() == MIN_PIXEL_AMOUNT) {
         camFX_Pixel.enabled = false;
      }

      // When fading back in, we want to hurry up and get it over with if we're close
      if (Mathf.Abs(_targetPixelAmount - getPixelAmount()) < .5f && _targetPixelAmount == MIN_PIXEL_AMOUNT) {
         setNewPixelAmount(MIN_PIXEL_AMOUNT);
         camFX_Pixel.enabled = false;

         if (_willFadeIn) {
            //onFadeInEnd.Invoke();
            _willFadeIn = false;
         }

         if (_willFadeOut) {
            //onFadeOutEnd.Invoke();
            _willFadeOut = false;
         }
      }
   }

   public bool isPixelated () {
      return camFX_Pixel.enabled && (_targetPixelAmount > MIN_PIXEL_AMOUNT || getPixelAmount() > MIN_PIXEL_AMOUNT);
   }

   public void fadeOut () {
      _willFadeOut = true;

      // Note that we're about to start the fade
      _fadeStartTime = Time.time;

      // Set an appropriate fade duration
      _fadeDuration = FADE_OUT_DURATION;

      // Make sure the effect is enabled
      camFX_Pixel.enabled = true;

      // Set the new target pixelation
      _targetPixelAmount = MAX_PIXEL_AMOUNT;

      // Start a darkness color fade as well
      BrightnessManager.self.setNewTargetIntensity(0f, 1f);
   }

   public void fadeIn () {
      _willFadeIn = true;

      // Note that we're about to start the fade
      _fadeStartTime = Time.time;

      // Set an appropriate fade duration
      _fadeDuration = FADE_IN_DURATION;

      // Make sure the effect is enabled
      camFX_Pixel.enabled = true;

      // Set the new target pixelation
      _targetPixelAmount = MIN_PIXEL_AMOUNT;

      // Start a darkness color fade as well
      BrightnessManager.self.setNewTargetIntensity(1f, 1f);
   }

   protected void setNewPixelAmount (float newPixelAmount) {
      CameraFilterPack_Pixel_Pixelisation.ChangePixel = newPixelAmount;
   }

   protected float getPixelAmount () {
      return CameraFilterPack_Pixel_Pixelisation.ChangePixel;
   }

   #region Private Variables

   // Our minimum pixel amount
   protected static float MIN_PIXEL_AMOUNT = .6f;

   // Our maximum pixel amount
   protected static float MAX_PIXEL_AMOUNT = 24f;

   // How long a fade in should take
   protected static float FADE_IN_DURATION = 16f;

   // How long a fade out should take
   protected static float FADE_OUT_DURATION = 12f;

   // The time at which the fade started
   protected float _fadeStartTime;

   // The duration of this fade
   protected float _fadeDuration = FADE_IN_DURATION;

   // Our desired pixel amount
   protected float _targetPixelAmount = MIN_PIXEL_AMOUNT;

   // Flags for checking if we are fading in or fading out.
   private bool _willFadeOut;
   private bool _willFadeIn;

   #endregion
}
