using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

public class UserItemTemplate : ItemCell, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{

   #region Public Variables

   // Reference to the user auction panel
   public AuctionUserPanel userAuctionPanel;

   // The canvas group component
   public CanvasGroup canvasGroup;

   // Object indicating this template is selected
   public GameObject highlightObj;

   #endregion

   public void setObjHighlight (bool isOn) {
      highlightObj.SetActive(isOn);
   }

   public void OnPointerEnter (PointerEventData eventData) {
   }

   public void OnPointerExit (PointerEventData eventData) {
   }

   public void setPanel (AuctionUserPanel panel) {
      userAuctionPanel = panel;
   }

   public void OnPointerDown (PointerEventData eventData) {
      if (_interactable) {
         if (eventData.button == PointerEventData.InputButton.Left) {
            userAuctionPanel.selectItem(this);
         }
      }
   }

   public void show () {
      canvasGroup.alpha = 1f;
   }

   public void hide () {
      canvasGroup.alpha = 0f;
   }

   #region Private Variables

   // The distance the mouse must move for the dragging process to begin (squared distance)
   private float DISTANCE_UNTIL_START_DRAG = 0.05f * 0.05f;

   #endregion
}