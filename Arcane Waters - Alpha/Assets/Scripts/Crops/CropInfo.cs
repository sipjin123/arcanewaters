using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public struct CropInfo {
   #region Public Variables

   // The type of Crop
   public Crop.Type cropType;

   // The user who owns the crop
   public int userId;

   // The spot number for this crop
   public int cropNumber;

   // The time the crop was planted
   public long creationTime;

   // The unix timestamp for when the crop was last watered
   public long lastWaterTimestamp;

   // The amount of time we have to wait between watering
   public float waterInterval;

   // The current growth level of this crop
   public int growthLevel;

   // The area key where this crop is spawned
   public string areaKey;

   #endregion

   #if IS_SERVER_BUILD

   public CropInfo (MySqlDataReader dataReader) {
      this.cropType = (Crop.Type) DataUtil.getInt(dataReader, "crpType");
      this.userId = DataUtil.getInt(dataReader, "usrId");
      this.cropNumber = DataUtil.getInt(dataReader, "cropNumber");
      this.creationTime = DataUtil.getDateTime(dataReader, "creationTime").ToBinary();
      this.lastWaterTimestamp = DataUtil.getLong(dataReader, "lastWaterTimestamp");
      this.waterInterval = DataUtil.getInt(dataReader, "waterInterval");
      this.growthLevel = DataUtil.getInt(dataReader, "growthLevel");
      try {
         this.areaKey = DataUtil.getString(dataReader, "areaKey");
      } catch {
         this.areaKey = "unknown";
      }
   }

   #endif

   public CropInfo (Crop.Type cropType, int userId, int cropNumber, long creationTime, long lastWaterTimestamp, int waterInterval, int growthLevel = 0, string areaKey = "") {
      this.cropType = cropType;
      this.userId = userId;
      this.cropNumber = cropNumber;
      this.creationTime = creationTime;
      this.lastWaterTimestamp = lastWaterTimestamp;
      this.waterInterval = waterInterval;
      this.growthLevel = growthLevel;
      this.areaKey = areaKey;
   }

   public override bool Equals(object obj) {
      if (!(obj is CropInfo))
         return false;

      CropInfo other = (CropInfo)obj;
      return cropType == other.cropType && userId == other.userId && cropNumber == other.cropNumber && creationTime == other.creationTime;
   }

   public override int GetHashCode () {
      unchecked // Overflow is fine, just wrap
      {
         int hash = 17;
         hash = hash * 23 + cropType.GetHashCode();
         hash = hash * 23 + userId.GetHashCode();
         hash = hash * 23 + cropNumber.GetHashCode();
         hash = hash * 23 + creationTime.GetHashCode();
         return hash;
      }
   }

   public bool isMaxLevel () {
      return this.growthLevel >= Crop.getMaxGrowthLevel(this.cropType);
   }

   public bool isReadyForWater () {
      if (isMaxLevel()) {
         return false;
      }

      long serverTimestamp = ((DateTimeOffset) TimeManager.self.getLastServerDateTime()).ToUnixTimeSeconds();
      return (serverTimestamp - lastWaterTimestamp) > this.waterInterval;
   }

   #region Private Variables

   #endregion
}
