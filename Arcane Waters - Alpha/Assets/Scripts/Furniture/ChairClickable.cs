﻿using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ChairClickable : MonoBehaviour
{
   #region Public Variables

   // How close we have to be in order to sit down
   public static float SIT_DISTANCE = .32f;

   // The direction of the chair (only cardinal direction are supported)
   public Direction direction = Direction.North;

   #endregion

   private void Awake () {
      // Look up components
      _outline = GetComponentInChildren<SpriteOutline>();
      _clickableBox = GetComponentInChildren<ClickableBox>();
   }

   public void Update () {
      if (Util.isBatch()) {
         return;
      }

      // Figure out whether our outline should be showing
      handleSpriteOutline();
   }

   private bool isOccupied () {
      // The chair is considered occupied if any player is very close to the chair and is sitting
      List<NetEntity> entities = EntityManager.self.getAllEntities();

      foreach (NetEntity entity in entities) {
         if (entity is PlayerBodyEntity body) {
            if (body.isSitting && Util.AreVectorsAlmostTheSame(transform.position, body.chairPosition)) {
               return true;
            }
         }
      }

      return false;
   }

   public void onClick () {
      if (Global.player == null || Global.player.getPlayerBodyEntity() == null || Global.player.getPlayerBodyEntity().isSitting) {
         return;
      }

      if (isTooFarFromPlayer()) {
         FloatingCanvas.instantiateAt(transform.position + new Vector3(0f, 0f)).asTooFar();
         return;
      }

      if (isOccupied()) {
         return;
      }

      Global.player.getPlayerBodyEntity().Cmd_EnterChair(transform.position, direction);
   }

   private bool isTooFarFromPlayer () {
      return Vector2.Distance(transform.position, Global.player.transform.position) > SIT_DISTANCE;
   }

   public void handleSpriteOutline () {
      if (_outline == null || _clickableBox == null) {
         return;
      }

      // Only show our outline when the mouse is over us
      bool isHovering = MouseManager.self.isHoveringOver(_clickableBox);
      _outline.setVisibility(isHovering);
   }

   #region Private Variables

   // Outline of clock object
   protected SpriteOutline _outline;

   // Button which is used to check if mouse is above it
   protected ClickableBox _clickableBox;

   #endregion
}