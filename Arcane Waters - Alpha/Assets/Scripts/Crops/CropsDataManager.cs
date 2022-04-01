using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CropsDataManager : MonoBehaviour
{
   #region Public Variables

   // Self
   public static CropsDataManager self;

   // The crop data list
   public List<CropsData> cropDataList;

   // Have all the crops been loaded
   public bool cropsLoaded = false;

   #endregion

   private void Awake () {
      self = this;
   }

   public CropsData getCropData (Crop.Type cropType) {
      if (_cropDataCollection.ContainsKey(cropType)) {
         return _cropDataCollection[cropType];
      }
      return new CropsData();
   }

   public bool tryGetCropData (Crop.Type cropType, out CropsData data) => _cropDataCollection.TryGetValue(cropType, out data);
   public bool tryGetCropData (int cropDataId, out CropsData data) => _cropsDataById.TryGetValue(cropDataId, out data);

   public void initializeDataCache () {
      _cropDataCollection = new Dictionary<Crop.Type, CropsData>();
      cropDataList = new List<CropsData>();
      _cropsDataById = new Dictionary<int, CropsData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getCropsXML();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlData in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(xmlData.rawXmlData);
               CropsData cropsData = Util.xmlLoad<CropsData>(newTextAsset);
               Crop.Type cropType = (Crop.Type) cropsData.cropsType;

               // Save the Crop data in the memory cache
               if (!_cropDataCollection.ContainsKey(cropType)) {
                  _cropDataCollection.Add(cropType, cropsData);
                  cropDataList.Add(cropsData);
                  _cropsDataById.Add(cropsData.xmlId, cropsData);
               } else {
                  D.debug("Key already exists: " + cropType);
               }

               cropsLoaded = true;
            }
         });
      });
   }

   public void receiveCropsFromZipData (List<CropsData> newCropDataList) {
      foreach (CropsData cropData in newCropDataList) {
         Crop.Type cropType = (Crop.Type) cropData.cropsType;
         // Save the Crop data in the memory cache
         if (!_cropDataCollection.ContainsKey(cropType)) {
            _cropDataCollection.Add(cropType, cropData);
            cropDataList.Add(cropData);
            _cropsDataById.Add(cropData.xmlId, cropData);
         }
      }

      cropsLoaded = true;
   }

   #region Private Variables

   // The data collection of crops data
   protected Dictionary<Crop.Type, CropsData> _cropDataCollection = new Dictionary<Crop.Type, CropsData>();

   // The data collection of crops by their id
   protected Dictionary<int, CropsData> _cropsDataById = new Dictionary<int, CropsData>();

   #endregion
}
