using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class XmlDataToolManager : MonoBehaviour {
   #region Public Variables

   // Holds the collection of user id that created the data entry
   public List<SQLEntryNameClass> userNameData = new List<SQLEntryNameClass>();

   // Holds the collection of user id that created the data entry
   public List<SQLEntryIDClass> userIdData = new List<SQLEntryIDClass>();

   // Determines the type of tool this manager is
   public EditorSQLManager.EditorToolType editorToolType;

   #endregion

   public bool didUserCreateData (string entryName) {
      SQLEntryNameClass sqlEntry = userNameData.Find(_ => _.dataName == entryName);
      if (sqlEntry != null) {
         if (sqlEntry.ownerID == MasterToolAccountManager.self.currentAccountID) {
            return true;
         }
      } else {
         Debug.LogWarning("Entry does not exist: " + entryName);
      }

      return false;
   }

   public bool didUserCreateData (int entryID) {
      SQLEntryIDClass sqlEntry = userIdData.Find(_ => _.dataID == entryID);
      if (sqlEntry != null) {
         if (sqlEntry.ownerID == MasterToolAccountManager.self.currentAccountID) {
            return true;
         }
      } else {
         Debug.LogWarning("Entry does not exist: " + entryID);
      }

      return false;
   }

   #region Private Variables

   #endregion
}
