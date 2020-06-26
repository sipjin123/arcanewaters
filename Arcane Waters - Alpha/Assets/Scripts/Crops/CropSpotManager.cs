using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class CropSpotManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static CropSpotManager self;

   // The crops that are queued to be spawned after the crop spots are loaded
   public List<CropQueueData> cropQueueList = new List<CropQueueData>();

   #endregion

   void Awake () {
      self = this;
   }

   public void resetCropSpots () {
      _cropSpots.Clear();

      foreach (CropSpot cropSpot in FindObjectsOfType<CropSpot>()) {
         storeCropSpot(cropSpot);
      }
   }

   private void Start () {
      // Look up all of our Crop Spots and store them
      foreach (CropSpot cropSpot in FindObjectsOfType<CropSpot>()) {
         storeCropSpot(cropSpot);
      }
   }

   public void storeCropSpot (CropSpot cropSpot) {
      if (cropSpot.areaKey.Length > 1) {
         // Create data if does not exist
         if (!_cropSpots.Exists(_ => _.areaKey == cropSpot.areaKey)) {
            _cropSpots.Add(new CropSpotInfo {
               areaKey = cropSpot.areaKey,
               cropSpots = new List<CropSpot>()
            });
         }

         _cropSpots.Find(_ => _.areaKey == cropSpot.areaKey).cropSpots.Add(cropSpot);
         CropQueueData cachedCropQueueData = cropQueueList.Find(_ => _.areaKey == cropSpot.areaKey && _.cropSpotNumber == cropSpot.cropNumber);
         if (cachedCropQueueData != null) {
            cropQueueList.Remove(cachedCropQueueData);

            cachedCropQueueData.crop.gameObject.SetActive(true);
            cropSpot.crop = cachedCropQueueData.crop;
            cachedCropQueueData.crop.transform.position = cropSpot.transform.position;

            // Show some effects
            if (cachedCropQueueData.showEffects) {
               EffectManager.self.create(Effect.Type.Crop_Shine, cropSpot.transform.position);

               if (cachedCropQueueData.justGrew) {
                  EffectManager.self.create(Effect.Type.Crop_Water, cropSpot.transform.position);

                  // Play a sound
                  SoundManager.create3dSound("crop_water_", cropSpot.transform.position, 5);
               } else {
                  EffectManager.self.create(Effect.Type.Crop_Harvest, cropSpot.transform.position);
                  EffectManager.self.create(Effect.Type.Crop_Dirt_Large, cropSpot.transform.position);

                  // Play a sound
                  SoundManager.create3dSound("crop_plant_", cropSpot.transform.position, 5);
               }
            }
         }
      }
   }

   public CropSpot getCropSpot (int number, string areaKey) {
      if (_cropSpots.Find(_=>_.areaKey == areaKey) != null) {
         return _cropSpots.Find(_ => _.areaKey == areaKey).cropSpots.Find(_ => _.cropNumber == number);
      }
      return null;
   }

   #region Private Variables

   // The crop spots we know about
   [SerializeField]
   protected List<CropSpotInfo> _cropSpots = new List<CropSpotInfo>();

   #endregion
}

[Serializable]
public class CropSpotInfo {
   // The area key
   public string areaKey;

   // The crop spots
   public List<CropSpot> cropSpots;
}

[Serializable]
public class CropQueueData {
   // The area of the crop
   public string areaKey;

   // The spot where the crop should be planted
   public int cropSpotNumber;

   // The crop data
   public Crop crop;

   // If the crops should spawn effects
   public bool showEffects;

   // If the crops just grew
   public bool justGrew;
}