using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class ClickableTab : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerDownHandler {
   #region Public Variables

   // Whether or not this tab is currently enabled
   public bool isEnabled;

   // Whether or not this tab can be clicked to turn it off
   public bool canBeTurnedOff = false;

   // Set to true if one and only one tab can be active at any time
   public bool isExclusive;

   #endregion

   private void Start () {
      _image = GetComponent<Image>();
      _text = GetComponentInChildren<Text>();
      _imageOutline = _image.GetComponent<Outline>();

      if (_text != null) {
         _originalTextColor = _text.color;
      }
      _originalImageColor = _image.color;
   }

   private void Update () {
      // Update our text and image based on whether we're enabled
      if (_text != null) {
         _text.color = isEnabled ? _originalTextColor : Color.gray;
      }
      _image.color = isEnabled ? _originalImageColor : Color.gray;
      _imageOutline.enabled = isEnabled;
   }

   public virtual void OnPointerEnter (PointerEventData eventData) {
      
   }

   public virtual void OnPointerClick (PointerEventData eventData) {

   }

   public virtual void OnPointerDown (PointerEventData eventData) {
      // Some tabs aren't allowed to be toggled off
      if (isEnabled && !canBeTurnedOff) {
         return;
      }

      isEnabled = !isEnabled;

      // If the tab is exclusive, make sure none of the other tabs are on
      if (isExclusive) {
         foreach (ClickableTab tab in getAllOtherTabsInGroup()) {
            tab.isEnabled = false;
         }
      }
   }

   public List<ClickableTab> getAllTabsInGroup () {
      List<ClickableTab> list = new List<ClickableTab>();

      foreach (ClickableTab tab in this.transform.parent.GetComponentsInChildren<ClickableTab>()) {
         list.Add(tab);
      }

      return list;
   }

   public List<ClickableTab> getAllOtherTabsInGroup () {
      List<ClickableTab> list = new List<ClickableTab>();

      foreach (ClickableTab tab in this.transform.parent.GetComponentsInChildren<ClickableTab>()) {
         if (tab != this) {
            list.Add(tab);
         }
      }

      return list;
   }

   #region Private Variables

   // Our Text
   protected Text _text;

   // Our image
   protected Image _image;

   // Our image outline
   protected Outline _imageOutline;

   // The original text color
   protected Color _originalTextColor;

   // The original image color
   protected Color _originalImageColor;

   #endregion
}
