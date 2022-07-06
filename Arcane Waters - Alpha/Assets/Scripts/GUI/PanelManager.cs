using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PanelManager : GenericGameManager
{
   #region Public Variables

   // The stack of panels we want to manage (generally optional panels that can be discarded at any time)
   public List<Panel> panelStack;

   // Screens which are separate from the panel stack
   public ConfirmScreen confirmScreen;
   public NoticeScreen noticeScreen;
   public TradeConfirmScreen tradeConfirmScreen;
   public ItemSelectionScreen itemSelectionScreen;
   public CountSelectionScreen countSelectionScreen;
   public GroupInviteScreen groupInviteScreen;
   public LoadingScreen loadingScreen;
   public CountdownScreen countdownScreen;
   public ContextMenuPanel contextMenuPanel;
   public ShortcutPanel itemShortcutPanel;
   public NotificationPanel notificationPanel;
   public AdminGameSettingsPanel adminGameSettingsPanel;
   public GenericActionPromptScreen genericPromptScreen;

   // Reference to the main Canvas - GUI in the scene
   public Canvas mainCanvasRef;

   // Helps Determine which active panel requires inventory data (crafting/inventory)
   public Panel.Type selectedPanel;

   // Gets set to true when panel data is being requested from the server and a panel will soon open
   public static bool isLoading = false;

   // Self
   public static PanelManager self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;

      foreach (Panel panel in panelStack) {
         _panels[panel.type] = panel;

         // Store references to the Canvas Groups
         panel.canvasGroup = panel.GetComponent<CanvasGroup>();
      }
   }

   void Start () {
      // Remove the initial text on the confirm screen
      confirmScreen.hide();

      // Start off with only the login screen visible
      // showModalPanel(Panel.Type.Login);

      // Initialize list with separate panels to avoid calling FindObjectsOfType after every player's LMB click/press
      _fullScreenSeparatePanels = mainCanvasRef.gameObject.GetComponentsInChildren<FullScreenSeparatePanel>(true);
   }

   private void Update () {
      // Skip update for batch mode - server
      if (Util.isBatch()) {
         return;
      }

      // Skip if the game window is not in focus
      if (!Application.isFocused) {
         return;
      }

      // If a panel was loading and one shows up, disable the loading status
      if (isLoading && isAnyPanelShowing()) {
         isLoading = false;
      }

      // Let us easily close panels with the Escape key
      if (
         !TitleScreen.self.isShowing() &&
         !CharacterScreen.self.isShowing() &&
         (InputManager.self.inputMaster?.UIControl.Close.WasPerformedThisFrame() == true || Keyboard.current.escapeKey.wasPressedThisFrame)
      ) {
         onEscapeKeyPressed();
      }

      // Don't check for keys if we're typing
      if (ChatManager.isTyping()) {
         return;
      }

      // TODO: Setup gamepad keybindings here using actions instead of keyboard keys

      // Disable hotkeys when not focused on game window
      if (!InputManager.self.isFocused) {
         return;
      }

      // Bottom button panels
      if (InputManager.self.inputMaster?.UIShotcuts.Inventory.WasPressedThisFrame() == true) {
         BottomBar.self.toggleInventoryPanel();
      } else if (InputManager.self.inputMaster?.UIShotcuts.GuildInfo.WasPressedThisFrame() == true) {
         BottomBar.self.toggleGuildPanel();
      } else if (InputManager.self.inputMaster?.UIShotcuts.ShipList.WasPressedThisFrame() == true) {
         BottomBar.self.toggleShipsPanel();
      } else if (
         !TitleScreen.self.isShowing() &&
         !PvpShopPanel.self.isActive() &&
         InputManager.self.inputMaster?.UIControl.Close.WasPerformedThisFrame() != true &&
         !Keyboard.current.escapeKey.wasPressedThisFrame &&
         InputManager.self.inputMaster?.UIShotcuts.Options.WasPressedThisFrame() == true
      ) {
         BottomBar.self.toggleOptionsPanel();
      } else if (InputManager.self.inputMaster?.UIShotcuts.Map.WasPressedThisFrame() == true) {
         BottomBar.self.toggleMapPanel();
      } else if (InputManager.self.inputMaster?.UIShotcuts.Store.WasPressedThisFrame() == true) {
         BottomBar.self.toggleStorePanel();
      } else if (InputManager.self.inputMaster?.UIShotcuts.TradeHistory.WasPressedThisFrame() == true) {
         BottomBar.self.toggleTradeHistoryPanel();
      } else if (InputManager.self.inputMaster?.UIShotcuts.FriendsList.WasPressedThisFrame() == true) {
         BottomBar.self.toggleFriendListPanel();
      } else if (InputManager.self.inputMaster?.UIShotcuts.VisitFriend.WasPressedThisFrame() == true) {
         BottomBar.self.toggleFriendVisitPanel();
      } else if (InputManager.self.inputMaster?.UIShotcuts.Abilities.WasPressedThisFrame() == true) {
         BottomBar.self.toggleAbilityPanel();
      } else if (InputManager.self.inputMaster?.UIShotcuts.Mail.WasPressedThisFrame() == true) {
         BottomBar.self.toggleMailPanel();
      } else if (KeyUtils.GetKeyDown(Key.F7)) {
         get<AdminInstanceListPanel>(Panel.Type.AdminInstanceList).togglePanel();
      } else if (KeyUtils.GetKeyDown(Key.F6)) {
         adminGameSettingsPanel.togglePanel();
      } else if (KeyUtils.GetKeyDown(Key.V)) {
         if (Global.isLoggedInAsAdmin()) {
            AdminPanel.self.show();
         }
      }

      // Handle escape button only when character screen is active
      else if (CharacterScreen.self.isShowing() && KeyUtils.GetKeyUp(Key.Escape)) {
         // Show option on character screen to give user logout option
         if (!get(Panel.Type.Options).isShowing()) {
            showPanel(Panel.Type.Options);
         } else {
            hideCurrentPanel();
         }
      }

      // Show trade screen when needed
      get<PlayerTradeScreen>(Panel.Type.PlayerTrade).startShowingIfNeeded();

      if (InputManager.self.inputMaster?.Pvp.Stat.WasReleasedThisFrame() == true) {
         BottomBar.self.disablePvpStatPanel();
      } else if (InputManager.self.inputMaster?.Pvp.Stat.WasPressedThisFrame() == true) {
         BottomBar.self.enablePvpStatPanel();
      }
   }

   public void onEscapeKeyPressed () {
      // Hide tooltips before closing the panel
      hideToolTips();

      // The chat input field might remain selected
      if (EventSystem.current?.currentSelectedGameObject == ChatPanel.self.inputField.gameObject || EventSystem.current?.currentSelectedGameObject == ChatPanel.self.nameInputField.gameObject) {
         EventSystem.current?.SetSelectedGameObject(null);
      } else if (confirmScreen.canvasGroup.alpha > 0f) {
         confirmScreen.hide();
      } else if (noticeScreen.canvasGroup.alpha > 0f) {
         noticeScreen.hide();
      } else if (PerksPanel.self.isShowing()) {
         PerksPanel.self.hide();
      } else if (itemSelectionScreen.isShowing()) {
         itemSelectionScreen.hide();
      } else if (countSelectionScreen.isShowing()) {
         countSelectionScreen.cancelButton.onClick?.Invoke();
      } else if (get<AuctionPanel>(Panel.Type.Auction).auctionInfoPanel.isShowing()) {
         AuctionPanel.self.auctionInfoPanel.hide();
      } else if (get<PvpStatPanel>(Panel.Type.PvpScoreBoard).isShowing()) {
         hideCurrentPanel();
      } else if (PvpShopPanel.self.isActive()) {
         PvpShopPanel.self.hideEntirePanel();
      } else if (get<NoticeBoardPanel>(Panel.Type.NoticeBoard).pvpArenaSection.pvpArenaInfoPanel.isShowing()) {
         NoticeBoardPanel.self.pvpArenaSection.pvpArenaInfoPanel.hide();
      } else if (isAnyPanelShowing()) {
         hideCurrentPanel();
      } else if (
         !((OptionsPanel) get(Panel.Type.Options)).isShowing() &&
         !TitleScreen.self.termsOfServicePanel.activeSelf
      ) {
         // Play SFX
         SoundEffectManager.self.playGuiMenuOpenSfx();

         showPanel(Panel.Type.Options);
      }
   }

   public Panel currentPanel () {
      foreach (Panel panel in panelStack) {
         if (panel.gameObject.activeSelf && panel.canvasGroup.alpha > 0f) {
            return panel;
         }
      }
      return null;
   }

   public Panel get (Panel.Type panelType) {
      if (_panels.ContainsKey(panelType)) {
         return _panels[panelType];
      } else {
         D.editorLog("Panel does not exist!", Color.red);
         return _panels[Panel.Type.Options];
      }
   }

   public T get<T> (Panel.Type panelType) where T : Panel {
      return get(panelType) as T;
   }

   public void showPanel (Panel.Type panelType) {
      // If it's already showing, we're done
      if (get(panelType).isShowing()) {
         return;
      }

      // Hide any currently showing panels
      hideCurrentPanel();

      // Show the requested panel
      Panel panel = _panels[panelType];
      panel.show();
   }

   public void hideCurrentPanel () {
      // Cycle over all of our panels and make sure they're all disabled
      foreach (Panel panel in _panels.Values) {
         panel.hide();
      }
   }

   public bool isAnyPanelShowing () {
      foreach (Panel panel in panelStack) {
         if (panel.gameObject.activeSelf && panel.canvasGroup.alpha > 0f) {
            return true;
         }
      }

      return false;
   }

   public bool areMultiplePanelsShowing () {
      int showingCount = 0;
      foreach (Panel panel in panelStack) {
         if (panel.gameObject.activeSelf && panel.canvasGroup.alpha > 0f) {
            showingCount++;
         }
      }

      return showingCount > 1;
   }

   public void togglePanel (Panel.Type panelType) {
      Panel panel = get(panelType);

      // If it's already showing, hide it
      if (panel.isShowing()) {
         hideCurrentPanel();
      } else {
         // Otherwise, show it
         showPanel(panelType);
      }

      // Hide any tooltips that were previously showing
      hideToolTips();
   }

   public void hideAllPanels () {
      hideCurrentPanel();

      tradeConfirmScreen.hide();
      itemSelectionScreen.hide();
      countSelectionScreen.hide();
      GroupManager.self.hideGroupInvitation();
      MapCustomization.CustomizationUI.ensureHidden();

      if (CharacterCreationPanel.self.isShowing()) {
         CharacterCreationPanel.self.cancelCreating();
      }

      if (PvpShopPanel.self != null) {
         PvpShopPanel.self.hideEntirePanel();
      }

      foreach (Panel panel in FindObjectsOfType<Panel>()) {
         if (panel.isShowing()) {
            panel.hide();
         }
      }
   }

   public void showConfirmationPanel (string message, UnityAction onConfirm, UnityAction onCancel = null, bool hideOnConfirmation = true, int cost = 0, string description = "", bool showInput = false, string keyword = "") {
      if (showInput) {
         confirmScreen.enableConfirmInputField(keyword);
      }

      confirmScreen.confirmButton.onClick.RemoveAllListeners();
      confirmScreen.confirmButton.onClick.AddListener(onConfirm);

      if (hideOnConfirmation) {
         confirmScreen.confirmButton.onClick.AddListener(() => confirmScreen.hide());
      }

      confirmScreen.cancelButton.onClick.RemoveAllListeners();
      if (onCancel != null) {
         confirmScreen.cancelButton.onClick.AddListener(onCancel);
      }

      confirmScreen.cancelButton.onClick.AddListener(() => confirmScreen.hide());

      confirmScreen.show(message, cost, description);
   }

   public void hideToolTips () {
      // Check for any opened tooltips and close them
      foreach (GameObject toolTip in UIToolTipManager.openTooltips) {
         toolTip.GetComponent<CanvasGroup>().alpha = 0;
         toolTip.GetComponent<CanvasGroup>().blocksRaycasts = false;
      }
   }

   public bool isFullScreenSeparatePanelShowing () {
      if (_fullScreenSeparatePanels != null) {
         foreach (FullScreenSeparatePanel panel in _fullScreenSeparatePanels) {
            if (panel.gameObject.activeSelf) {
               foreach (CanvasGroup canvas in panel.allCanvasGroups) {
                  if (canvas && canvas.enabled && canvas.alpha > 0) {
                     return true;
                  }
               }
            }
         }
      }

      return false;
   }

   public void showPowerupPanel () {
      PowerupPanel.self.gameObject.SetActive(true);
   }

   public void hidePowerupPanel () {
      if (!ClientManager.isApplicationQuitting) {
         PowerupPanel.self.gameObject.SetActive(false);
      }
   }

   #region Private Variables

   // The Panel instances we know about
   protected Dictionary<Panel.Type, Panel> _panels = new Dictionary<Panel.Type, Panel>();

   // The linkedList of panel types that are currently open
   protected LinkedList<Panel.Type> _linkedList = new LinkedList<Panel.Type>();

   // The panels that are never linked. They're taking full screen, don't need to be managed but they can sometimes be blocked by Context Menu
   protected FullScreenSeparatePanel[] _fullScreenSeparatePanels;

   #endregion
}