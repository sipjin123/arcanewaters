using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class RewardRow : MonoBehaviour {
   #region Public Variables

   // The container for the item cell
   public GameObject itemCellContainer;

   // The prefab we use to create item cells - used as a static icon
   public ItemCell itemCellPrefab;

   // Name of the rewarded item
   public Text rewardName;

   #endregion

   public void setRowForItem(Item castedItem) {
      // Clear any existing item cell
      itemCellContainer.DestroyChildren();

      // Instantiates an item cell
      ItemCell cell = Instantiate(itemCellPrefab, itemCellContainer.transform, false);

      // Initializes the cell
      cell.setCellForItem(castedItem);

      // Disable the click event on the cell
      cell.disablePointerEvents();

      // Disable the cell background
      cell.hideBackground();

      // Set the item name
      rewardName.text = EquipmentXMLManager.self.getItemName(castedItem);
   }

   public void activateRowForItem (ItemInstance item) {
      // Clear any existing item cell
      itemCellContainer.DestroyChildren();

      // Instantiates an item cell
      ItemCell cell = Instantiate(itemCellPrefab, itemCellContainer.transform, false);

      // Initializes the cell
      cell.setCellForItem(item);

      // Disable the click event on the cell
      cell.disablePointerEvents();

      // Disable the cell background
      cell.hideBackground();

      // Set the item name
      rewardName.text = item.getName();

      gameObject.SetActive(true);
   }

   public void setDisplayRow (string name, string iconPath) {
      itemCellContainer.DestroyChildren();
      rewardName.text = name;

      ItemCell cell = Instantiate(itemCellPrefab, itemCellContainer.transform, false);
      Sprite newSprite = ImageManager.getSprite(iconPath);
      cell.icon.sprite = newSprite;
      cell.itemCountText.gameObject.SetActive(false);
      cell.iconShadow.sprite = newSprite; 
      cell.tooltip.message = name;
   }

   #region Private Variables

   #endregion
}