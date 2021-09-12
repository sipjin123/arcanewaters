using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Linq;

public class HairDyeXMLManager : MonoBehaviour
{
   #region Public Variables

   // A convenient self reference
   public static HairDyeXMLManager self;

   // References to all the haircut data
   public List<HairDyeData> hairdyeStatList { get { return _hairdyeStatList.ToList(); } }

   // Is loaded?
   public bool isLoaded;

   // Determines if data setup is done
   public UnityEvent finishedDataSetup = new UnityEvent();

   #endregion

   private void Awake () {
      self = this;
   }

   public HairDyeData getHairdyeData (int haircutID) {
      if (_hairdyeDataRegistry.ContainsKey(haircutID)) {
         return _hairdyeDataRegistry[haircutID];
      }

      return null;
   }

   private void finishedLoading () {
      isLoaded = true;
      finishedDataSetup.Invoke();
   }

   public void initializeDataCache () {
      _hairdyeDataRegistry = new Dictionary<int, HairDyeData>();
      _hairdyeStatList = new List<HairDyeData>();

      List<XMLPair> hairdyesXML = DB_Main.getHairdyesXML();

      foreach (XMLPair xmlPair in hairdyesXML) {
         if (xmlPair.isEnabled) {
            try {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               HairDyeData rawData = Util.xmlLoad<HairDyeData>(newTextAsset);
               rawData.itemID = xmlPair.xmlId;

               // Save the data in the memory cache
               if (!_hairdyeDataRegistry.ContainsKey(xmlPair.xmlId) && xmlPair.isEnabled) {
                  _hairdyeDataRegistry.Add(xmlPair.xmlId, rawData);
                  _hairdyeStatList.Add(rawData);
               }
            } catch {
               D.editorLog("Failed to translate data: " + xmlPair.rawXmlData, Color.red);
            }
         }
      }

      finishedLoading();
   }

   public void receiveDataFromZipData (List<HairDyeData> data) {
      foreach (HairDyeData rawData in data) {
         int uniqueID = rawData.itemID;

         // Save the data in the memory cache
         if (!_hairdyeDataRegistry.ContainsKey(uniqueID)) {
            _hairdyeDataRegistry.Add(uniqueID, rawData);
            _hairdyeStatList.Add(rawData);
         }
      }

      finishedLoading();
   }

   public void resetAllData () {
      if (_hairdyeDataRegistry == null) {
         _hairdyeDataRegistry = new Dictionary<int, HairDyeData>();
      }

      _hairdyeDataRegistry.Clear();
      _hairdyeStatList.Clear();
   }

   #region Private Variables

   // Display list for editor reviewing
   [SerializeField]
   private List<HairDyeData> _hairdyeStatList = new List<HairDyeData>();

   // Data registry
   private Dictionary<int, HairDyeData> _hairdyeDataRegistry = new Dictionary<int, HairDyeData>();

   #endregion
}