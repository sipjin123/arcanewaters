using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AnimalPettingPuppetController : TemporaryController
{
   #region Public Variables

   #endregion

   public void startControlOverPlayer (NetEntity player, bool overrideMovement = false) {
      player.requestControl(this, overrideMovement);
   }

   protected override void onForceFastForward (ControlData puppet) {
      // Do nothing
   }

   protected override void startControl (ControlData puppet) {
      _direction = puppet.entity.facing;
   }

   protected override void controlUpdate (ControlData puppet) {
      // Force player to stay in place in the same direction when animal petting 
      if (puppet.entity.isLocalPlayer && !puppet.controlledMovementExternally) {
         puppet.entity.getRigidbody().MovePosition(puppet.startPos);
         puppet.entity.facing = _direction;
      }
   }

   public void stopAnimalPetting () {
      if (_puppets.Count == 1) {
         endControl(_puppets[0]);
      }
   }

   #region Private Variables

   // Direction that player will be forced to looked at (based on player's initial direction)
   private Direction _direction;

   #endregion
}
