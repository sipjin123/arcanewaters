using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class Bookshelf : MonoBehaviour {
   #region Public Variables

   // The book data
   public BookData book;

   #endregion

   private void Awake () {
      _outline = GetComponent<SpriteOutlineHelper>();
      _clickableBox = GetComponentInChildren<ClickableBox>();
   }

   private void Update () {
      _isMouseOver = MouseManager.self.isHoveringOver(_clickableBox);

      handleSpriteOutline();

      if (_isMouseOver && Input.GetMouseButtonUp(0)) {
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
         Instantiate(PrefabsManager.self.tooFarPrefab, this.transform.position + new Vector3(0f, .24f), Quaternion.identity);
         return;
      }

      // Get the book reader panel
      PanelManager.self.selectedPanel = Panel.Type.BookReader;
      BookReaderPanel bookPanel = (BookReaderPanel) PanelManager.self.get(Panel.Type.BookReader);

      // Show this book
      bookPanel.show(book);
   }

   private void handleSpriteOutline () {
      if (_outline == null) {
         return;
      }

      _outline.setVisibility(_isMouseOver);
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


   #region Private Variables

   // Is the mouse currently hovering over us?
   protected bool _isMouseOver = false;

   // Is the local player near this bookshelf?
   private bool _isGlobalPlayerNearby = false;

   // Our components
   protected SpriteOutlineHelper _outline;
   protected ClickableBox _clickableBox;

   #endregion
}
