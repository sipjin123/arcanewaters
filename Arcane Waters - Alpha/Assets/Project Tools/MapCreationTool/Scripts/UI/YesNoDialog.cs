using System;
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

      public void display (string title, string content, Action onYes, Action onNo) {
         this.onYes = onYes;
         this.onNo = onNo;

         titleText.text = title;
         contentText.text = content;

         show();
      }

      public void yesButton_Click () {
         onYes?.Invoke();
         hide();
      }

      public void noButton_Click () {
         onNo?.Invoke();
         hide();
      }
   }
}