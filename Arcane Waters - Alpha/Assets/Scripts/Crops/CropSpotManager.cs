using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CropSpotManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static CropSpotManager self;

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
      _cropSpots[cropSpot.cropNumber] = cropSpot;
   }

   public CropSpot getCropSpot (int number) {
      return _cropSpots[number];
   }

   #region Private Variables

   // The crop spots we know about
   protected Dictionary<int, CropSpot> _cropSpots = new Dictionary<int, CropSpot>();

   #endregion
}
