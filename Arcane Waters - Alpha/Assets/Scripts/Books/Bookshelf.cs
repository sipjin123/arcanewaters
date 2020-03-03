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
      _outline = GetComponent<SpriteOutline>();
      _clickableBox = GetComponent<ClickableBox>();
   }

   private void Update () {
      _isMouseOver = MouseManager.self.isHoveringOver(_clickableBox);

      handleSpriteOutline();

      if (_isMouseOver && Input.GetMouseButtonUp(0)) {
         // Get the book reader panel
         PanelManager.self.selectedPanel = Panel.Type.BookReader;
         BookReaderPanel bookPanel = (BookReaderPanel) PanelManager.self.get(Panel.Type.BookReader);
         bookPanel.show(book);
      }
   }

   private void handleSpriteOutline () {
      if (_outline == null) {
         return;
      }

      _outline.setVisibility(_isMouseOver);
   }

   #region Private Variables

   // Is the mouse currently hovering over us?
   protected bool _isMouseOver = false;

   // Our components
   protected SpriteOutline _outline;
   protected ClickableBox _clickableBox;

   #endregion
}
