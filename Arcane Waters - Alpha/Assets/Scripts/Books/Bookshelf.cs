using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using MapCreationTool.Serialization;
using UnityEngine.InputSystem;

public class Bookshelf : MonoBehaviour, IMapEditorDataReceiver {
   #region Public Variables

   // The book unique ID in the database
   public int bookId;

   #endregion

   private void Awake () {
      _outline = GetComponent<SpriteOutline>();
      _clickableBox = GetComponentInChildren<ClickableBox>();
   }

   private void Update () {
      if (bookId == 0 || PanelManager.self.get(Panel.Type.BookReader).isShowing()) {
         return;
      }

      _isMouseOver = MouseManager.self.isHoveringOver(_clickableBox);

      handleSpriteOutline();

      if (_isMouseOver && KeyUtils.isLeftButtonPressedUp()) {
         openBookReaderPanel();
      }
      
      // Allow pressing keyboard to open the crafting panel
      if (InputManager.isActionKeyPressed() && _isGlobalPlayerNearby) {
         openBookReaderPanel();
      }
   }

   private void openBookReaderPanel () { 
      // The player has to be close enough
      if (!_isGlobalPlayerNearby) {
         FloatingCanvas.instantiateAt(transform.position + new Vector3(0f, .24f)).asTooFar();
         return;
      }

      // Send a request to the server to show the book
      Global.player.rpc.Cmd_RetrieveBook(bookId);
   }

   private void handleSpriteOutline () {
      if (_outline == null) {
         return;
      }

      _outline.setVisibility(_isMouseOver || _isGlobalPlayerNearby);
   }
   private void OnTriggerStay2D (Collider2D other) {
      NetEntity entity = other.GetComponent<NetEntity>();

      // If our player enters the trigger, we show the GUI
      if (entity != null && Global.player != null && entity.userId == Global.player.userId) {
         _isGlobalPlayerNearby = true;
      }
   }

   private void OnTriggerExit2D (Collider2D other) {
      NetEntity entity = other.GetComponent<NetEntity>();

      // If our player exits the trigger, we hide the GUI
      if (entity != null && Global.player != null && entity.userId == Global.player.userId) {
         _isGlobalPlayerNearby = false;
      }
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.BOOK_ID_KEY) == 0) {
            if (field.tryGetIntValue(out int id)) {
               bookId = id;
            }
         }
      }
   }

   #region Private Variables

   // Is the mouse currently hovering over us?
   protected bool _isMouseOver = false;

   // Is the local player near this bookshelf?
   private bool _isGlobalPlayerNearby = false;

   // The book that belongs to this bookshelf
   private BookData _book;

   // Our components
   protected SpriteOutline _outline;
   protected ClickableBox _clickableBox;

   #endregion
}
