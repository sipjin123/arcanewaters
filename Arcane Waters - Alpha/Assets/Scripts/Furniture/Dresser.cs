using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Dresser : ClientMonoBehaviour, IBiomable
{
   #region Public Variables

   // The sprite to show when we're open
   public Sprite openSprite;

   // The sprite to show when we're closed
   public Sprite closedSprite;

   // Whether we're open or closed
   public bool isOpen = false;

   // Current biome that is set
   public Biome.Type currentBiome = Biome.Type.Forest;

   #endregion

   protected override void Awake () {
      base.Awake();

      if (enabled) {
         _renderer = GetComponent<SpriteRenderer>();
      }
   }

   private void Update () {
      _renderer.sprite = isOpen ? openSprite : closedSprite;
   }

   void OnTriggerEnter2D (Collider2D other) {
      BodyEntity player = other.transform.GetComponent<BodyEntity>();

      if (player == null) {
         return;
      }

      // If it's our player, maybe get dressed
      if (player.isLocalPlayer && !player.armorManager.hasArmor()) {
         PlayerBodyEntity localPlayer = (PlayerBodyEntity) player;
         localPlayer.Cmd_ToggleClothes();

         // Show an effect
         GameObject sortPoint = GetComponent<ZSnap>().sortPoint;
         EffectManager.show(Effect.Type.Item_Discovery_Particles, sortPoint.transform.position - new Vector3(0f, .12f));
      }

      isOpen = true;

      // Play a sound
      //SoundManager.create3dSound("door_open_", this.transform.position, 3);
   }

   void OnTriggerExit2D (Collider2D other) {
      BodyEntity player = other.transform.GetComponent<BodyEntity>();

      if (player == null) {
         return;
      }

      isOpen = false;

      // Play a sound
      //SoundManager.create3dSound("door_close_", this.transform.position, 3);
   }

   public void setBiome (Biome.Type biomeType) {
      openSprite = Util.switchSpriteBiome(openSprite, currentBiome, biomeType);
      closedSprite = Util.switchSpriteBiome(closedSprite, currentBiome, biomeType);

      if (_renderer != null) {
         _renderer.sprite = Util.switchSpriteBiome(_renderer.sprite, currentBiome, biomeType);
      }

      currentBiome = biomeType;
   }

   #region Private Variables

   // Our renderer
   protected SpriteRenderer _renderer;

   #endregion
}
