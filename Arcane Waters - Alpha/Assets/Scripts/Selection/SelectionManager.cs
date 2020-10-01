using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SelectionManager : MonoBehaviour {
   #region Public Variables

   // The currently selected entity, if any
   public SeaEntity selectedEntity;

   // The selection sprite
   public SelectionSprite selectionSprite;

   // Gets set to true when we've clicked down on an object with a ClickTrigger
   public static bool hasClickedOnObject = false;

   // Self reference
   public static SelectionManager self;

   #endregion

   void Awake () {
      self = this;
   }

   void Update () {
      // We can only select entities if we're using the Select combat mode and we're in the sea and not dead
      if (SeaManager.combatMode != SeaManager.CombatMode.Select || Global.player == null || Global.player.isDead() || !(Global.player is SeaEntity)) {
         selectedEntity = null;
      }

      // If our target has died, deselect it
      if (selectedEntity != null && selectedEntity.isDead()) {
         selectedEntity = null;
      }
   }

   public void setSelectedEntity (SeaEntity entity) {
      selectedEntity = entity;

      if (entity != null && entity.guildId == BotShipEntity.PIRATES_GUILD_ID) {
         TutorialManager3.self.tryCompletingStep(TutorialTrigger.SelectPirateShip);
      }
   }

   #region Private Variables

   #endregion
}
