using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Text;
using UnityEngine.Events;

public class ContextMenuPanel : MonoBehaviour
{
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // The prefab we use for creating buttons
   public ContextMenuButton buttonPrefab;

   // The container for the buttons
   public GameObject buttonContainer;

   // The rect transform of the button zone
   public RectTransform buttonsRectTransform;

   // The title of the context menu (often the user name)
   public Text titleText;

   #endregion

   public void show(string title) {
      show(title, Input.mousePosition);
   }

   public void show (string title, Vector3 position) {
      this.gameObject.SetActive(true);
      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;

      transform.position = position;

      // Set the title
      if (string.IsNullOrEmpty(title)) {
         titleText.transform.parent.gameObject.SetActive(false);
      } else {
         titleText.transform.parent.gameObject.SetActive(true);
         titleText.text = title;
      }
   }

   public void hide () {
      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
      this.gameObject.SetActive(false);
   }

   public bool isShowing () {
      return gameObject.activeSelf;
   }

   public void Update () {
      if (Global.player == null) {
         hide();
      }

      if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) {
         // Hide the menu if a mouse button is clicked and the pointer is not over any button
         if (!RectTransformUtility.RectangleContainsScreenPoint(buttonsRectTransform, Input.mousePosition)) {
            hide();
         }
      } else if (Input.anyKeyDown) {
         hide();
      }
   }

   public void clearButtons () {
      buttonContainer.DestroyChildren();
   }

   public void addButton (string text, UnityAction action) {
      ContextMenuButton button = Instantiate(buttonPrefab, buttonContainer.transform, false);
      button.initForAction(text, action);
   }

   #region Private Variables

   #endregion
}