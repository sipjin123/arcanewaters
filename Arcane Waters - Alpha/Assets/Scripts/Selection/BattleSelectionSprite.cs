using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;
using Mirror;

public class BattleSelectionSprite : MonoBehaviour {
   #region Public Variables

   // The four sprites that we display
   public SpriteRenderer north;
   public SpriteRenderer east;
   public SpriteRenderer south;
   public SpriteRenderer west;

   // Caches the y axis so when the battler pops up, the battle selector stays on the ground
   public float initialYaxis;

   // Holds the sprite content
   public GameObject spriteHolder;

   // Sprites that is determined by the selected battler if is an ally or enemy
   public GameObject enemySelection, allySelection;

   // Sprite Render used for the enemy
   public SpriteRenderer currentEnemySprite;

   // Sprites that are swapped depending on the size of the enemy
   public Sprite smallEnemyTarget, largeEnemyTarget;

   #endregion

   void Start () {
      // Add our arrows to a list for easier reference
      _arrows = new List<SpriteRenderer>() { north, east, south, west };
   }

   void Update () {
      Battler selectedBattler = BattleSelectionManager.self.selectedBattler;
      enemySelection.SetActive(false);
      allySelection.SetActive(false);

      // Start out hidden if we don't have a selected entity
      if (selectedBattler == null || selectedBattler.isDead()) {
         hide();
      }

      // Only support selections while in Battle
      if (!Global.isInBattle()) {
         selectedBattler = null;
         return;
      }

      // Do some extra stuff if we have a Battler selected
      if (selectedBattler != null) {
         Battler currentBattler = BattleManager.self.getBattler(Global.player.userId);

         if (currentBattler == null) {
            return;
         }

         if (!selectedBattler.isJumping) {
            // Keep it positioned under the selected Battler
            Vector3 targetPosition = selectedBattler.transform.position;
            targetPosition.z -= .001f;
            this.transform.position = targetPosition + BattleSelectionManager.getOffsetToFeet();
         }

         Util.setLocalZ(this.transform, 1.6f);

         // Move the arrows around based on the orientation of our target
         setDistances(selectedBattler);

         // Set the color of the arrows
         setColorsForTarget(selectedBattler);

         // Make sure we're visible
         show();

         // Show the appropriate target sprite
         bool isAlly = currentBattler.teamType == selectedBattler.teamType;
         enemySelection.SetActive(!isAlly);
         allySelection.SetActive(isAlly);

         if (!isAlly) {
            currentEnemySprite.sprite = selectedBattler.isBossType ? largeEnemyTarget : smallEnemyTarget;
         }
      }
   }

   public void show () {
      foreach (SpriteRenderer arrow in _arrows) {
         arrow.enabled = true;
      }
   }

   public void hide () {
      foreach (SpriteRenderer arrow in _arrows) {
         arrow.enabled = false;
      }
   }

   protected void setDistances (Battler selectedBattler) {
      // Default distances
      float horizontalDistance = .20f;
      float verticalDistance = .20f;

      // Spread things out for large sprites
      /*if (selectedBattler is GolemBossBattler) {
         horizontalDistance = .55f;
      }*/

      // A small offset applied to make the arrows bounce
      float offset = (NetworkTime.time % 1 > .5f) ? .01f : -.01f;

      north.transform.localPosition = new Vector3(0f, verticalDistance + offset, north.transform.localPosition.z);
      south.transform.localPosition = new Vector3(0f, -verticalDistance - offset, south.transform.localPosition.z);
      east.transform.localPosition = new Vector3(horizontalDistance + offset, 0f, east.transform.localPosition.z);
      west.transform.localPosition = new Vector3(-horizontalDistance - offset, 0f, west.transform.localPosition.z);
   }

   protected void setColorsForTarget (Battler selectedBattler) {
      Battler playerBattler = BattleManager.self.getPlayerBattler();

      // Default color
      setColors(Color.gray);

      // Red if it's an Enemy
      if (playerBattler != null && playerBattler.teamType != selectedBattler.teamType) {
         setColors(Util.getColor(255, 90, 90));
      } else {
         setColors(Util.getColor(90, 255, 90));
      }
   }

   protected void setColors (Color color) {
      foreach (SpriteRenderer arrow in _arrows) {
         arrow.color = color;
      }
   }

   #region Private Variables

   // A convenient list collection of our arrows
   protected List<SpriteRenderer> _arrows = new List<SpriteRenderer>();

   #endregion
}
