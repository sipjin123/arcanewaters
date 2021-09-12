using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Linq;

public class ConsumableXMLManager : MonoBehaviour
{
   #region Public Variables

   // A convenient self reference
   public static ConsumableXMLManager self;

   // References to all the consumable data
   public List<ConsumableData> consumableDataList { get { return _consumableDataList.ToList(); } }

   // Is loaded?
   public bool isLoaded;

   // Determines if data setup is done
   public UnityEvent finishedDataSetup = new UnityEvent();

   #endregion

   private void Awake () {
      self = this;
   }

   public ConsumableData getConsumableData (int consumableId) {
      if (_consumableDataRegistry.ContainsKey(consumableId)) {
         return _consumableDataRegistry[consumableId];
      }

      return null;
   }

   private void finishedLoading () {
      isLoaded = true;
      finishedDataSetup.Invoke();
   }

   public void initializeDataCache () {
      _consumableDataRegistry = new Dictionary<int, ConsumableData>();
      _consumableDataList = new List<ConsumableData>();

      List<XMLPair> consumablesXML = DB_Main.getConsumableXML();

      foreach (XMLPair xmlPair in consumablesXML) {
         if (xmlPair.isEnabled) {
            try {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               ConsumableData rawData = Util.xmlLoad<ConsumableData>(newTextAsset);
               rawData.itemID = xmlPair.xmlId;

               // Save the data in the memory cache
               if (!_consumableDataRegistry.ContainsKey(xmlPair.xmlId) && xmlPair.isEnabled) {
                  _consumableDataRegistry.Add(xmlPair.xmlId, rawData);
                  _consumableDataList.Add(rawData);
               }
            } catch {
               D.editorLog("Failed to translate data: " + xmlPair.rawXmlData, Color.red);
            }
         }
      }

      finishedLoading();
   }

   public void receiveConsumableDataFromZipData (List<ConsumableData> data) {
      foreach (ConsumableData rawData in data) {
         int uniqueID = rawData.itemID;

         // Save the data in the memory cache
         if (!_consumableDataRegistry.ContainsKey(uniqueID)) {
            _consumableDataRegistry.Add(uniqueID, rawData);
            _consumableDataList.Add(rawData);
         }
      }

      finishedLoading();
   }

   public void resetAllData () {
      if (_consumableDataRegistry == null) {
         _consumableDataRegistry = new Dictionary<int, ConsumableData>();
      }

      _consumableDataRegistry.Clear();
      _consumableDataList.Clear();
   }

   #region Private Variables

   // Display list for editor reviewing
   [SerializeField]
   private List<ConsumableData> _consumableDataList = new List<ConsumableData>();

   // Stores the list of all consumables data
   private Dictionary<int, ConsumableData> _consumableDataRegistry = new Dictionary<int, ConsumableData>();

   #endregion
}
