using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

public class ShipAbilityToolManager : XmlDataToolManager {
   #region Public Variables

   // Holds the main scene 
   public ShipAbilityScene shipAbilityToolScene;

   // Self
   public static ShipAbilityToolManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      Invoke("loadXMLData", MasterToolScene.loadDelay);
   }

   public void saveXMLData (ShipAbilityData data) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateShipAbilityXML(longString, data.abilityName);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void deleteDataFile (ShipAbilityData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteShipAbilityXML(data.abilityName);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void duplicateXMLData (ShipAbilityData data) {
      data.abilityName = "Undefined Faction";
      data.projectileSpritePath = "";
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateShipAbilityXML(longString, data.abilityName);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void loadXMLData () {
      _shipAbilityData = new Dictionary<string, ShipAbilityData>();
      XmlLoadingPanel.self.startLoading();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getShipAbilityXML();
         userNameData = DB_Main.getSQLDataByName(editorToolType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               ShipAbilityData shipAbilityData = Util.xmlLoad<ShipAbilityData>(newTextAsset);

               // Save the data in the memory cache
               if (!_shipAbilityData.ContainsKey(shipAbilityData.abilityName)) {
                  _shipAbilityData.Add(shipAbilityData.abilityName, shipAbilityData);
               }
            }

            shipAbilityToolScene.loadData(_shipAbilityData);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   #region Private Variables

   // Holds the list of ship ability data
   private Dictionary<string, ShipAbilityData> _shipAbilityData = new Dictionary<string, ShipAbilityData>();

   #endregion
}
