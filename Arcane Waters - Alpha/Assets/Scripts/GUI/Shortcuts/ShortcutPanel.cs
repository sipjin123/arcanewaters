using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShortcutPanel : ClientMonoBehaviour {
   #region Public Variables

   // Self
   public static ShortcutPanel self;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;
   }

   private void Start () {
      // Look up components
      _canvasGroup = GetComponent<CanvasGroup>();
      _boxes = new List<ShortcutBox>(GetComponentsInChildren<ShortcutBox>());

      // Start hidden
      _canvasGroup.Hide();
      disableShortcuts();
   }

   private void Update () {
      if (Util.isBatch()) {
         return;
      }

      // Hide this panel when we don't have a body
      if (Global.player == null || !(Global.player is PlayerBodyEntity) || Global.isInBattle()) {
         _canvasGroup.Hide();
         disableShortcuts();
      } else {
         _canvasGroup.Show();

         // Disable shortcuts when a panel is opened
         Panel currentPanel = PanelManager.self.currentPanel();
         if (currentPanel != null && currentPanel.type != Panel.Type.Inventory) {
            disableShortcuts();
         } else {
            enableShortcuts();
         }
      }
   }

   public void updatePanelWithShortcuts (ItemShortcutInfo[] shortcuts) {
      // Clear the boxes
      foreach (ShortcutBox box in _boxes) {
         box.clear();
      }

      // Set the items in the corresponding boxes
      foreach (ItemShortcutInfo shortcut in shortcuts) {
         ShortcutBox box = _boxes.Find(b => b.slotNumber == shortcut.slotNumber);
         if (box != null) {
            box.setItem(shortcut.item);
         }
      }
   }

   public void activateShortcut (int slotNumber) {
      ShortcutBox box = _boxes.Find(b => b.slotNumber == slotNumber);
      if (box != null) {
         box.onShortcutPress();
      }
   }

   public ShortcutBox getShortcutBoxAtPosition (Vector2 screenPoint) {
      foreach (ShortcutBox box in _boxes) {
         if (box.isInDropZone(screenPoint)) {
            return box;
         }
      }
      return null;
   }

   private void enableShortcuts () {
      if (!_areShortcutsEnabled) {
         foreach (ShortcutBox box in _boxes) {
            box.button.interactable = true;
         }
         _areShortcutsEnabled = true;
      }
   }

   private void disableShortcuts () {
      if (_areShortcutsEnabled) {
         foreach (ShortcutBox box in _boxes) {
            box.button.interactable = false;
         }
         _areShortcutsEnabled = false;
      }
   }

   #region Private Variables

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   // Our shortcut boxes
   protected List<ShortcutBox> _boxes;

   // Gets set to true when the shortcuts can be used
   protected bool _areShortcutsEnabled = true;

   #endregion
}
