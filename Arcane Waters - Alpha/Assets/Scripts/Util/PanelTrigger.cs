using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;

public class PanelTrigger : MonoBehaviour {
   #region Public Variables

   // The panel type to use
   public Panel.Type panelType;

   #endregion

   protected virtual void Awake () {
      // We don't want to waste time on Client scripts when the server is running in Batch Mode
      if (Util.isBatch()) {
         this.enabled = false;
      }
   }

   void OnTriggerEnter2D (Collider2D other) {
      NetEntity entity = other.GetComponent<NetEntity>();

      // Only do this for our own player
      if (entity != null && entity == Global.player) {
         PanelManager.self.pushPanel(panelType);
      }
   }

   #region Private Variables

   #endregion
}
