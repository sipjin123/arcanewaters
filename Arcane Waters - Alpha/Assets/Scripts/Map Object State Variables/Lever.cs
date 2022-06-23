using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class Lever : VaryingStateObject
{
   #region Public Variables

   // The animator of the lever
   public SpriteRendererAnimator leverAnim;

   #endregion

   private void Update () {
      leverAnim.playBackwards = state.Equals("left");
   }

   protected override void clientInteract () {
      requestStateState(state.Equals("left") ? "right" : "left");
   }

   protected override void onStateChanged (string state) {
      if (_makesSound) {
         // Need proper sound, using random as placeholder!
         SoundEffectManager.self.playFmodSfx(SoundEffectManager.HOVER_CURSOR_GENERIC, transform.position);
      }
   }

   protected override void receiveMapEditorData (DataField[] dataFields) {
      foreach (DataField f in dataFields) {
         if (f.isKey(DataField.LEVER_MAKES_SOUND_KEY)) {
            if (f.tryGetBoolValue(out bool makesSound)) {
               _makesSound = makesSound;
            }
         }
      }
   }

   #region Private Variables

   // Current rotation in degrees
   private float _rotation = 0;

   // Does this lever make a sound when used by the player
   private bool _makesSound = true;

   #endregion
}
