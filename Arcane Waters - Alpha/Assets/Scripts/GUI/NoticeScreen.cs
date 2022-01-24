using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using UnityEngine.EventSystems;

public class NoticeScreen : FullScreenSeparatePanel {
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // Our various components that we need references to
   public TextMeshProUGUI text;
   public Button confirmButton;

   #endregion

   public void show (string newText) {
      text.SetText(newText);

      // Now make us visible
      show();
   }

   public void show () {
      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;
      this.gameObject.SetActive(true);
   }

   public void hide () {
      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
      this.gameObject.SetActive(false);
   }

   public void disableButtons () {
      canvasGroup.interactable = false;
   }

   public bool isActive { get { return canvasGroup.alpha != 0; } }

   private void Update () {
      if (InputManager.self.inputMaster.UIControl.Close.WasPressedThisFrame()) {
         hide();
      }
   }

   #region Private Variables

   #endregion
}
