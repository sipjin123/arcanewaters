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

   public class ShipAbilityGroup {
      // The database id
      public int xmlId;

      // The ability data
      public ShipAbilityData shipAbility;

      // The owner of the content
      public int ownerId;
   }

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      Invoke("loadXMLData", MasterToolScene.loadDelay);
   }

   public void saveXMLData (int xmlId, ShipAbilityData data) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateShipAbilityXML(longString, data.abilityName, xmlId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void deleteDataFile (int xmlId) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteShipAbilityXML(xmlId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void duplicateXMLData (ShipAbilityData data) {
      data.abilityName = MasterToolScene.UNDEFINED;
      data.projectileSpritePath = "";
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateShipAbilityXML(longString, data.abilityName, -1);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void loadXMLData () {
      _shipAbilityData = new Dictionary<int, ShipAbilityGroup>();
      XmlLoadingPanel.self.startLoading();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> shipAbilityXml = DB_Main.getShipAbilityXML();
         userNameData = DB_Main.getSQLDataByName(editorToolType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in shipAbilityXml) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               ShipAbilityData shipAbilityData = Util.xmlLoad<ShipAbilityData>(newTextAsset);

               // Save the data in the memory cache
               if (!_shipAbilityData.ContainsKey(xmlPair.xmlId)) {
                  ShipAbilityGroup newGroup = new ShipAbilityGroup {
                     xmlId = xmlPair.xmlId,
                     ownerId = xmlPair.xmlOwnerId,
                     shipAbility = shipAbilityData
                  };

                  _shipAbilityData.Add(xmlPair.xmlId, newGroup);
               }
            }

            shipAbilityToolScene.loadData(_shipAbilityData);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   #region Private Variables

   // Holds the list of ship ability data
   private Dictionary<int, ShipAbilityGroup> _shipAbilityData = new Dictionary<int, ShipAbilityGroup>();

   #endregion
}
