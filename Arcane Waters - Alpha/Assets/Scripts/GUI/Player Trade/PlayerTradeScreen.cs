using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class PlayerTradeScreen : Panel
{
   #region Public Variables

   #endregion

   public override void Update () {
      base.Update();

      if (!NetworkClient.active) {
         return;
      }

      // Toggle showing of the panel
      if (!shouldBeShowing()) {
         if (isShowing()) {
            hide();
         }
      }

      // If we are showing, update GUI accordingly
      if (!isShowing()) {
         return;
      }

      updateTradeScreenGUI();
   }

   private void updateTradeScreenGUI () {
      _titleText.text = "Trading with: " + EntityManager.self.tryGetEntityName(Global.player.getPlayerBodyEntity().tradeTargetUserId, "Unknown");

      PlayerBodyEntity ourPlayer = Global.player.getPlayerBodyEntity();

      _ourPlayerItemGrid.setState(ourPlayer.offeredTradeItems, ourPlayer.offeredGoldTrade, ourPlayer.tradeState == TradeState.ChoosingItems, true);
      if (EntityManager.self.tryGetEntityNotNull(ourPlayer.tradeTargetUserId, out PlayerBodyEntity tradeTarget)) {
         _otherPlayerItemGrid.setState(tradeTarget.offeredTradeItems, tradeTarget.offeredGoldTrade, false, false);
      }

      _statusText.text = getStatusText(ourPlayer.tradeState, tradeTarget.tradeState);

      bool showItemList =
         (ourPlayer.tradeState == TradeState.ReviewingTrade || ourPlayer.tradeState == TradeState.AcceptedTrade)
         &&
         (tradeTarget.tradeState == TradeState.ReviewingTrade || tradeTarget.tradeState == TradeState.AcceptedTrade);

      _itemListLeft.gameObject.SetActive(showItemList);
      _itemListRight.gameObject.SetActive(showItemList);

      _inputBlockLeft.gameObject.SetActive(ourPlayer.tradeState == TradeState.ConfirmedChosenItems || ourPlayer.tradeState == TradeState.AcceptedTrade);
      _inputBlockRight.gameObject.SetActive(tradeTarget.tradeState == TradeState.ConfirmedChosenItems || tradeTarget.tradeState == TradeState.AcceptedTrade);
   }

   private static string getStatusText (TradeState ourState, TradeState otherState) {
      if (ourState == TradeState.None || otherState == TradeState.None) {
         return "Trade was cancelled";
      }

      if (ourState == TradeState.ChoosingItems) {
         return "Choose items you'd like to trade";
      }

      if (ourState == TradeState.ConfirmedChosenItems) {
         if (otherState == TradeState.ChoosingItems) {
            return "Waiting for other player...";
         }
      }

      if (ourState == TradeState.ReviewingTrade) {
         return "Review trade";
      }

      if (ourState == TradeState.AcceptedTrade) {
         if (otherState == TradeState.ReviewingTrade) {
            return "Waiting for other player...";
         }
      }

      return "Trading";
   }

   public void onAddItemButtonClick () {
      if (!shouldBeShowing()) {
         return;
      }

      // Associate a new function with the select button
      PanelManager.self.itemSelectionScreen.selectButton.onClick.RemoveAllListeners();
      PanelManager.self.itemSelectionScreen.selectButton.onClick.AddListener(() => itemSelectionScreenSelected());

      // Associate a new function with the cancel button
      PanelManager.self.itemSelectionScreen.cancelButton.onClick.RemoveAllListeners();
      PanelManager.self.itemSelectionScreen.cancelButton.onClick.AddListener(() => PanelManager.self.itemSelectionScreen.hide());

      // Show the item selection screen
      PanelManager.self.itemSelectionScreen.show(Global.player.getPlayerBodyEntity().offeredTradeItems.Values.Select(i => i.id).ToList());
   }

   public void onRemoveItemClick (int itemId) {
      if (!shouldBeShowing()) {
         return;
      }

      Global.player.rpc.Cmd_SetOfferItemInTrade(itemId, 0);
   }

   public void onSetGoldAmountClick () {
      if (!shouldBeShowing()) {
         return;
      }

      // Capture variables
      bool isGold = true;
      int itemId = 0;

      // Associate a new function with the select button
      PanelManager.self.countSelectionScreen.selectButton.onClick.RemoveAllListeners();
      PanelManager.self.countSelectionScreen.selectButton.onClick.AddListener(() => countSelectionScreenSelected(itemId, isGold));

      // Associate a new function with the cancel button
      PanelManager.self.countSelectionScreen.cancelButton.onClick.RemoveAllListeners();
      PanelManager.self.countSelectionScreen.cancelButton.onClick.AddListener(() => PanelManager.self.countSelectionScreen.hide());

      // Show the item selection screen
      PanelManager.self.countSelectionScreen.show(Global.player.getPlayerBodyEntity().offeredGoldTrade, 0, Global.lastUserGold);
   }

   public void onSetItemAmountClick (int itemId) {
      if (!shouldBeShowing()) {
         return;
      }

      // Capture variables
      bool isGold = false;

      // Associate a new function with the select button
      PanelManager.self.countSelectionScreen.selectButton.onClick.RemoveAllListeners();
      PanelManager.self.countSelectionScreen.selectButton.onClick.AddListener(() => countSelectionScreenSelected(itemId, isGold));

      // Associate a new function with the cancel button
      PanelManager.self.countSelectionScreen.cancelButton.onClick.RemoveAllListeners();
      PanelManager.self.countSelectionScreen.cancelButton.onClick.AddListener(() => PanelManager.self.countSelectionScreen.hide());

      int current = 0;
      if (Global.player.getPlayerBodyEntity().offeredTradeItems.TryGetValue(itemId, out Item item)) {
         current = item.count;
      }

      int max = 1;
      if (Global.lastUserItemCounts.TryGetValue(itemId, out int last)) {
         max = last;
      }

      // Show the item selection screen
      PanelManager.self.countSelectionScreen.show(current, 0, max);
   }

   private void countSelectionScreenSelected (int itemId, bool isGold) {
      // Hide item selection screen
      PanelManager.self.countSelectionScreen.hide();

      if (!shouldBeShowing()) {
         return;
      }

      if (isGold) {
         Global.player.rpc.Cmd_SetOfferGoldInTrade(CountSelectionScreen.selectedCount);
      } else {
         Global.player.rpc.Cmd_SetOfferItemInTrade(itemId, CountSelectionScreen.selectedCount);
      }
   }

   private void itemSelectionScreenSelected () {
      // Hide item selection screen
      PanelManager.self.itemSelectionScreen.hide();

      if (!shouldBeShowing()) {
         return;
      }

      if (ItemSelectionScreen.selectedItem != null) {
         Global.player.rpc.Cmd_SetOfferItemInTrade(ItemSelectionScreen.selectedItem.id, ItemSelectionScreen.selectedItemCount);
      }
   }

   public void startShowingIfNeeded () {
      if (shouldBeShowing() && !isShowing()) {
         PanelManager.self.showPanel(Type.PlayerTrade);
         Update();
      }
   }

   public bool shouldBeShowing () {
      return
         Global.player != null &&
         Global.player is PlayerBodyEntity &&
         Global.player.getPlayerBodyEntity().tradeState != TradeState.None &&
         Global.player.getPlayerBodyEntity().tradeState != TradeState.RequestedTrade &&
         EntityManager.self.tryGetEntityNotNull(Global.player.getPlayerBodyEntity().tradeTargetUserId, out PlayerBodyEntity tradeTarget);
   }

   private void declineTradeIfExists () {
      if (Global.player != null && Global.player is PlayerBodyEntity &&
         Global.player.getPlayerBodyEntity().tradeState != TradeState.None &&
         Global.player.getPlayerBodyEntity().tradeState != TradeState.RequestedTrade) {
         // Immediately remove trade on client
         Global.player.getPlayerBodyEntity().tradeState = TradeState.None;

         Global.player.rpc.Cmd_DeclineTrade(Global.player.getPlayerBodyEntity().tradeTargetUserId);
      }
   }

   public void declineTradeClicked () {
      declineTradeIfExists();
   }

   public void acceptTradeClicked () {
      if (Global.player != null && Global.player is PlayerBodyEntity && Global.player.getPlayerBodyEntity().tradeState != TradeState.None) {
         if (Global.player.getPlayerBodyEntity().tradeState == TradeState.ChoosingItems) {
            Global.player.rpc.Cmd_ConfirmAddedItems();
         } else if (Global.player.getPlayerBodyEntity().tradeState == TradeState.ReviewingTrade) {
            Global.player.rpc.Cmd_ConfirmTradeReview();
         }
      }
   }

   public override void hide () {
      bool wasShowing = isShowing();

      base.hide();

      if (wasShowing) {
         // When panel is hidden, immediately decline the trade
         declineTradeIfExists();
      }
   }

   #region Private Variables

   // Title text of the trade screen
   [SerializeField]
   private Text _titleText = null;

   // Status text of the trade screen
   [SerializeField]
   private Text _statusText = null;

   // The item grid of our player
   [SerializeField]
   private TradeScreenItemGrid _ourPlayerItemGrid = null;

   // The item grid of other player
   [SerializeField]
   private TradeScreenItemGrid _otherPlayerItemGrid = null;

   // Items list objects
   [SerializeField]
   private GameObject _itemListLeft = null;
   [SerializeField]
   private GameObject _itemListRight = null;

   // Input blocks for grids of items
   [SerializeField]
   private GameObject _inputBlockLeft = null;
   [SerializeField]
   private GameObject _inputBlockRight = null;

   #endregion
}
