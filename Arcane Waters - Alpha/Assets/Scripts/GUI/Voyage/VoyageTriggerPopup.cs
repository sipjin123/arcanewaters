using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class VoyageTriggerPopup : MonoBehaviour {
   #region Public Variables
      
   // Self
   public static VoyageTriggerPopup self;

   // The new voyage status GUI triggered when colliding with GenericActionTrigger
   public GameObject voyageStatusPopup;
   public Image voyageStatusBiomeImg;
   public Button voyageStatusConfirm, voyageStatusClose;

   // Warning related UI
   public GameObject warningPanel;
   public TextMeshProUGUI warningMessage;
   public Button closeWarningButton;

   #endregion

   private void Awake () {
      self = this;
      voyageStatusClose.onClick.AddListener(() => {
         voyageStatusPopup.SetActive(false);
      });
      closeWarningButton.onClick.AddListener(() => {
         warningPanel.SetActive(false);
      });
   }

   public void disableAllPanels () {
      voyageStatusPopup.SetActive(false);
      warningPanel.SetActive(false);
   }

   public void toggleWarningPanel (string message) {
      warningMessage.text = message;
      warningPanel.SetActive(true);
   }

   public void enableVoyageGUI (bool isEnable, Sprite spriteRef = null) {
      voyageStatusPopup.SetActive(isEnable);
      if (isEnable) {
         ReturnToCurrentVoyagePanel panel = (ReturnToCurrentVoyagePanel) PanelManager.self.get(Panel.Type.ReturnToCurrentVoyagePanel);

         // Make sure the panel is not showing, otherwise override state
         if (panel != null && panel.isShowing()) {
            voyageStatusPopup.SetActive(false);
         } else {
            voyageStatusBiomeImg.sprite = spriteRef;
         }
      }
   }

   #region Private Variables

   #endregion
}
