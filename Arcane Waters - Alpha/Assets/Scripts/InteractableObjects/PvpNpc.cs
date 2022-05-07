using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class PvpNpc : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   // How close we have to be in order to interact
   public static float INTERACT_DISTANCE = .75f;

   #endregion

   private void Start () {
      _outline = GetComponentInChildren<SpriteOutline>();
   }

   public void receiveData (DataField[] dataFields) {
      throw new System.NotImplementedException();
   }

   public void onHoverObject () {
      _outline.setVisibility(true);
   }

   public void onHoverObjectExit () {
      _outline.setVisibility(false);
   }

   public void onClickedObject () {
      if (Global.player == null) {
         return;
      }
      if (Vector2.Distance(Global.player.transform.position, transform.position) < INTERACT_DISTANCE) {
         // Enable pvp panel
         PanelManager.self.linkIfNotShowing(Panel.Type.PvpNpc);
      }
   }

   #region Private Variables

   // The outline component
   protected SpriteOutline _outline;

   #endregion
}
