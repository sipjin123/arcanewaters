using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;

[RequireComponent(typeof(ClickableBox))]
public class ClickTrigger : MonoBehaviour {
   #region Public Variables

   #endregion

   private void Awake () {
      _entity = GetComponentInParent<SeaEntity>();
      _clickableBox = GetComponent<ClickableBox>();

      _clickableBox.mouseButtonDown += onMouseButtonDown;
      _clickableBox.mouseButtonUp += onMouseButtonUp;
   }

   private void OnDestroy () {
      if (_clickableBox != null) {
         _clickableBox.mouseButtonDown -= onMouseButtonDown;
         _clickableBox.mouseButtonUp -= onMouseButtonUp;
      }
   }

   private void onMouseButtonUp (MouseButton button) {
      if (button == MouseButton.Left) {
         if (SelectionManager.self.selectedEntity == _entity) {
            SelectionManager.self.setSelectedEntity(null);
         } else if (!_entity.isDead() && _entity != Global.player) {
            SelectionManager.self.setSelectedEntity(_entity);
         }

         SelectionManager.hasClickedOnObject = false;
      }
   }

   private void onMouseButtonDown (MouseButton button) {
      if (button == MouseButton.Left) {
         SelectionManager.hasClickedOnObject = true;
      }
   }

   #region Private Variables

   // The entity this click trigger belongs to
   private SeaEntity _entity;

   // The clickable box
   private ClickableBox _clickableBox;

   #endregion
}
