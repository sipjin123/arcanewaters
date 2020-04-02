using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Xml;
using System;
using System.Linq;

public class ShipDataToolManager : XmlDataToolManager {
   #region Public Variables

   // Holds the main scene for the ship data
   public ShipDataScene shipDataScene;

   // Holds the path of the folder
   public const string FOLDER_PATH = "ShipStats";

   // List of abilities
   public List<ShipAbilityPair> shipSkillList = new List<ShipAbilityPair>();

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      Invoke("loadXMLData", MasterToolScene.loadDelay);
   }

   public void saveXMLData (ShipData data, int xml_id, bool isActive) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      Ship.Type uniqueID = data.shipType;
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateShipXML(longString, xml_id, data.shipType, data.shipName, isActive);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void deleteDataFile (int  xml_id) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteShipXML(xml_id);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void duplicateXMLData (ShipData data) {
      data.shipName = MasterToolScene.UNDEFINED;
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateShipXML(longString, -1 , data.shipType, data.shipName, false);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void loadXMLData () {
      XmlLoadingPanel.self.startLoading();
      _shipDataList = new List<ShipXMLContent>();
      shipSkillList = new List<ShipAbilityPair>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getShipXML();
         List<XMLPair> rawShipAbilityXMLData = DB_Main.getShipAbilityXML();
         userIdData = DB_Main.getSQLDataByID(editorToolType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair shipAbilityText in rawShipAbilityXMLData) {
               TextAsset newTextAsset = new TextAsset(shipAbilityText.rawXmlData);
               ShipAbilityData shipAbility = Util.xmlLoad<ShipAbilityData>(newTextAsset);
               shipSkillList.Add(new ShipAbilityPair { 
                  abilityName = shipAbility.abilityName,
                  abilityId = shipAbilityText.xmlId
               });
            }

            foreach (XMLPair xmlPair in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               ShipData shipData = Util.xmlLoad<ShipData>(newTextAsset);

               // Save the Ship data in the memory cache
               if (!_shipDataList.Exists(_=>_.xmlId == xmlPair.xmlId)) {
                  ShipXMLContent shipContent = new ShipXMLContent { 
                     xmlId = xmlPair.xmlId,
                     isEnabled = xmlPair.isEnabled,
                     shipData = shipData
                  };
                  _shipDataList.Add(shipContent);
               }
            }

            shipDataScene.loadShipData(_shipDataList);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   #region Private Variables

   // Cached ship list
   [SerializeField] private List<ShipXMLContent> _shipDataList = new List<ShipXMLContent>();

   #endregion
}
