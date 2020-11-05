using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class DebugObjMapInGame : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   // The map editor ID
   public int mapEditorId;

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.DEBUG_OBJECT_ID) == 0) {
            D.debug("Received new Map Entry: " + field.v);
            mapEditorId = int.Parse(field.v);
         }
      }
   }

   #endregion

   #region Private Variables

   #endregion
}