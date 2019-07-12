using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class StoreItemBox : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerDownHandler {
   #region Public Variables

   // A unique identifier for the item
   public int itemId;

   // The name of the item
   public string itemName;

   // The cost of the item
   public int itemCost;

   // A description of the item
   public string itemDescription;

   // The text object that contains the item name
   public Text nameText;

   // The text object that contains the item cost
   public Text costText;

   // The image icon
   public Image imageIcon;

   #endregion

   public virtual void Start () {
      this.nameText.text = itemName;
      this.costText.text = itemCost + "";
   }

   public virtual void OnPointerEnter (PointerEventData eventData) {

   }

   public virtual void OnPointerClick (PointerEventData eventData) {
      StoreScreen.self.selectItem(this);
   }

   public virtual void OnPointerDown (PointerEventData eventData) {

   }

   #region Private Variables

   #endregion
}
