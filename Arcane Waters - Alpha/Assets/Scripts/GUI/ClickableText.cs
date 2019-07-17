using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class ClickableText : ClientMonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler {
   #region Public Variables

   // The different types of clickable text options
   public enum Type { None = 0, TradeGossip = 1, TradeBluePrint = 2, TradeDeliveryInit = 3, TradeDeliveryComplete = 4 }

   // The Type of clickable text option this is
   public Type textType;

   // A custom Event that gets invoked when we're clicked
   public UnityEvent clickedEvent = new UnityEvent();

   #endregion

   private void Start () {
      // Look up components
      _text = GetComponent<Text>();

      // Note our initial settings
      _initialFontColor = _text.color;

      // Fill in our text
      _text.text = getRowNumber() + ". " + getMessageForType();
   }

   protected string getMessageForType () {
      switch (this.textType) {
         case Type.TradeGossip:
            return "Have you heard about any <color=green>crops</color> that are currently in high demand?";
         case Type.TradeBluePrint:
            return "Would you like <color=green>crops</color> to have a blueprint?";
         case Type.TradeDeliveryInit:
            return "Would you bring me <color=green>(insert)</color> to have a blueprint?";
         case Type.TradeDeliveryComplete:
            return "Oh yey you completed it!";
         case Type.None:
            return "I got nothing for you today!";
      }

      return "";
   }

   protected int getRowNumber () {
      int num = 0;

      foreach (Transform child in this.transform.parent) {
         if (child == this.transform) {
            return num;
         }

         num++;
      }

      return num;
   }

   public void OnPointerEnter (PointerEventData eventData) {
      // Switch our color
      _text.color = Color.yellow;
   }

   public void OnPointerExit (PointerEventData eventData) {
      // Switch back to our original color
      _text.color = _initialFontColor;
   }

   public void OnPointerDown (PointerEventData eventData) {
      _text.fontStyle = FontStyle.Bold;
   }

   public void OnPointerUp (PointerEventData eventData) {
      _text.fontStyle = FontStyle.Normal;

      // Invoke our event that may have been customized by whatever created this clickable text row
      clickedEvent.Invoke();
   }

   #region Private Variables

   // Our Text component
   protected Text _text;

   // Our initial font color
   protected Color _initialFontColor;
      
   #endregion
}
