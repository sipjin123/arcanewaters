using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Xml;
using System.Linq;

public class CraftingToolManager : XmlDataToolManager {
   #region Public Variables

   // Reference to the tool scene
   public CraftingToolScene craftingToolScreen;

   // Holds the path of the folder
   public const string FOLDER_PATH = "Crafting";

   public class CraftableRequirementXML
   {
      // The id of the sql entry
      public int xmlID;

      // The id of the creator
      public int creatorID;

      //The content of the sql entry
      public CraftableItemRequirements requirements;
   }

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
      _craftingDataList = new Dictionary<int, CraftableRequirementXML>();
      XmlLoadingPanel.self.startLoading();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getCraftingXML();
         userIdData = DB_Main.getSQLDataByID(editorToolType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlData in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(xmlData.rawXmlData);
               CraftableItemRequirements craftingData = Util.xmlLoad<CraftableItemRequirements>(newTextAsset);

               CraftableRequirementXML newRequirementXML = new CraftableRequirementXML {
                  creatorID = xmlData.xmlOwnerId,
                  xmlID = xmlData.xmlId,
                  requirements = craftingData
               };

               // Save the Crafting data in the memory cache
               if (!_craftingDataList.ContainsKey(xmlData.xmlId)) {
                  _craftingDataList.Add(xmlData.xmlId, newRequirementXML);
               } 
            }
            craftingToolScreen.updatePanelWithCraftingIngredients(_craftingDataList.Values.ToList());
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   public void deleteCraftingDataFile (int xmlID) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteCraftingXML(xmlID);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadAllDataFiles();
         });
      });
   }

   public void saveDataToFile (CraftableItemRequirements data, int xmlID) {
      string fileName = data.resultItem.category == Item.Category.None ? "Undefined" : Util.getItemName(data.resultItem.category, data.resultItem.itemTypeId);

      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateCraftingXML(xmlID, longString, fileName);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadAllDataFiles();
         });
      });
   }

   #region Private Variables

   // Cache for craftable items data
   private Dictionary<int, CraftableRequirementXML> _craftingDataList = new Dictionary<int, CraftableRequirementXML>();

   #endregion
}