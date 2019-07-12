using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class FadeOut : MonoBehaviour {
   #region Public Variables

   // The delay before we start fading out
   public float fadeDelay = 0f;

   // How long it should take us to fade out target
   public float fadeDuration = 1.8f;

   #endregion

   void Start () {
      _creationTime = Time.time;

      foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>()) {
         _renderers.Add(renderer);
      }

      foreach (Text text in GetComponentsInChildren<Text>()) {
         _texts.Add(text);
      }

      foreach (Image image in GetComponentsInChildren<Image>()) {
         _images.Add(image);
      }
   }

   void Update () {
      float timeSinceCreation = Time.time - _creationTime;
      float percent = (timeSinceCreation / fadeDuration);

      // Wait the specified delay before we begin
      if (timeSinceCreation < fadeDelay) {
         return;
      }

      // Fade out over time
      foreach (SpriteRenderer renderer in _renderers) {
         Util.setAlpha(renderer, Mathf.Lerp(1f, 0f, percent));
      }
      foreach (Image image in _images) {
         Util.setAlpha(image, Mathf.Lerp(1f, 0f, percent));
      }
      foreach (Text text in _texts) {
         Util.setAlpha(text, Mathf.Lerp(1f, 0f, percent));
      }

      // If we've reached 0 alpha, then destroy ourself
      if (timeSinceCreation > fadeDuration) {
         Destroy(this.gameObject);
      }
   }

   #region Private Variables

   // Any Sprite Renderers we have
   protected List<SpriteRenderer> _renderers = new List<SpriteRenderer>();

   // Any Images we have
   protected List<Image> _images = new List<Image>();

   // Any Texts we have
   protected List<Text> _texts = new List<Text>();

   // The time at which we were created
   protected float _creationTime;

   #endregion
}
