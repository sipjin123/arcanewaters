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

   public bool didUserCreateData (int xml_id) {
      SQLEntryIDClass sqlEntry = userIdData.Find(_ => _.xmlID == xml_id);
      if (sqlEntry != null) {
         if (sqlEntry.ownerID == MasterToolAccountManager.self.currentAccountID) {
            return true;
         }
      }
      return false;
   }

   protected void fetchRecipe () {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getCraftingXML();
         userIdData = DB_Main.getSQLDataByID(editorToolType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlData in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(xmlData.rawXmlData);
               CraftableItemRequirements craftingData = Util.xmlLoad<CraftableItemRequirements>(newTextAsset);

               // Save the Crafting data in the memory cache
               craftingDataList.Add(craftingData);
            }
         });
      });
   }

   public CraftableItemRequirements getCraftableItem (int itemID, Item.Category category) {
      CraftableItemRequirements fetchedItemRequirement = craftingDataList.Find(_ => _.resultItem.itemTypeId == itemID && _.resultItem.category == category);
      if (fetchedItemRequirement == null) {
         D.debug("Item does not exist: " + itemID + " - " + category);
         return null;
      }

      return fetchedItemRequirement;
   }

   #region Private Variables

   #endregion
}
