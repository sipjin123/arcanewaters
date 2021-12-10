using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System;

public class Panel : MonoBehaviour, IPointerClickHandler
{
   #region Public Variables

   // The type of Panel this is
   public enum Type {
      None = 0, Login = 1, CharSelect = 2, CharCreate = 3, Options = 4, BuySellCargo = 5,
      Starved = 6, Inventory = 7, Sound = 8, Ship = 9, Shipyard = 10,
      ItemShop = 11, CharPreview = 12, WorldMap = 13, Store = 14, Merchant = 15,
      Adventure = 16, Guild = 18, Flagship = 20,
      NPC_Panel = 21, Overworld = 22, Craft = 23, Reward = 24, TradeHistory = 25, RandomMaps = 26,
      LeaderBoards = 27, FriendList = 28, Ability_Panel = 29, Mail = 30, Team_Combat = 31,
      Voyage = 32, BookReader = 33, Companion = 34, Discovery = 35,
      CustomMaps = 38, StepCompletedNotification = 39, Auction = 41, Keybindings = 42,
      ReturnToCurrentVoyagePanel = 43, CharacterInfo = 44, AdminVoyage = 45, PvpScoreBoard = 46,
      PvpArena = 47, Admin = 48, PvpShop = 49, VisitPanel = 50
   }

   // The type of Panel this is
   public Type type;

   // A convenient reference to the Canvas Group
   public CanvasGroup canvasGroup;

   // If we want this panel to be draggable, assign the Rect transform here
   public RectTransform draggableRect;

   // Event to call when a panel is opened
   public static UnityAction OnPanelOpened;

   // Event to call when a panel is closed
   public static UnityAction OnPanelClosed;

   #endregion

   public virtual void Awake () {

   }

   public virtual void Start () {

   }

   public virtual void Update () {

   }

   public virtual void show () {
      // If we're already active and visible, then there's nothing to do
      if (this.gameObject.activeSelf && canvasGroup.alpha == 1) {
         return;
      }
      
      InputManager.self.inputMaster.UIShotcuts.Disable();
      InputManager.self.inputMaster.UIControl.Enable();

      // Start invisible initially
      canvasGroup.alpha = 0f;
      canvasGroup.interactable = true;
      canvasGroup.blocksRaycasts = true;

      // Then turn on the game object so that everything gets positioned
      this.gameObject.SetActive(true);

      // Make visible
      canvasGroup.alpha = 1f;

      // Call event
      if (OnPanelOpened != null) {
         OnPanelOpened();
      }

      // Throw a warning if the panel is displayed without adding it to the panel list
      if (!PanelManager.self.isFirstPanelInLinkedList(type)) {
         D.warning("The panel " + type + " has not been added to the panel list. Use PanelManager.linkIfNotShowing() to show panels.");
      }
   }

   public virtual void hide () {
      InputManager.self.inputMaster.UIShotcuts.Enable();
      InputManager.self.inputMaster.UIControl.Disable();
      
      // Make sure we're fully hidden
      canvasGroup.alpha = 0f;
      canvasGroup.interactable = false;
      canvasGroup.blocksRaycasts = false;
      this.gameObject.SetActive(false);

      // Call event
      if (OnPanelClosed != null) {
         OnPanelClosed();
      }
   }

   public bool isShowing () {
      return this.gameObject.activeSelf && canvasGroup.alpha > 0f;
   }

   public virtual void close () {
      if (isShowing()) {
         PanelManager.self.unlinkPanel();
      }
   }

   public void titlebarDrag (BaseEventData baseData) {
      PointerEventData data = (PointerEventData) baseData;

      if (draggableRect != null) {
         draggableRect.anchoredPosition += data.delta;
      }
   }

   public virtual void OnPointerClick (PointerEventData eventData) {
      if (eventData.rawPointerPress == this.gameObject) {
         // If the mouse is over the input field zone, select it through the panel black background
         if (!tryFocusChatInputField()) {
            // If the black background outside is clicked, hide the panel
            close();
         }
      }
   }

   public static bool tryFocusChatInputField () {
      if (ChatPanel.self != null && ChatPanel.self.isPointerOverInputFieldZone()) {
         ChatPanel.self.focusInputField();
         return true;
      }
      return false;
   }

   #region Private Variables

   #endregion
}
