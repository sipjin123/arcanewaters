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
      // We only support entity selection in one of the combat modes
      if (SeaManager.combatMode != SeaManager.CombatMode.Select) {
         selectedEntity = null;
      }

      // If our target has died, deselect it
      if (selectedEntity != null && selectedEntity.isDead()) {
         selectedEntity = null;
      }
   }

   #region Private Variables

   #endregion
}
