using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Linq;

public class ShipSkinXMLManager : MonoBehaviour
{
   #region Public Variables

   // A convenient self reference
   public static ShipSkinXMLManager self;

   // References to all the ship skins data
   public List<ShipSkinData> shipSkinStatList { get { return _shipSkinDataList.ToList(); } }

   // Is loaded?
   public bool isLoaded;

   // Determines if data setup is done
   public UnityEvent finishedDataSetup = new UnityEvent();

   #endregion

   private void Awake () {
      self = this;
   }

   public ShipSkinData getShipSkinDataByType (Ship.SkinType skinType) {
      return _shipSkinDataList.Find(_ => _.skinType == skinType);
   }

   public ShipSkinData getShipSkinData (int shipSkinId) {
      if (_shipSkinDataRegistry.ContainsKey(shipSkinId)) {
         return _shipSkinDataRegistry[shipSkinId];
      }

      return null;
   }

   private void finishedLoading () {
      isLoaded = true;
      finishedDataSetup.Invoke();
   }

   public void initializeDataCache () {
      _shipSkinDataRegistry = new Dictionary<int, ShipSkinData>();
      _shipSkinDataList = new List<ShipSkinData>();

      List<XMLPair> shipSkinsXML = DB_Main.getShipSkinsXML();

      foreach (XMLPair xmlPair in shipSkinsXML) {
         if (xmlPair.isEnabled) {
            try {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               ShipSkinData rawData = Util.xmlLoad<ShipSkinData>(newTextAsset);
               rawData.itemID = xmlPair.xmlId;

               // Save the data in the memory cache
               if (!_shipSkinDataRegistry.ContainsKey(xmlPair.xmlId) && xmlPair.isEnabled) {
                  _shipSkinDataRegistry.Add(xmlPair.xmlId, rawData);
                  _shipSkinDataList.Add(rawData);
               }
            } catch {
               D.editorLog("Failed to translate data: " + xmlPair.rawXmlData, Color.red);
            }
         }
      }

      finishedLoading();
   }

   public void receiveShipSkinDataFromZipData (List<ShipSkinData> data) {
      foreach (ShipSkinData rawData in data) {
         int uniqueID = rawData.itemID;

         // Save the data in the memory cache
         if (!_shipSkinDataRegistry.ContainsKey(uniqueID)) {
            _shipSkinDataRegistry.Add(uniqueID, rawData);
            _shipSkinDataList.Add(rawData);
         }
      }

      D.adminLog("EquipmentXML :: Received a total of {" + _shipSkinDataList.Count + "} haircut data from {" + data.Count + "}", D.ADMIN_LOG_TYPE.Equipment);
      finishedLoading();
   }

   public void resetAllData () {
      if (_shipSkinDataRegistry == null) {
         _shipSkinDataRegistry = new Dictionary<int, ShipSkinData>();
      }

      _shipSkinDataRegistry.Clear();
      _shipSkinDataList.Clear();
   }

   #region Private Variables

   // Display list for editor reviewing
   [SerializeField]
   private List<ShipSkinData> _shipSkinDataList = new List<ShipSkinData>();

   // Stores the list of all haircut data
   private Dictionary<int, ShipSkinData> _shipSkinDataRegistry = new Dictionary<int, ShipSkinData>();

   #endregion
}
