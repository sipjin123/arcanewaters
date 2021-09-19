using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using static Store.StoreItem;

public class StoreItemBox : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerDownHandler, IPointerExitHandler {
   #region Public Variables

   // A unique identifier for the item
   public ulong itemId;

   // The name of the item
   public string itemName;

   // The cost of the item
   public int itemCost;

   // A description of the item
   public string itemDescription;

   // The category of the item
   public Item.Category itemCategory;

   // The quantity of the item
   public int itemQuantity;

   // The text object that contains the item name
   public TMPro.TextMeshProUGUI nameText;

   // The text object that contains the item cost
   public TMPro.TextMeshProUGUI costText;

   // The image icon
   public Image imageIcon;

   // Reference to the border visible when the box is hovered
   public Image hoverBorder;

   // Reference to the border visible when the box is selected
   public Image selectionBorder;

   // The category of the item in the Store UI
   public StoreTab.StoreTabType storeTabCategory;

   #endregion

   public virtual void Start () {
      this.nameText.text = itemName;
      this.costText.text = getDisplayCost();

      toggleHover(false);
      toggleSelection(false);
   }

   public void deselect () {
      toggleSelection(false);
   }

   public void select () {
      toggleHover(false);
      toggleSelection(true);
   }

   public bool isSelected () {
      if (selectionBorder == null) {
         return false;
      }

      return selectionBorder.gameObject.activeSelf;
   }

   public void toggleHover (bool show) {
      if (hoverBorder != null) {
         hoverBorder.gameObject.SetActive(show);
      }
   }

   public void toggleSelection (bool show) {
      if (selectionBorder != null) {
         selectionBorder.gameObject.SetActive(show);
      }
   }

   public virtual void OnPointerEnter (PointerEventData eventData) {
      if (isSelected()) {
         return;
      }

      toggleHover(true);
   }

   public virtual void OnPointerExit(PointerEventData eventData) {   
      toggleHover(false);
   }

   public virtual void OnPointerClick (PointerEventData eventData) {
      StoreScreen.self.onStoreItemBoxClicked(this);
   }

   public virtual void OnPointerDown (PointerEventData eventData) {

   }

   public virtual string getDisplayCost () {
      return itemCost.ToString();
   }

   #region Private Variables

   #endregion
}
