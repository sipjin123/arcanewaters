using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class Panel : MonoBehaviour {
   #region Public Variables

   // The type of Panel this is
   public enum Type {
      None = 0, Login = 1, CharSelect = 2, CharCreate = 3, Options = 4, BuySellCargo = 5,
      Starved = 6, Inventory = 7, Sound = 8, Ship = 9, Shipyard = 10,
      ItemShop = 11, CharPreview = 12, WorldMap = 13, Store = 14, Merchant = 15,
      Adventure = 16, CharacterInfo = 17, Guild = 18, GuildCreate = 19, Flagship = 20,
      NPC_Panel = 21, Overworld = 22, Craft = 23, Reward = 24, TradeHistory = 25, RandomMaps = 26,
      LeaderBoards = 27, FriendList = 28, Ability_Panel = 29, Mail = 30, Team_Combat = 31,
      Voyage = 32, BookReader = 33, Companion = 34, Discovery = 35,
      CustomMaps = 38, StepCompletedNotification = 39, MapCustomization = 40, Auction = 41,
      Keybindings = 42
   }

   // The type of Panel this is
   public Type type;

   // A convenient reference to the Canvas Group
   public CanvasGroup canvasGroup;

   // If we want this panel to be draggable, assign the Rect transform here
   public RectTransform draggableRect;

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

      // Start invisible initially
      canvasGroup.alpha = 0f;
      canvasGroup.interactable = true;

      // Then turn on the game object so that everything gets positioned
      this.gameObject.SetActive(true);

      // Make us visible after 1 frame, once everything is set up
      StartCoroutine(CO_Show());
   }

   public virtual void hide () {
      // Make sure we're fully hidden
      canvasGroup.alpha = 0f;
      canvasGroup.interactable = false;
      this.gameObject.SetActive(false);
   }

   public bool isShowing () {
      return this.gameObject.activeSelf && canvasGroup.alpha > 0f;
   }

   public void titlebarDrag (BaseEventData baseData) {
      PointerEventData data = (PointerEventData) baseData;

      if (draggableRect != null) {
         draggableRect.anchoredPosition += data.delta;
      }
   }

   protected IEnumerator CO_Show () {
      // Wait 1 frame
      yield return null;

      canvasGroup.alpha = 1f;
   }

   #region Private Variables

   #endregion
}
