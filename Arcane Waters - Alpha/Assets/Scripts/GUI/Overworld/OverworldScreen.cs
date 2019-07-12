using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class OverworldScreen : Panel, IPointerClickHandler {
   #region Public Variables

   // Self
   public static OverworldScreen self;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;
   }

   public void OnPointerClick (PointerEventData eventData) {
      // If the black background outside is clicked, hide the panel
      if (eventData.rawPointerPress == this.gameObject) {
         PanelManager.self.popPanel();
      }
   }

   #region Private Variables

   #endregion
}
