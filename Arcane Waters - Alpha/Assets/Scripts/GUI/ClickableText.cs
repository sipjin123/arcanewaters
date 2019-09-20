using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class ClickableText : ClientMonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler {
   #region Public Variables

   // The different types of clickable text options
   public enum Type { None = 0, TradeGossip = 1, TradeBluePrint = 2, TradeDeliveryInit = 3, TradeDeliveryComplete = 4 , QuestAccept = 5, TradeDeliverySuccess = 6, TradeDeliveryFail = 7, AcceptReward =8,
   HuntInit = 9, HuntComplete = 10, HuntSuccess = 11, HuntFail = 12, NPCDialogueOption = 13, NPCDialogueEnd = 14, TradeGossipThanks = 15}

   // The Type of clickable text option this is
   public Type textType;

   // Our Text component
   public Text textComp;

   // A custom Event that gets invoked when we're clicked
   public UnityEvent clickedEvent = new UnityEvent();

   #endregion

   public void initData (Type textType) {
      // Note our initial settings
      _initialFontColor = textComp.color;

      // Set the type
      this.textType = textType;

      // Fill in our text
      textComp.text = getMessageForType();
   }

   public void initData (Type textType, string msg) {
      initData(textType);

      // Fill in our text
      textComp.text = msg;
   }

   public void disablePointerEvents (Color disabledColor) {
      _interactable = false;
      textComp.color = disabledColor;
   }

   protected string getMessageForType () {
      switch (this.textType) {
         case Type.TradeGossip:
            return "Have you heard about any <color=green>crops</color> that are currently in high demand?";
         case Type.TradeBluePrint:
            return "May I have a blueprint?";
         case Type.TradeDeliveryInit:
            return "I will bring you these items";
         case Type.QuestAccept:
            return "Sure what do you want?";
         case Type.TradeDeliveryComplete:
            return "Thank you sir";
         case Type.None:
            return "Have a good day then";
         case Type.TradeDeliverySuccess:
            return "I got all your goods";
         case Type.TradeDeliveryFail:
            return "I still dont have enough";
         case Type.AcceptReward:
            return "Thankyou very much!";
         case Type.HuntInit:
            return "Let the hunt begin!";
         case Type.HuntComplete:
            return "It was not easy!";
         case Type.HuntSuccess:
            return "Target eliminated!";
         case Type.HuntFail:
            return "I havent killed it yet!";
         case Type.NPCDialogueEnd:
            return "Goodbye!";
         case Type.TradeGossipThanks:
            return "Thank you!";
      }

      return "";
   }

   public void OnPointerEnter (PointerEventData eventData) {
      if (_interactable) {
         // Switch our color
         textComp.color = Color.yellow;
      }
   }

   public void OnPointerExit (PointerEventData eventData) {
      if (_interactable) {
         // Switch back to our original color
         textComp.color = _initialFontColor;
      }
   }

   public void OnPointerDown (PointerEventData eventData) {
      if (_interactable) {
         textComp.fontStyle = FontStyle.Bold;
      }
   }

   public void OnPointerUp (PointerEventData eventData) {
      if (_interactable) {
         textComp.fontStyle = FontStyle.Normal;

         // Invoke our event that may have been customized by whatever created this clickable text row
         clickedEvent.Invoke();
      }
   }

   #region Private Variables

   // Our initial font color
   protected Color _initialFontColor;

   // Gets set to true when the text must react to pointer events
   protected bool _interactable = true;
      
   #endregion
}