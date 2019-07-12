using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Effect : MonoBehaviour {
   #region Public Variables

   // The Type of effect
   public enum Type {
      Crop_Dirt_Large = 1, Crop_Dirt_Small = 2, Crop_Water = 3, Crop_Harvest = 4, Crop_Shine = 5,
      Gate_Damage_1 = 6, Gate_Damage_2 = 7, Gate_Damage_3 = 8, Gate_Damage_4 = 9, Freeze = 10,
      Pickup_Effect = 11, Item_Discovery_Particles = 12, Cannon_Smoke = 13, Ranged_Fire = 14,
      Ranged_Air = 15, Poof = 16,
      Slam_Physical = 17, Slash_Physical = 18, Slash_Fire = 19, Slash_Ice = 20, Slash_Lightning = 21,
      Blunt_Physical = 22, Block = 23, Hit = 24,
   }

   // The Type of effect this is
   public Type effectType;

   #endregion

   void Start () {
      // Look up components
      _renderer = GetComponent<SpriteRenderer>();

      // Load our sprites
      string path = "Effects/" + effectType;
      _sprites = ImageManager.getSprites(path);

      // Routinely change our sprite
      InvokeRepeating("changeSprite", 0f, EffectManager.self.getTimePerFrame(effectType));
   }

   protected void changeSprite () {
      _index++;

      // If we've reached the end, destroy the sprite
      if (_index >= _sprites.Length) {
         Destroy(this.gameObject);
      } else {
         _renderer.sprite = _sprites[_index];
      }
   }

   #region Private Variables

   // Our Renderer
   protected SpriteRenderer _renderer;

   // Our set of sprites
   protected Sprite[] _sprites;

   // Our current sprite index
   protected int _index;

   #endregion
}
