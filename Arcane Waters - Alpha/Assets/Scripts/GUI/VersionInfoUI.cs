using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class VersionInfoUI : MonoBehaviour {
   #region Public Variables

   #endregion

   private void Start () {
      _text.SetText($"Version info: { Util.getGameVersion() }");
   }

   #region Private Variables

   // The text that will display version information
   [SerializeField]
   private TMPro.TextMeshProUGUI _text;

   #endregion
}
