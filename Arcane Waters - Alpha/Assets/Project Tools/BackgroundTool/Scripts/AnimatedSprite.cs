using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AnimatedSprite : MonoBehaviour {
   #region Public Variables

   // Reference to simple animation
   public SimpleAnimation simpleAnimation;

   // Reference to the sprite renderer
   public SpriteRenderer spriteRenderer;

   // Sets the animation to loop
   public bool isLooping;

   // If this sprite will react upon collision
   public bool isInteractable;

   // Determines if the animation is currently playing
   public bool isInteracting;

   // The number of sprites required before the animation can be split
   public static int SPRITE_SPLIT_MARKER = 8;

   // Frame references
   public int min_idle_frame;
   public int max_idle_frame;
   public int min_interact_frame;
   public int max_interact_frame;

   // Cached sprite array
   public Sprite[] spriteArray;

   // Delay between sprite frames
   public static float SPRITE_DELAY = .025f;

   #endregion

   public void setToLoop () {
      isLooping = true;
      simpleAnimation.stayAtLastFrame = false;
      simpleAnimation.frameLengthOverride = .2f;
      simpleAnimation.enabled = true;
   }

   public void setToInteractable () {
      isInteractable = true;
   }

   public void setSpriteCount (int spriteCount) {
      if (spriteCount < SPRITE_SPLIT_MARKER) {
         min_idle_frame = 0;
         max_idle_frame = spriteCount;
      } else {
         min_idle_frame = 0;
         max_idle_frame = (spriteCount / 2) - 1;
         min_interact_frame = spriteCount / 2;
         max_interact_frame = spriteCount;
      }

      spriteArray = ImageManager.getSprites(spriteRenderer.sprite.texture);
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      // Trigger interact animation when Clickable Box collides with this object (ClickableBox is the collider attached to all battlers)
      if (isInteractable && !isInteracting && collision.GetComponent<ClickableBox>() != null) {
         isInteracting = true;
         simpleAnimation.isPaused = true;
         StartCoroutine(CO_HandleInteractAnimation());
      }
   }

   private void OnTriggerExit2D (Collider2D collision) { 
      // Trigger interact animation when Clickable Box collides with this object (ClickableBox is the collider attached to all battlers)
      if (isInteractable && !isInteracting && collision.GetComponent<ClickableBox>() != null) {
         isInteracting = true;
         simpleAnimation.isPaused = true;
         StartCoroutine(CO_HandleInteractAnimation());
      }
   }

   private IEnumerator CO_HandleInteractAnimation () {
      int spriteIndex = min_interact_frame;

      while (spriteIndex < max_interact_frame) {
         spriteRenderer.sprite = spriteArray[spriteIndex];
         spriteIndex++;
         yield return new WaitForSeconds(SPRITE_DELAY);
      }

      spriteIndex = max_interact_frame - 1;
      while (spriteIndex > min_interact_frame) {
         yield return new WaitForSeconds(SPRITE_DELAY);
         spriteRenderer.sprite = spriteArray[spriteIndex];
         spriteIndex--;
      }

      resetPassiveAnimation();
   }

   private void resetPassiveAnimation () {
      isInteracting = false;
      simpleAnimation.isPaused = false;
      simpleAnimation.minIndex = min_idle_frame;
      simpleAnimation.maxIndex = max_idle_frame;
      simpleAnimation.resetAnimation();
   }

   #region Private Variables

   #endregion
}
