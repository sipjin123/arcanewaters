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

   // Crafting Data to be rewarded
   public List<CraftableItemRequirements> craftingDataList = new List<CraftableItemRequirements>();

   // Self
   public static XmlDataToolManager self;

   #endregion

   public bool didUserCreateData (string entryName) {
      SQLEntryNameClass sqlEntry = userNameData.Find(_ => _.dataName == entryName);
      if (sqlEntry != null) {
         if (sqlEntry.ownerID == MasterToolAccountManager.self.currentAccountID) {
            return true;
         }
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

   protected void fetchRecipe () {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getCraftingXML();
         userNameData = DB_Main.getSQLDataByName(EditorSQLManager.EditorToolType.Crafting);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               CraftableItemRequirements craftingData = Util.xmlLoad<CraftableItemRequirements>(newTextAsset);

               // Save the Crafting data in the memory cache
               craftingDataList.Add(craftingData);
            }
         });
      });
   }

   #region Private Variables

   #endregion
}
