using UnityEngine;

public class CharacterScreenCamera : BaseCamera
{
   #region Public Variables

   #endregion

   private void Start () {
      onResolutionChanged();
   }

   public override void onResolutionChanged () {
      _vcam.m_Lens.OrthographicSize = (Screen.height / 300f) * 0.5f;
   }

   #region Private Variables

   #endregion
}
