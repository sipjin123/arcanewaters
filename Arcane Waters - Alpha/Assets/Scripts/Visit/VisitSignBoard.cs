using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using TMPro;
using UnityEngine.EventSystems;

public class VisitSignBoard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
   #region Public Variables

   // The tooltip display
   public GameObject visitLabel;

   #endregion

   private void Awake () {
      _outline = GetComponentInChildren<SpriteOutline>();
   }

   private void Start () {
      _outline.setVisibility(false);
   }

   public void triggerVisitPanel () {
      BottomBar.self.toggleFriendVisitPanel();
   }

   public void OnPointerEnter (PointerEventData eventData) {
      visitLabel.SetActive(true);
      _outline.setVisibility(true);
   }

   public void OnPointerExit (PointerEventData eventData) {
      visitLabel.SetActive(false);
      _outline.setVisibility(false);
   }

   #region Private Variables

   // Our various components
   protected SpriteOutline _outline;

   #endregion
}
