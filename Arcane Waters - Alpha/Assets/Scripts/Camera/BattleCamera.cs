using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleCamera : BaseCamera {
   #region Public Variables

   // Self
   public static BattleCamera self;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;
   }

   private void Start () {
      // Check if the current resolution width is bigger than 1024, or the height greater than 768
      float sizeDiff = Mathf.Max(Screen.width / 1024f, Screen.height / 768f);

      // We need to increase the camera scale so that the battle screen (normally at 3x scale) fills the entire screen
      if (sizeDiff > 0f) {
         setOrthoSize(sizeDiff * 3f);
      }
   }

   #region Private Variables

   #endregion
}
