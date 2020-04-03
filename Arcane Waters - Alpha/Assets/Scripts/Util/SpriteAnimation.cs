using UnityEngine;
using UnityEngine.UI;

public class SpriteAnimation : ClientMonoBehaviour
{
   #region Public Variables

   // Our set of sprites
   public Sprite[] sprites;

   // A custom defined speed we set in the editor
   public float frameDuration = 0.15f;

   // An initial delay before the animation starts playing
   public float initialDelay = 0f;

   // Whether we should be destroyed after the animation finishes
   public bool destroyAtEnd = false;

   // Whether we should stay at the last frame
   public bool stayAtLastFrame = false;

   // How long we should wait after the animation ends before looping
   public float loopDelay = 0f;

   // Whether we want to temporarily pause the animation
   public bool isPaused = false;

   #endregion

   protected override void Awake () {
      base.Awake();

      // Look up components
      _renderer = GetComponent<SpriteRenderer>();
      _image = GetComponent<Image>();
   }

   private void Start () {
      if (!Util.isBatch()) {
         // Routinely change our sprite
         if (sprites.Length > 0) {
            InvokeRepeating("changeSprite", initialDelay, frameDuration);
         }
      }
   }


   protected void changeSprite () {
      // If we've been temporarily paused, don't do anything
      if (isPaused || !enabled) {
         return;
      }

      // Check if we reached the end
      if (index == sprites.Length - 1) {
         // Destroy if we need to
         if (destroyAtEnd) {
            Destroy(gameObject);
            return;
         }

         // Otherwise, if it is the last frame, stop animating
         if (stayAtLastFrame) {
            CancelInvoke();
            return;
         }

         // Otherwise, if we need to delay, delay the next animation loop
         if (loopDelay > 0) {
            CancelInvoke();
            InvokeRepeating("changeSprite", loopDelay, frameDuration);
         }

         // Otherwise, reset the index and let the animation continue
         index = -1;
      }

      index++;
      setSprite(sprites[index]);
   }

   protected void setSprite (Sprite sprite) {
      if (_renderer != null) {
         _renderer.sprite = sprite;
      }

      if (_image != null) {
         _image.sprite = sprite;
      }
   }

   #region Private Variables

   // Our Renderer
   protected SpriteRenderer _renderer;

   // Our Image (if any)
   protected Image _image;

   // Our current sprite index
   protected int index = -1;

   #endregion
}
