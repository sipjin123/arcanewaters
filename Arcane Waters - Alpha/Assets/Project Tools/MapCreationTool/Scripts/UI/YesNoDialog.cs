﻿using System;
using MapCreationTool.UndoSystem;
using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class YesNoDialog : UIPanel
   {
      private Action onYes;
      private Action onNo;

      [SerializeField]
      private Text titleText = null;
      [SerializeField]
      private Text contentText = null;

      public void display (string title, string content, Action onYes, Action onNo, Color titleColor) {
         this.onYes = onYes;
         this.onNo = onNo;

         titleText.text = title.ToUpper();
         titleText.color = titleColor;
         contentText.text = content;

         show();
      }

      public void display (string title, string content, Action onYes, Action onNo) {
         display(title, content, onYes, onNo, UI.messagePanel.infoColor);
      }

      public void displayIfMapStateModified (string title, string content, Action onYes, Action onNo) {
         if (Undo.anyModificationUndoEntries()) {
            display(title, content, onYes, onNo, UI.messagePanel.warningColor);
         } else {
            onYes?.Invoke();
         }
      }

      public void yesButton_Click () {
         hide();
         onYes?.Invoke();
      }

      public void noButton_Click () {
         hide();
         onNo?.Invoke();
      }
   }
}