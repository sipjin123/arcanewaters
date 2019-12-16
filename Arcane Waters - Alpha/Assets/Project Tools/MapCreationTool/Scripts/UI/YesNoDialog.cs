using System;
using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class YesNoDialog : MonoBehaviour
   {
      private CanvasGroup cGroup;

      private Action onYes;
      private Action onNo;

      [SerializeField]
      private Text titleText = null;
      [SerializeField]
      private Text contentText = null;

      private void Awake () {
         cGroup = GetComponent<CanvasGroup>();
         hide();
      }

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

      private void hide () {
         cGroup.alpha = 0;
         cGroup.blocksRaycasts = false;
         cGroup.interactable = false;
      }

      private void show () {
         cGroup.alpha = 1;
         cGroup.blocksRaycasts = true;
         cGroup.interactable = true;
      }
   }
}