using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public enum Layer {
   Body = 100, Eyes = 101, Hair = 102, Armor = 103, Weapon = 104,
   Hull = 200, Masts = 201, Sails = 202, Flags = 203, Ripple = 204,
}

public class SpriteLayer : RecoloredSprite {
   #region Public Variables

   #endregion

   public override void Awake () {
      base.Awake();
      
      _renderer = GetComponent<SpriteRenderer>();
      _spriteSwap = GetComponent<SpriteSwap>();
   }

   public SpriteRenderer getRenderer () {
      return _renderer;
   }

   protected SpriteSwap getSpriteSwap () {
      return _spriteSwap ?? (_spriteSwap = GetComponent<SpriteSwap>());
   }

   protected IEnumerator CO_SwapTexture (Texture2D newTexture) {
      // Hide our sprite and wait a frame
      if (_renderer != null) {
         _renderer.enabled = false;
         yield return new WaitForEndOfFrame();
      }

      if (newTexture == null) {
         Texture2D emptySprite = ImageManager.getTexture("Assets/Sprites/empty_layer");

         // Set the new texture and wait another frame
         GetComponent<SpriteSwap>().newTexture = emptySprite;
      } else {
         // Set the new texture and wait another frame
         GetComponent<SpriteSwap>().newTexture = newTexture;
      }
      yield return new WaitForEndOfFrame();

      // Now we can enable the sprite again
      if (_renderer != null) {
         _renderer.enabled = true;
      }
   }

   #region Private Variables

   // The sprite renderer we use for displaying animations
   protected SpriteRenderer _renderer;

   // The sprite swap used for animated sprites
   protected SpriteSwap _spriteSwap;

   #endregion
}
