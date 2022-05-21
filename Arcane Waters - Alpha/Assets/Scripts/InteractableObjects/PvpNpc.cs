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

   // A reference to the simple animation component for this npc
   public SimpleAnimation npcAnimation;

   #endregion

   private void Start () {
      _outline = GetComponentInChildren<SpriteOutline>();
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.NPC_DIRECTION_KEY) == 0) {
            try {
               Direction newDirection = (Direction) System.Enum.Parse(typeof(Direction), field.v.Split(':')[0]);
               updateFacingDirection(newDirection);
            } catch {

            }
         }
      }
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

   private void updateFacingDirection (Direction newFacingDirection) {
      int directionIndex = (int) newFacingDirection;
      npcAnimation.minIndex = _idleStartFramesByDirection[directionIndex];
      npcAnimation.maxIndex = _idleEndFramesByDirection[directionIndex];
   }

   #region Private Variables

   // The outline component
   protected SpriteOutline _outline;

   // Start and end frames for idle animations, indexed by direction enum
   private readonly int[] _idleStartFramesByDirection = { 4, 0, 0, 0, 8, 0, 0, 0 };
   private readonly int[] _idleEndFramesByDirection = { 7, 3, 3, 3, 11, 3, 3, 3 };

   #endregion
}
