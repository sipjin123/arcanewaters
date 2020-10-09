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
   public ShortcutPanel itemShortcutPanel;

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
         } else if (hasPanelInLinkedList()) {
            unlinkPanel();
         } else if (!((OptionsPanel) get(Panel.Type.Options)).isShowing()) {
            linkPanel(Panel.Type.Options);
         } else {
            unlinkPanel();
         }
      }

      // Don't check for keys if we're typing
      if (ChatManager.isTyping()) {
         return;
      }

      // Bottom button panels
      if (Input.GetKeyDown(KeyCode.C)) {
         BottomBar.self.toggleCharacterInfoPanel();
      } else if (Input.GetKeyDown(KeyCode.I)) {
         BottomBar.self.toggleInventoryPanel();
      } else if (Input.GetKeyDown(KeyCode.G)) {
         BottomBar.self.toggleGuildPanel();
      } else if (Input.GetKeyDown(KeyCode.L)) {
         BottomBar.self.toggleShipsPanel();
      } else if (Input.GetKeyDown(KeyCode.O)) {
         BottomBar.self.toggleOptionsPanel();
      } else if (Input.GetKeyDown(KeyCode.M)) {
         BottomBar.self.toggleMapPanel();
      } else if (Input.GetKeyDown(KeyCode.T)) {
         BottomBar.self.toggleTradeHistoryPanel();
      } else if (Input.GetKeyDown(KeyCode.B)) {
         BottomBar.self.toggleLeaderBoardsPanel();
      } else if (Input.GetKeyDown(KeyCode.F)) {
         BottomBar.self.toggleFriendListPanel();
      } else if (Input.GetKeyDown(KeyCode.U)) {
         BottomBar.self.toggleAbilityPanel();
      } else if (Input.GetKeyDown(KeyCode.K)) {
         BottomBar.self.toggleMailPanel();
      }
   }

   public Panel currentPanel () {
      Panel.Type panelType = _linkedList.First.Value;
      return _panels[panelType];
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

   public Panel linkPanel (Panel.Type panelType) {
      // Hide any currently showing panels
      hidePanels();

      // Check if panel is already in the linkedList
      if (!_linkedList.Contains(panelType)) {
         // Not in linkedList so add the panel type to the front of the linkedList
         _linkedList.AddFirst(panelType);
         return showPanel(panelType);
      }
      else {
         // Panel is already in linkedList.  Find it and move it to the front of the list.
         LinkedListNode<Panel.Type> previous = null;
         LinkedListNode<Panel.Type> current = _linkedList.First;
         LinkedListNode<Panel.Type> temp = null;

         while (current != null) {
            if (panelType == current.Value) { // item has been found
               previous = current.Next;
               temp = current;
               _linkedList.Remove(current);
               _linkedList.AddFirst(temp);
               return showPanel(_linkedList.First.Value);
            }
            previous = current;
            current = current.Next;
         }
         return _panels[Panel.Type.Options];
      }
   }

   public void linkIfNotShowing (Panel.Type panelType) {
      // If it's already showing, we're done
      if (get(panelType).isShowing()) {
         return;
      }

      linkPanel(panelType);
   }

   public void unlinkPanel () {
      hidePanels();

      // If the linkedList is already empty, we don't have to do anything
      if (_linkedList.Count == 0) {
         return;
      }

      // Remove the panel from top of the stack
      _linkedList.Remove(_linkedList.First);

      // If the linkedList is empty now, we're done
      if (_linkedList.Count == 0) {
         return;
      }

      // Show whatever panel is now at the front of linkedList
      showPanel(_linkedList.First.Value);
   }

   public bool hasPanelInLinkedList () {
      foreach (Panel panel in panelStack) {
         if (panel.gameObject.activeSelf && panel.canvasGroup.alpha > 0f) {
            return true;
         }
      }

      return false;
   }

   public void togglePanel (Panel.Type panelType) {
      Panel panel = get(panelType);

      // If it's already showing, remove it from the front of the linkedList
      if (panel.isShowing()) {
         unlinkPanel();
      } else {
         // Otherwise, just add it to the linkedList and show it now
         linkPanel(panelType);
      }
   }

   public void toggleMainPanel (Panel.Type panelType) {
      Panel panel = get(panelType);
      bool wasShowing = panel.gameObject.activeSelf;

      // Whenever a main panel is toggled either on or off, always clear out the display list
      clearLinkedList();

      // Link the panel if it wasn't already showing
      if (!wasShowing) {
         linkPanel(panelType);
      }
   }

   public void clearLinkedList () {
      _linkedList.Clear();

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

      D.debug("Couldn't find modal panel with type: " + panelType);
      return;
   }

   public void clearAll () {

      // Clear the linkedList of optional panels
      clearLinkedList();

      // Clear out all of our non-optional panels
      foreach (Panel panel in modalPanels) {
         panel.hide();
      }
   }

   public void showConfirmationPanel (string message, UnityAction onConfirm, UnityAction onCancel, bool hideOnConfirmation = true) {
      confirmScreen.confirmButton.onClick.RemoveAllListeners();
      confirmScreen.confirmButton.onClick.AddListener(onConfirm);

      if (hideOnConfirmation) {
         confirmScreen.confirmButton.onClick.AddListener(() => confirmScreen.hide());
      }

      confirmScreen.cancelButton.onClick.RemoveAllListeners();
      confirmScreen.cancelButton.onClick.AddListener(onCancel);
      confirmScreen.cancelButton.onClick.AddListener(() => confirmScreen.hide());

      confirmScreen.show(message);
   }

   public void showConfirmationPanel (string message, UnityAction onConfirm, bool hideOnConfirmation = true) {
      confirmScreen.confirmButton.onClick.RemoveAllListeners();
      confirmScreen.confirmButton.onClick.AddListener(onConfirm);

      if (hideOnConfirmation) {
         confirmScreen.confirmButton.onClick.AddListener(() => confirmScreen.hide());
      }

      confirmScreen.cancelButton.onClick.RemoveAllListeners();
      confirmScreen.cancelButton.onClick.AddListener(() => confirmScreen.hide());

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

   // The linkedList of panel types that are currently open
   protected LinkedList<Panel.Type> _linkedList = new LinkedList<Panel.Type>();

   #endregion
}