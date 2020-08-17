using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class VoyageSignboard : ToolTipSign
{
   #region Public Variables

   // The sort point of the sign
   public Transform sortPoint;

   #endregion

   protected override void Awake () {
      base.Awake();

      _outline = GetComponent<SpriteOutline>();
      _outline.setNewColor(Color.white);
      _outline.enabled = false;
   }

   public override void toggleToolTip (bool isActive) {
      base.toggleToolTip(isActive);

      _outline.setVisibility(isActive);
      _outline.enabled = isActive;
   }

   public void showVoyagePanel () {
      if (Global.player == null) {
         return;
      }

      // Only works when the player is close enough
      if (Vector2.Distance(sortPoint.position, Global.player.transform.position) > NPC.TALK_DISTANCE) {
         FloatingCanvas.instantiateAt(transform.position + new Vector3(0f, .5f)).asTooFar();
         return;
      }

      VoyageManager.self.showVoyagePanel(Global.player);
   }

   #region Private Variables

   // The outline of the sign
   protected SpriteOutline _outline;

   #endregion
}
