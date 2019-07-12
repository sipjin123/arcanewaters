using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public struct SiloInfo
{
   #region Public Variables

   // The type of Crop
   public Crop.Type cropType;

   // The user who owns the crop
   public int userId;

   // The amount of this crop in the silo
   public int cropCount;

   #endregion

#if IS_SERVER_BUILD

   public SiloInfo(MySqlDataReader dataReader)
   {
      this.cropType = (Crop.Type)DataUtil.getInt(dataReader, "crpType"); ;
      this.userId = DataUtil.getInt(dataReader, "usrId"); ;
      this.cropCount = DataUtil.getInt(dataReader, "cropCount"); ;
   }

#endif

   public SiloInfo(Crop.Type cropType, int userId, int cropCount)
   {
      this.cropType = cropType;
      this.userId = userId;
      this.cropCount = cropCount;
   }

   public override bool Equals(object obj) {
      if (!(obj is SiloInfo))
         return false;

      SiloInfo other = (SiloInfo)obj;
      return cropType == other.cropType && userId == other.userId && cropCount == other.cropCount;
   }

   public override int GetHashCode()
   {
      unchecked // Overflow is fine, just wrap
      {
         int hash = 17;
         hash = hash * 23 + cropType.GetHashCode();
         hash = hash * 23 + userId.GetHashCode();
         hash = hash * 23 + cropCount.GetHashCode();
         return hash;
      }
   }

   public override string ToString() {
      return "SiloInfo: " + cropType + " , user: " + userId + ", count: " + cropCount;
   }

   #region Private Variables

   #endregion
}
