using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;

public class ClickTrigger : MonoBehaviour {
   #region Public Variables

   #endregion

   void Start () {
      // Set our name and position
      this.name = "Click Trigger";
      this.transform.localPosition = new Vector3(0f, 0f, -.1f);
   }

   void Update () {
      if (SeaManager.combatMode != SeaManager.CombatMode.Select) {
         return;
      }

      // If we just clicked down on a trigger, take note
      if (Input.GetMouseButtonDown(0)) {
         Vector2 downClickLocation = Camera.main.ScreenToWorldPoint(Input.mousePosition);

         // If the click was close to our position, it was this ship
         if (Vector2.Distance(downClickLocation, this.transform.position) < .15f) {
            SelectionManager.hasClickedOnObject = true;
         }
      }

      // Check if the mouse was released over this click trigger
      if (Input.GetMouseButtonUp(0)) {
         SelectionManager.hasClickedOnObject = false;
         Vector2 clickLocation = Camera.main.ScreenToWorldPoint(Input.mousePosition);

         // If the click was close to this entity's position, select this entity
         if (Vector2.Distance(clickLocation, this.transform.position) < .15f) {
            SeaEntity clickedEntity = this.GetComponentInParent<SeaEntity>();

            // If we clicked on the already selected entity, unselect it
            if (clickedEntity == SelectionManager.self.selectedEntity) {
               SelectionManager.self.selectedEntity = null;
            } else {
               if (!clickedEntity.isDead()) {
                  SelectionManager.self.selectedEntity = clickedEntity;
               }
            }
         }
      }
   }

   #region Private Variables

   #endregion
}
