using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;

public class BattleSelectionManager : MonoBehaviour {
   #region Public Variables

   // The currently selected Battler, if any
   public Battler selectedBattler;

   // The selection sprite
   public BattleSelectionSprite selectionSprite;

   // Self reference
   public static BattleSelectionManager self;

   #endregion

   void Awake () {
      self = this;
   }

   void Update () {
      // If our target has died, deselect it
      if (selectedBattler != null && selectedBattler.isDead()) {
         selectedBattler.deselectThis();
         selectedBattler = null;
      }

      // Whenever a mouse-click ends, check if it was on a Battle sprite
      if (Input.GetMouseButtonUp(0) && !Util.isButtonClick() && Global.isInBattle()) {
         if (Global.player == null || !(Global.player is PlayerBodyEntity)) {
            return;
         }

         // Look up the player's Battle ID
         PlayerBodyEntity body = (PlayerBodyEntity) Global.player;
         int battleId = body.battleId;

         // Find the player's Battle object
         Battle playerBattle = null;

         foreach (Battle battle in FindObjectsOfType<Battle>()) {
            if (battle.battleId == battleId) {
               playerBattle = battle;
            }
         }

         if (playerBattle == null || playerBattle.battleId <= 0) {
            return;
         }

         // Check where on the screen this click was
         Vector3 clickLocation = BattleCamera.self.getCamera().ScreenToWorldPoint(Input.mousePosition);
         bool clickedBattler = false;

         // Cycle over each of the Battlers in the Battle
         foreach (Battler battler in playerBattle.getParticipants()) {
            // Gets the Bounds of the sprite, but scale it down a bit since it contains a lot of transparent border
            Bounds bounds = battler.clickBox.bounds;

            // We don't care about the Z location for the purpose of Contains(), so make the click Z match the bounds Z
            clickLocation.z = bounds.center.z;

            // Check if the click was within the sprite bounds of this Battler
            if (bounds.Contains(clickLocation) && !battler.isDead()) {
               clickedBattler = true;

               // Check if the newly selected battler is the same as the previous one, we deselect it
               if (selectedBattler == battler) {
                  selectedBattler.deselectThis();
                  selectedBattler = null;
               } else {
                  // If it is another battler
                  if (selectedBattler != null) {
                     selectedBattler.deselectThis();
                  }
                  
                  selectedBattler = battler;
                  selectedBattler.selectThis();
               }

               break;
            }
         }

         // If no Battler was clicked on, then select nothing
         if (!clickedBattler) {
            if(selectedBattler != null) {
               selectedBattler.deselectThis();
            }
            selectedBattler = null;
         }
      }
   }

   #region Private Variables

   #endregion
}
