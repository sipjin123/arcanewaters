using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShopDataItemTemplate : MonoBehaviour {
   #region Public Variables

   // Image of the item
   public Image itemImage;

   // Cost of the Item
   public InputField itemCostMax;

   // Item Name
   public Text itemName;

   // Item typeID
   public Text itemIDType;

   // Item typeCategory
   public Text itemIDCategory;

   // Select Item
   public Button itemSelection;

   // Delete template
   public Button deleteItem;

   // Data of the item
   public Item itemData;

   // Icon Images
   public GameObject weaponIcon, armorIcon, shipIcon, cropIcon;

   // Current shop category of the template
   public ShopToolPanel.ShopCategory currentCategory;

   // The path of the image icon
   public Text iconPath;

   // Drop rate
   public InputField chanceToDrop;

   // Quantity Min Max
   public InputField quantityMin, quantityMax;

   #endregion

   public void setIcon (ShopToolPanel.ShopCategory category) {
      currentCategory = category;

      weaponIcon.SetActive(false);
      armorIcon.SetActive(false);
      shipIcon.SetActive(false);
      cropIcon.SetActive(false);

      switch (category) {
         case ShopToolPanel.ShopCategory.Weapon:
            weaponIcon.SetActive(true);
            break;
         case ShopToolPanel.ShopCategory.Armor:
            armorIcon.SetActive(true);
            break;
         case ShopToolPanel.ShopCategory.Ship:
            shipIcon.SetActive(true);
            break;
         case ShopToolPanel.ShopCategory.Crop:
            cropIcon.SetActive(true);
            break;
      }

      if (!MasterToolAccountManager.canAlterData()) {
         deleteItem.gameObject.SetActive(false);
      }
   }
}
