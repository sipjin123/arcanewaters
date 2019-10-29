using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShipEntity : SeaEntity {
   #region Public Variables

   // The Type of ship this is
   [SyncVar]
   public Ship.Type shipType;

   // The Skin Type of the ship
   [SyncVar]
   public Ship.SkinType skinType;

   // How fast this ship goes
   [SyncVar]
   public int speed = 100;

   // The range of attack
   [SyncVar]
   public float attackRange = 1.5f;

   // The number of sailors it takes to run this ship
   [SyncVar]
   public int sailors;

   // The Rarity of the ship
   public Rarity.Type rarity;

   #endregion

   public override void playAttackSound () {
      // Play a sound effect
      SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Ship_Cannon_1, this.transform.position);
   }

   public void updateSkin (Ship.SkinType newSkinType) {
      this.skinType = newSkinType;

      StartCoroutine(CO_UpdateAllSprites());
   }

   public bool canUseSkin (Ship.SkinType newSkinType) {
      string skinClass = newSkinType.ToString().Split('_')[0];

      return this.shipType.ToString().ToLower().Contains(skinClass.ToLower());
   }

   public override float getMoveSpeed () {
      // Start with the base speed for all sea entities
      float baseSpeed = base.getMoveSpeed();

      // Increase or decrease our speed based on the settings for this ship
      return baseSpeed * (this.speed / 100f);
   }

   public override float getTurnDelay () {
      switch (this.shipType) {
         case Ship.Type.Caravel:
            return .25f;
         case Ship.Type.Brigantine:
            return .30f;
         case Ship.Type.Nao:
            return .35f;
         case Ship.Type.Carrack:
            return .40f;
         case Ship.Type.Cutter:
            return .45f;
         case Ship.Type.Buss:
            return .50f;
         case Ship.Type.Galleon:
            return .60f;
         case Ship.Type.Barge:
            return .75f;
         default:
            return .25f;
      }
   }

   [Server]
   public override void fireAtSpot (Vector2 spot, Attack.Type attackType, float attackDelay, float launchDelay, Vector2 spawnPosition = new Vector2(), bool isLastProjectile = true) {
      // The target point is clamped to the attack range
      spot = AttackManager.clampToRange(transform.position, spot, attackRange);

      // Run the base function
      base.fireAtSpot(spot, attackType, attackDelay, launchDelay, spawnPosition, isLastProjectile);
   }

   #region Private Variables

   #endregion
}
