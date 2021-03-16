using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;

public enum Layer {
   Body = 100, Eyes = 101, Hair = 102, Armor = 103, Weapon = 104,
   Hull = 200, Masts = 201, Sails = 202, Flags = 203, Ripple = 204,
}

public class SpriteLayer : RecoloredSprite {
   #region Public Variables

   // Event triggered when texture is finished swapping
   public UnityEvent textureSwappedEvent = new UnityEvent();

   #endregion

   public override void Awake () {
      base.Awake();
      
      _renderer = GetComponent<SpriteRenderer>();
      _spriteSwap = GetComponent<SpriteSwap>();
   }

   public SpriteRenderer getRenderer () {
      return _renderer;
   }

   public SpriteSwap getSpriteSwap () {
      return _spriteSwap != null ? _spriteSwap : (_spriteSwap = GetComponent<SpriteSwap>());
   }

   public SimpleAnimation getSimpleAnimation () {
      return _simpleAnimation != null ? _simpleAnimation : (_simpleAnimation = GetComponent<SimpleAnimation>());
   }

   public void setTexture (Texture2D newTexture) {
      getSpriteSwap().newTexture = newTexture;

      SimpleAnimation animation = getSimpleAnimation();
      
      if (animation != null) {
         animation.setNewTexture(newTexture);
      }
      textureSwappedEvent.Invoke();
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
         setTexture(emptySprite);
      } else {
         // Set the new texture and wait another frame
         setTexture(newTexture);
      }

      yield return new WaitForEndOfFrame();

      // Now we can enable the sprite again
      if (_renderer != null) {
         _renderer.enabled = true;
      }
      textureSwappedEvent.Invoke();
   }

   protected PlayerBodyEntity getPlayer () {
      if (_player == null) {
         _player = GetComponentInParent<PlayerBodyEntity>();
      }

      return _player;
   }

   #region Private Variables

   // The sprite renderer we use for displaying animations
   protected SpriteRenderer _renderer;

   // The sprite swap used for animated sprites
   protected SpriteSwap _spriteSwap;

   // The simple animation component
   protected SimpleAnimation _simpleAnimation;

   // The stencil reference ID used for hiding hair behind the hat
   protected const int HAT_STENCIL_ID = 3;

   // A reference to the player
   protected PlayerBodyEntity _player;

   #endregion
}
