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

public class ShipDataToolManager : MonoBehaviour {
   #region Public Variables

   // Holds the main scene for the ship data
   public ShipDataScene shipDataScene;

   // Holds the path of the folder
   public const string FOLDER_PATH = "ShipStats";

   // List of abilities
   public List<string> shipSkillList = new List<string>();

   #endregion

   private void Start () {
      Invoke("loadXMLData", 2f);
   }

   public void saveXMLData (ShipData data) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      Ship.Type uniqueID = data.shipType;
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateShipXML(longString, (int) uniqueID);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void deleteDataFile (ShipData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteShipXML((int) data.shipType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void duplicateXMLData (ShipData data) {
      int uniqueID = 0;
      foreach (Ship.Type shipType in Enum.GetValues(typeof(Ship.Type))) {
         if (!_shipDataList.Values.ToList().Exists(_ => _.shipType == shipType)) {
            uniqueID = (int)shipType;
            break;
         }
      }
      if (uniqueID == 0) {
         Debug.LogWarning("All ID is accounted for, please edit data instead");
         return;
      }

      data.shipName += "_copy";
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateShipXML(longString, uniqueID);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void loadXMLData () {
      XmlLoadingPanel.self.startLoading();
      _shipDataList = new Dictionary<string, ShipData>();
      shipSkillList = new List<string>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getShipXML();
         List<string> rawShipAbilityXMLData = DB_Main.getShipAbilityXML();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string shipAbilityText in rawShipAbilityXMLData) {
               TextAsset newTextAsset = new TextAsset(shipAbilityText);
               ShipAbilityData shipAbility = Util.xmlLoad<ShipAbilityData>(newTextAsset);
               shipSkillList.Add(shipAbility.abilityName);
            }

            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               ShipData shipData = Util.xmlLoad<ShipData>(newTextAsset);

               // Save the Ship data in the memory cache
               if (!_shipDataList.ContainsKey(shipData.shipName)) {
                  _shipDataList.Add(shipData.shipName, shipData);
               }
            }

            shipDataScene.loadShipData(_shipDataList);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   #region Private Variables

   // Holds the list of ship data
   private Dictionary<string, ShipData> _shipDataList = new Dictionary<string, ShipData>();

   #endregion
}
