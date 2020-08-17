using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class VersionInfoUI : MonoBehaviour {
   #region Public Variables

   #endregion

   private void Start () {
      int gameVersion = Util.getGameVersion();

      // Display version information only if build manifest is available
      if (gameVersion != int.MaxValue) {
         _text.SetText($"v { Util.getGameVersion() }");
      } else {
         transform.parent.gameObject.SetActive(false);
      }
   }

   #region Private Variables

   // The text that will display version information
   [SerializeField]
   private TMPro.TextMeshProUGUI _text;

   #endregion
}
