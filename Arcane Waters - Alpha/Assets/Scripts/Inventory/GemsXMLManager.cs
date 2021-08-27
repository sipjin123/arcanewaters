using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Linq;

public class GemsXMLManager : MonoBehaviour
{
   #region Public Variables

   // A convenient self reference
   public static GemsXMLManager self;

   // References to all the gems data
   public List<GemsData> gemsStatList { get { return _gemsDataList.ToList(); } }

   // Is loaded?
   public bool isLoaded;

   // Determines if data setup is done
   public UnityEvent finishedDataSetup = new UnityEvent();

   #endregion

   private void Awake () {
      self = this;
   }

   public GemsData getGemsData (int id) {
      if (_gemsDataRegistry.ContainsKey(id)) {
         return _gemsDataRegistry[id];
      }

      return null;
   }

   private void finishedLoading () {
      isLoaded = true;
      finishedDataSetup.Invoke();
   }

   public void initializeDataCache () {
      _gemsDataRegistry = new Dictionary<int, GemsData>();
      _gemsDataList = new List<GemsData>();

      List<XMLPair> gemsXML = DB_Main.getGemsXML();

      foreach (XMLPair xmlPair in gemsXML) {
         if (xmlPair.isEnabled) {
            try {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               GemsData rawData = Util.xmlLoad<GemsData>(newTextAsset);
               rawData.itemID = xmlPair.xmlId;

               // Save the data in the memory cache
               if (!_gemsDataRegistry.ContainsKey(xmlPair.xmlId) && xmlPair.isEnabled) {
                  _gemsDataRegistry.Add(xmlPair.xmlId, rawData);
                  _gemsDataList.Add(rawData);
               }
            } catch {
               D.editorLog("Failed to translate data: " + xmlPair.rawXmlData, Color.red);
            }
         }
      }

      finishedLoading();
   }

   public void receiveDataFromZipData (List<GemsData> data) {
      foreach (GemsData rawData in data) {
         int uniqueID = rawData.itemID;

         // Save the data in the memory cache
         if (!_gemsDataRegistry.ContainsKey(uniqueID)) {
            _gemsDataRegistry.Add(uniqueID, rawData);
            _gemsDataList.Add(rawData);
         }
      }

      D.adminLog("EquipmentXML :: Received a total of {" + _gemsDataList.Count + "} gems data from {" + data.Count + "}", D.ADMIN_LOG_TYPE.Equipment);
      finishedLoading();
   }

   public void resetAllData () {
      if (_gemsDataRegistry == null) {
         _gemsDataRegistry = new Dictionary<int, GemsData>();
      }

      _gemsDataRegistry.Clear();
      _gemsDataList.Clear();
   }

   #region Private Variables

   // Display list for editor reviewing
   [SerializeField]
   private List<GemsData> _gemsDataList = new List<GemsData>();

   // Data repository
   private Dictionary<int, GemsData> _gemsDataRegistry = new Dictionary<int, GemsData>();

   #endregion
}
