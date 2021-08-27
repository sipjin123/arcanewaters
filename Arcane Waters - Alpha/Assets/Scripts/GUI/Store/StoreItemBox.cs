using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using static Store.StoreItem;

public class StoreItemBox : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerDownHandler {
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

   #endregion

   public virtual void Start () {
      this.nameText.text = itemName;
      this.costText.text = getDisplayCost();
   }

   public virtual void OnPointerEnter (PointerEventData eventData) {

   }

   public virtual void OnPointerClick (PointerEventData eventData) {
      StoreScreen.self.selectItem(this);
   }

   public virtual void OnPointerDown (PointerEventData eventData) {

   }

   public virtual string getDisplayCost () {
      return itemCost.ToString();
   }

   #region Private Variables

   #endregion
}
