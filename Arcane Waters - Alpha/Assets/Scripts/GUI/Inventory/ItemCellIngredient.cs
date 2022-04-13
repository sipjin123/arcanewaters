using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

public class ItemCellIngredient : ItemCell
{

   #region Public Variables

   // The color of the item count when there are enough ingredients available
   public Color enoughColor;

   // The color of the item count when there are not enough ingredients available
   public Color notEnoughColor;

   #endregion

   public void setCellForItem (Item item, int availableCount, int requiredCount) {
      base.setCellForItem(item, availableCount);

      // Set a custom item count
      itemCountText.transform.parent.gameObject.SetActive(true);
      itemCountText.SetText(availableCount + "/" + requiredCount);
      
      // Set the color of the item count
      if (availableCount >= requiredCount) {
         itemCountText.color = enoughColor;
      } else {
         itemCountText.color = notEnoughColor;
      }
   }

   #region Private Variables

   #endregion
}