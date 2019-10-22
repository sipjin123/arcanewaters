using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CombatEffect : MonoBehaviour {
   #region Public Variables

   // Main component that will show the VFX
   public SpriteRenderer effectRenderer;

   #endregion

   void Start () {
      // Routinely change our sprite
      InvokeRepeating("changeSprite", 0f, _timePerFrame);
   }

   public void initEffect(Sprite[] effectSprites, float timePerFrame) {
      _sprites = effectSprites;
      _timePerFrame = timePerFrame;
   }

   protected void changeSprite () {
      _index++;

      // If we've reached the end, destroy the sprite
      if (_index >= _sprites.Length) {
         Destroy(this.gameObject);
      } else {
         effectRenderer.sprite = _sprites[_index];
      }
   }

   #region Private Variables

   // Our set of sprites, will be coming from an scriptable object.
   private Sprite[] _sprites;

   // Our current sprite index
   private int _index;

   // The time of each sprite frame, larger value = slower animation speed.
   private float _timePerFrame;

   #endregion
}
