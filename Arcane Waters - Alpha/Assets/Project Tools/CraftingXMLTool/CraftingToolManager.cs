using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

public class CraftingToolManager : XmlDataToolManager {
   #region Public Variables

   // Reference to the tool scene
   public CraftingToolScene craftingToolScreen;

   // Holds the path of the folder
   public const string FOLDER_PATH = "Crafting";

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      // Initialize equipment data first
      Invoke("initializeEquipmentData", MasterToolScene.loadDelay);
      XmlLoadingPanel.self.startLoading();
   }

   private void initializeEquipmentData () {
      // Initialize all craftable item data after equipment data is setup
      EquipmentXMLManager.self.finishedDataSetup.AddListener(() => {
         loadAllDataFiles();
      });

      EquipmentXMLManager.self.initializeDataCache();
   }

   public void loadAllDataFiles () {
      _craftingDataList = new Dictionary<string, CraftableItemRequirements>();
      XmlLoadingPanel.self.startLoading();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getCraftingXML();
         userNameData = DB_Main.getSQLDataByName(editorToolType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               CraftableItemRequirements craftingData = Util.xmlLoad<CraftableItemRequirements>(newTextAsset);
               string craftingID = craftingData.resultItem.category == Item.Category.None ? "Undefined" : Util.getItemName(craftingData.resultItem.category, craftingData.resultItem.itemTypeId);

               // Save the Crafting data in the memory cache
               if (!_craftingDataList.ContainsKey(craftingID)) {
                  _craftingDataList.Add(craftingID, craftingData);
               } else {
                  craftingID += "_copy";
                  _craftingDataList.Add(craftingID, craftingData);
               }
            }
            craftingToolScreen.updatePanelWithCraftingIngredients(_craftingDataList);
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
      return _craftingDataList.ContainsKey(nameID);
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

   // Cache for craftable items data
   private Dictionary<string, CraftableItemRequirements> _craftingDataList = new Dictionary<string, CraftableItemRequirements>();

   #endregion
}
