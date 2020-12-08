using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class GUI_SoundEffects_Toggle : GUI_SoundEffects
{
   #region Public Variables

   #endregion

   private void Awake () {
      _toggle = GetComponent<Toggle>();
      if (_toggle != null) {
         _toggle.onValueChanged.AddListener(_ => {
            SoundManager.play2DClip(SoundManager.Type.GUI_Press);
         });
      }
   }

   #region Private Variables

   // Our toggle UI component
   private Toggle _toggle;

   #endregion
}
