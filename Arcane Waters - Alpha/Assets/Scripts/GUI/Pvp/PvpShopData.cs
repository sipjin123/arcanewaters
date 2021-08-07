using System;
using System.Collections.Generic;

[Serializable]
public class PvpShopData {
   #region Public Variables

   // Id referencing database entry
   public int shopId;

   // Basic shop info
   public string shopName;
   public string shopDescription;

   // The shop items in this specific shop data
   public List<PvpShopItem> shopItems;

   #endregion
}