using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class VoyageTriggerPopup : MonoBehaviour {
   #region Public Variables
      
   // Self
   public static VoyageTriggerPopup self;

   // The new voyage status GUI triggered when colliding with GenericActionTrigger
   public GameObject voyageStatusPopup;
   public Image voyageStatusBiomeImg;
   public Button voyageStatusConfirm, voyageStatusClose;

   #endregion

   private void Awake () {
      self = this;
      voyageStatusClose.onClick.AddListener(() => {
         voyageStatusPopup.SetActive(false);
      });
   }

   public void enableVoyageGUI (bool isEnable, Sprite spriteRef = null) {
      voyageStatusPopup.SetActive(isEnable);
      if (isEnable) {
         voyageStatusBiomeImg.sprite = spriteRef;
      }
   }

   #region Private Variables

   #endregion
}
