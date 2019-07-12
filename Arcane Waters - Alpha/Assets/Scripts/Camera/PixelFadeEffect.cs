using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;

public class PixelFadeEffect : MonoBehaviour {
   #region Public Variables

   // The Camera effect
   public CameraFilterPack_Pixel_Pixelisation camFX_Pixel;

   // Self
   public static PixelFadeEffect self;

   #endregion

   void Awake () {
      self = this;
   }

   void Update () {
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
      }
   }

   public bool isPixelated () {
      return camFX_Pixel.enabled && (_targetPixelAmount > MIN_PIXEL_AMOUNT || getPixelAmount() > MIN_PIXEL_AMOUNT);
   }

   public void fadeOut() {
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

   public void fadeIn() {
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

   #endregion
}
