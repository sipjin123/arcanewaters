using UnityEngine;
using UnityEngine.UI;
using System;

namespace MapCreationTool
{
   public class AdditionalInputPanel : UIPanel
   {
      #region Public Variables

      // The text that's provided in string input field
      public string string1Input => _stringInputField1.text;

      #endregion

      public void open (string titleText, string stringInput1Description, Action onConfirm, Action onCancel) {
         _onConfirm = onConfirm;
         _onCancel = onCancel;

         _titleText.text = titleText.ToUpper();
         _stringInput1Description.text = stringInput1Description;

         // Reset inputs
         _stringInputField1.text = "";

         show();
      }

      public void confirmButton_Click () {
         hide();
         _onConfirm?.Invoke();
      }

      public void cancelButton_Click () {
         hide();
         _onCancel?.Invoke();
      }

      public void close () {
         hide();
      }

      #region Private Variables

      // Events for user actions
      private Action _onConfirm = null;
      private Action _onCancel = null;

      // Text we use in the title bar of the panel
      [SerializeField]
      private Text _titleText = null;

      // Text we use to describe the purpose of the first string input
      [SerializeField]
      private Text _stringInput1Description = null;

      // The first string input
      [SerializeField]
      private InputField _stringInputField1 = null;

      #endregion
   }

}
