using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class DB_MainTester : MonoBehaviour {
   #region Public Variables

   #endregion

   private void OnGUI () {
      if (GUILayout.Button("Fetch DB_Main")) {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            var result = DB_Main.getArmor(745);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               D.editorLog("Thred: " + result.id+" : "+result.itemTypeId);
            });
         });
      }
   }

   #region Private Variables

   #endregion
}
