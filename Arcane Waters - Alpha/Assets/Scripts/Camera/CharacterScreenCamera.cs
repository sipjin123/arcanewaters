using UnityEngine;

public class CharacterScreenCamera : MyCamera
{
   #region Public Variables

   #endregion

   protected override void Start () {
      base.Start();
      onResolutionChanged();
   }

   public override void onResolutionChanged () {
      base.onResolutionChanged();
   }

   #region Private Variables

   #endregion
}
