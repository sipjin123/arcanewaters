using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

[CreateAssetMenu(fileName = "HatClipMaskGenerationSettings", menuName = "Hat ClipMask Generation Settings")]
public class HatClipMaskGenerationSettings : ScriptableObject {
   #region Public Variables

   // The set of settings
   public HatClipMaskGenerationSetting[] values;

   #endregion

   #region Private Variables

   #endregion
}
