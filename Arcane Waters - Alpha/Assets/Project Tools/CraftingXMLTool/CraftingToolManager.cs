using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

public class CraftingToolManager : MonoBehaviour {
   #region Public Variables

   // Reference to the tool scene
   public CraftingToolScene craftingToolScreen;

   // Holds the path of the folder
   public const string FOLDER_PATH = "Crafting";

   // Self
   public static CraftingToolManager self;

   // Holds the collection of user id that created the data entry
   public List<SQLEntryNameClass> _userIdData = new List<SQLEntryNameClass>();

   #endregion

   private void Awake () {
      self = this;
   }

   public bool didUserCreateData (string entryName) {
      SQLEntryNameClass sqlEntry = _userIdData.Find(_ => _.dataName == entryName);
      if (sqlEntry != null) {
         if (sqlEntry.ownerID == MasterToolAccountManager.self.currentAccountID) {
            return true;
         }
      } else {
         Debug.LogWarning("Entry does not exist: " + entryName);
      }

      return false;
   }

   private void Start () {
      Invoke("loadAllDataFiles", MasterToolScene.loadDelay);
   }

   public void loadAllDataFiles () {
      craftingDataList = new Dictionary<string, CraftableItemRequirements>();
      XmlLoadingPanel.self.startLoading();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getCraftingXML();
         _userIdData = DB_Main.getSQLDataByName(EditorSQLManager.EditorToolType.Crafting);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               CraftableItemRequirements craftingData = Util.xmlLoad<CraftableItemRequirements>(newTextAsset);
               string craftingID = craftingData.resultItem.category == Item.Category.None ? "Undefined" : Util.getItemName(craftingData.resultItem.category, craftingData.resultItem.itemTypeId);

               // Save the Crafting data in the memory cache
               if (!craftingDataList.ContainsKey(craftingID)) {
                  craftingDataList.Add(craftingID, craftingData);
               } else {
                  craftingID += "_copy";
                  craftingDataList.Add(craftingID, craftingData);
               }
            }
            craftingToolScreen.updatePanelWithCraftingIngredients(craftingDataList);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   public void deleteCraftingDataFile (CraftableItemRequirements data) {
      string craftingID = data.resultItem.category == Item.Category.None ? "Undefined" : Util.getItemName(data.resultItem.category, data.resultItem.itemTypeId);

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteCraftingXML(craftingID);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadAllDataFiles();
         });
      });
   }

   public bool ifExists (string nameID) {
      return craftingDataList.ContainsKey(nameID);
   }

   public void saveDataToFile (CraftableItemRequirements data, bool deleteBlankData) {
      string fileName = data.resultItem.category == Item.Category.None ? "Undefined" : Util.getItemName(data.resultItem.category, data.resultItem.itemTypeId);

      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateCraftingXML(longString, fileName);

         if (deleteBlankData) {
            deleteCraftingDataFile(new CraftableItemRequirements { resultItem = new Item { category = Item.Category.None } });
         }

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadAllDataFiles();
         });
      });
   }

   #region Private Variables

   private Dictionary<string, CraftableItemRequirements> craftingDataList = new Dictionary<string, CraftableItemRequirements>();

   #endregion
}
