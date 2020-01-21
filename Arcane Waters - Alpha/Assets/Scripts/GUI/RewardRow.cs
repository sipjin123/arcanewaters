using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

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
      rewardName.text = castedItem.getName();
   }

   #region Private Variables

   #endregion
}