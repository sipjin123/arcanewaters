using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System.IO;
using UnityEngine.Events;

public class HaircutXMLManager : MonoBehaviour {
   #region Public Variables

   // A convenient self reference
   public static HaircutXMLManager self;

   // References to all the haircut data
   public List<HaircutData> haircutStatList { get { return _haircutDataList.ToList(); } }

   // Is loaded?
   public bool isLoaded;

   // Determines if data setup is done
   public UnityEvent finishedDataSetup = new UnityEvent();

   #endregion

   private void Awake () {
      self = this;
   }

   public HaircutData getHaircutData (int haircutID) {
      if (_haircutDataRegistry.ContainsKey(haircutID)) {
         return _haircutDataRegistry[haircutID];
      }

      return null;
   }

   private void finishedLoading () {
      isLoaded = true;
      finishedDataSetup.Invoke();
   }

   public void initializeDataCache () {
      _haircutDataRegistry = new Dictionary<int, HaircutData>();
      _haircutDataList = new List<HaircutData>();

      List<XMLPair> haircutsXML = DB_Main.getHaircutsXML();

      foreach (XMLPair xmlPair in haircutsXML) {
         if (xmlPair.isEnabled) {
            try {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               HaircutData rawData = Util.xmlLoad<HaircutData>(newTextAsset);
               rawData.itemID = xmlPair.xmlId;

               // Save the data in the memory cache
               if (!_haircutDataRegistry.ContainsKey(xmlPair.xmlId) && xmlPair.isEnabled) {
                  _haircutDataRegistry.Add(xmlPair.xmlId, rawData);
                  _haircutDataList.Add(rawData);
               }
            } catch {
               D.editorLog("Failed to translate data: " + xmlPair.rawXmlData, Color.red);
            }
         }
      }
    
      finishedLoading();
   }

   public void receiveHaircutDataFromZipData (List<HaircutData> data) {
      foreach (HaircutData rawData in data) {
         int uniqueID = rawData.itemID;

         // Save the data in the memory cache
         if (!_haircutDataRegistry.ContainsKey(uniqueID)) {
            _haircutDataRegistry.Add(uniqueID, rawData);
            _haircutDataList.Add(rawData);
         }
      }

      D.adminLog("EquipmentXML :: Received a total of {" + _haircutDataList.Count + "} haircut data from {" + data.Count + "}", D.ADMIN_LOG_TYPE.Equipment);
      finishedLoading();
   }

   public void resetAllData () {
      if (_haircutDataRegistry == null) {
         _haircutDataRegistry = new Dictionary<int, HaircutData>();
      }

      _haircutDataRegistry.Clear();
      _haircutDataList.Clear();
   }

   #region Private Variables

   // Display list for editor reviewing
   [SerializeField]
   private List<HaircutData> _haircutDataList = new List<HaircutData>();

   // Stores the list of all haircut data
   private Dictionary<int, HaircutData> _haircutDataRegistry = new Dictionary<int, HaircutData>();

   #endregion
}
