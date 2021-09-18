using MapCreationTool;
using MapCreationTool.Serialization;
using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool{
   public class PvpShopEntityEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable
   {
      #region Public Variables

      // The name of the shop
      public Text shopName;

      // The range display of the shop collision
      public GameObject rangeDisplay;
      
      // The building of the shop
      public GameObject buildingObject;

      #endregion

      private void Start () {
         if (transform.GetComponentInParent<Palette>() != null) {
            rangeDisplay.SetActive(false);
         }
      }

      public void dataFieldChanged (DataField field) {
         if (field.k.CompareTo(DataField.SHOP_ID) == 0) {
            try {
               int shopId = int.Parse(field.v.Split(':')[0]);

               if (ShopManager.instance.pvpShopDataCollection.ContainsKey(shopId)) {
                  shopName.text = ShopManager.instance.pvpShopDataCollection[shopId].shopName;
               }
            } catch {
            }
         } else if (field.k.CompareTo(DataField.HAS_SHOP_BUILDING) == 0) {
            string isStationaryData = field.v.Split(':')[0];
            buildingObject.SetActive(isStationaryData.ToLower() == "true" ? true : false);
         }
      }

      public void setHighlight (bool hovered, bool selected, bool deleting) {
      }

      #region Private Variables

      #endregion
   }
}