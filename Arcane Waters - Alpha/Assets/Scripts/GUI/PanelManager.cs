using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class PanelManager : MonoBehaviour {
   #region Public Variables

   // The stack of panels we want to manage (generally optional panels that can be discarded at any time)
   public List<Panel> panelStack;

   // The modal panels that we want to manage (important panels shown one at a time, require action by the user)
   public List<Panel> modalPanels;

   // Screens which are separate from the panel stack
   public ConfirmScreen confirmScreen;
   public NoticeScreen noticeScreen;
   public TradeConfirmScreen tradeConfirmScreen;
   public ItemSelectionScreen itemSelectionScreen;
   public VoyageInviteScreen voyageInviteScreen;
   public LoadingScreen loadingScreen;
   public CountdownScreen countdownScreen;
   public ContextMenuPanel contextMenuPanel;

   // Helps Determine which active panel requires inventory data (crafting/inventory)
   public Panel.Type selectedPanel;

   // Self
   public static PanelManager self;

   #endregion

   void Awake () {
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
   }

   private void Update () {
      // Let us easily close panels with the Escape key
      if (Input.GetKeyUp(KeyCode.Escape)) {
         // The chat input field might remain selected
         if (EventSystem.current.currentSelectedGameObject == ChatPanel.self.inputField.gameObject) {
            EventSystem.current.SetSelectedGameObject(null);
         } else if (confirmScreen.canvasGroup.alpha > 0f) {
            confirmScreen.hide();
         } else if (noticeScreen.canvasGroup.alpha > 0f) {
            noticeScreen.hide();
         } else if (hasPanelInStack()) {
            popPanel();
         } else if (!((OptionsPanel) get(Panel.Type.Options)).isShowing()) {
            pushPanel(Panel.Type.Options);
         } else {
            popPanel();
         }
      }

      // Don't check for keys if we're typing
      if (ChatManager.isTyping()) {
         return;
      }

      // Bottom button panels
      if (Input.GetKeyUp(KeyCode.C)) {
         BottomBar.self.toggleCharacterInfoPanel();
      } else if (Input.GetKeyUp(KeyCode.I)) {
         BottomBar.self.toggleInventoryPanel();
      } else if (Input.GetKeyUp(KeyCode.G)) {
         BottomBar.self.toggleGuildPanel();
      } else if (Input.GetKeyUp(KeyCode.L)) {
         BottomBar.self.toggleShipsPanel();
      } else if (Input.GetKeyUp(KeyCode.O)) {
         BottomBar.self.toggleOptionsPanel();
      } else if (Input.GetKeyUp(KeyCode.M)) {
         BottomBar.self.toggleMapPanel();
      } else if (Input.GetKeyUp(KeyCode.T)) {
         BottomBar.self.toggleTradeHistoryPanel();
      } else if (Input.GetKeyUp(KeyCode.B)) {
         BottomBar.self.toggleLeaderBoardsPanel();
      } else if (Input.GetKeyUp(KeyCode.F)) {
         BottomBar.self.toggleFriendListPanel();
      } else if (Input.GetKeyUp(KeyCode.U)) {
         BottomBar.self.toggleAbilityPanel();
      } else if (Input.GetKeyUp(KeyCode.K)) {
         BottomBar.self.toggleMailPanel();
      }
   }

   public Panel currentPanel () {
      Panel.Type panelType = _stack.Peek();
      return _panels[panelType];
   }

   public Panel get (Panel.Type panelType) {
      return _panels[panelType];
   }

   public Panel pushPanel (Panel.Type panelType) {
      // Hide any currently showing panels
      hidePanels();

      // Add the panel type to the top of our stack
      _stack.Push(panelType);
      return showPanel(panelType);
   }

   public void pushIfNotShowing (Panel.Type panelType) {
      // If it's already showing, we're done
      if (get(panelType).isShowing()) {
         return;
      }

      pushPanel(panelType);
   }

   public void popPanel () {
      hidePanels();

      // If the stack is already empty, we don't have to do anything
      if (_stack.Count == 0) {
         return;
      }

      // Remove the panel from top of the stack
      _stack.Pop();

      // If the stack is empty now, we're done
      if (_stack.Count == 0) {
         return;
      }

      // Show whatever panel is now at the top of the stack
      showPanel(_stack.Peek());
   }

   public bool hasPanelInStack () {
      foreach (Panel panel in panelStack) {
         if (panel.gameObject.activeSelf && panel.canvasGroup.alpha > 0f) {
            return true;
         }
      }

      return false;
   }

   public void togglePanel (Panel.Type panelType) {
      Panel panel = get(panelType);

      // If it's already showing, pop it off the stack
      if (panel.isShowing()) {
         popPanel();
      } else {
         // Otherwise, just push it on the stack and show it now
         pushPanel(panelType);
      }
   }

   public void toggleMainPanel (Panel.Type panelType) {
      Panel panel = get(panelType);
      bool wasShowing = panel.gameObject.activeSelf;

      // Whenever a main panel is toggled either on or off, always clear out the display stack
      clearStack();

      // Push the panel if it wasn't already showing
      if (!wasShowing) {
         pushPanel(panelType);
      }
   }

   public void clearStack () {
      _stack.Clear();

      // Make sure nothing is showing now
      hidePanels();
   }

   public Panel getModalPanel (Panel.Type panelType) {
      foreach (Panel panel in modalPanels) {
         if (panel.type == panelType) {
            return panel;
         }
      }

      return null;
   }

   public void showModalPanel (Panel.Type panelType) {
      // Hide everything else
      clearAll();

      // Look for the panel in our list of modal panels
      foreach (Panel panel in modalPanels) {
         if (panel.type == panelType) {
            panel.show();
            return;
         }
      }

      D.warning("Couldn't find modal panel with type: " + panelType);
      return;
   }

   public void clearAll () {
      // Clear the stack of optional panels
      clearStack();

      // Clear out all of our non-optional panels
      foreach (Panel panel in modalPanels) {
         panel.hide();
      }
   }

   public void showConfirmationPanel(string message, UnityAction action, bool hideOnConfirmation = true) {
      confirmScreen.confirmButton.onClick.RemoveAllListeners();
      confirmScreen.confirmButton.onClick.AddListener(action);

      if (hideOnConfirmation) {
         confirmScreen.confirmButton.onClick.AddListener(() => confirmScreen.hide());
      }

      confirmScreen.show(message);
   }

   protected Panel showPanel (Panel.Type panelType) {
      Panel panel = _panels[panelType];
      panel.show();

      return _panels[panelType];
   }

   protected void hidePanels () {
      // Cycle over all of our panels and make sure they're all disabled
      foreach (Panel panel in _panels.Values) {
         panel.hide();
      }
   }

   #region Private Variables

   // The Panel instances we know about
   protected Dictionary<Panel.Type, Panel> _panels = new Dictionary<Panel.Type, Panel>();

   // The stack of panel types that are currently open
   protected Stack<Panel.Type> _stack = new Stack<Panel.Type>();

   #endregion
}