using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Events;
using DG.Tweening;

public class PixelFadeEffect : MonoBehaviour, IScreenFader {
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

   public float fadeOut () {
      // Make sure the effect is enabled
      camFX_Pixel.enabled = true;

      // If we're currently fading in or out, stop
      _fadeTween?.Kill();

      // Get the current pixel amount
      float pixelAmount = getPixelAmount();

      // Tween the pixel amount towards MAX_PIXEL_AMOUNT
      _fadeTween = DOTween.To(() => pixelAmount, x => pixelAmount = x, MAX_PIXEL_AMOUNT, FADE_OUT_DURATION);

      // Make it increase linearly instead of smoothly
      _fadeTween.SetEase(Ease.Linear);

      // Update the value of pixel amount on each step
      _fadeTween.OnUpdate(() => setNewPixelAmount(pixelAmount));

      // Start a darkness color fade as well
      BrightnessManager.self.setNewTargetIntensity(0f, 1f);
            
      return FADE_OUT_DURATION;
   }

   public float fadeIn () {
      if (getPixelAmount() <= MIN_PIXEL_AMOUNT) {
         return 0;
      }

      // Make sure the effect is enabled
      camFX_Pixel.enabled = true;

      _fadeTween?.Kill();
      float pixelAmount = getPixelAmount();

      _fadeTween = DOTween.To(() => pixelAmount, x => pixelAmount = x, MIN_PIXEL_AMOUNT, FADE_IN_DURATION);
      _fadeTween.OnUpdate(() => setNewPixelAmount(pixelAmount));
      _fadeTween.SetEase(Ease.Linear);
      _fadeTween.OnComplete(() => camFX_Pixel.enabled = false);

      // Start a darkness color fade as well
      BrightnessManager.self.setNewTargetIntensity(1f, 1f);

      return FADE_IN_DURATION;
   }

   protected void setNewPixelAmount (float newPixelAmount) {
      CameraFilterPack_Pixel_Pixelisation.ChangePixel = newPixelAmount;
   }

   protected float getPixelAmount () {
      return CameraFilterPack_Pixel_Pixelisation.ChangePixel;
   }

   public float getFadeInDuration () {
      return FADE_IN_DURATION;
   }

   public float getFadeOutDuration () {
      return FADE_OUT_DURATION;
   }

   #region Private Variables

   // Our minimum pixel amount
   protected static float MIN_PIXEL_AMOUNT = .6f;

   // Our maximum pixel amount
   protected static float MAX_PIXEL_AMOUNT = 24f;

   // How long a fade in should take
   protected static float FADE_IN_DURATION = 1f;

   // How long a fade out should take
   protected static float FADE_OUT_DURATION = 1f;

   // The sequence handling the fade in/out
   private Tween _fadeTween;

   #endregion
}
