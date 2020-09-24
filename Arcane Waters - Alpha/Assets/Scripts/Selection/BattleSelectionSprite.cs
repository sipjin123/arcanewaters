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

   #endregion

   void Start () {
      // Add our arrows to a list for easier reference
      _arrows = new List<SpriteRenderer>() { north, east, south, west };
   }

   void Update () {
      Battler selectedBattler = BattleSelectionManager.self.selectedBattler;

      // Start out hidden if we don't have a selected entity
      if (selectedBattler == null) {
         hide();
      }

      // Only support selections while in Battle
      if (!Global.isInBattle()) {
         selectedBattler = null;
         return;
      }

      // Do some extra stuff if we have a Battler selected
      if (selectedBattler != null) {
         // Keep it positioned under the selected Battler
         Vector3 targetPosition = selectedBattler.transform.position;
         targetPosition.z -= .001f;

         bool isBattlerJumping = selectedBattler.isJumping;

         // Freeze y axis if the battler is not attacking
         if (!isBattlerJumping) {
            targetPosition.y = initialYaxis;
         }

         // Disabled sprite UI if the battler is jumping
         spriteHolder.SetActive(!isBattlerJumping);

         this.transform.position = targetPosition + getOffset(selectedBattler);

         // Move the arrows around based on the orientation of our target
         setDistances(selectedBattler);

         // Set the color of the arrows
         setColorsForTarget(selectedBattler);

         // Make sure we're visible
         show();
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

   protected Vector3 getOffset (Battler selectedBattler) {
      /*if (selectedBattler is GolemBossBattler) {
         return new Vector2(0f, -.10f);
      }*/

      // Default
      return new Vector2(0f, -.12f);
   }

   #region Private Variables

   // A convenient list collection of our arrows
   protected List<SpriteRenderer> _arrows = new List<SpriteRenderer>();

   #endregion
}
