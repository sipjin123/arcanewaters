using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class ChatLine : MonoBehaviour, IPointerClickHandler
{
   #region Public Variables

   // The Chat Info associated with this Chat Line
   public ChatInfo chatInfo;

   // The time at which this component was created
   public float creationTime;

   #endregion

   public void Awake () {
      creationTime = Time.time;
      _outline = GetComponentsInChildren<Outline>();
      _shadow = GetComponentsInChildren<Shadow>();
      setAppearanceWithBackground();
   }

   public void setAppearanceWithBackground () {
      if (_outline.Length > 0 && _outline[0].enabled) {
         foreach (Shadow shadow in _shadow) {
            shadow.enabled = true;
            shadow.effectDistance = new Vector2(2, -2);
         }
         foreach (Outline outline in _outline) {
            outline.enabled = false;
         }
      }
   }

   public void setAppearanceWithoutBackground () {
      if (_outline.Length > 0 && !_outline[0].enabled) {
         foreach (Shadow shadow in _shadow) {
            shadow.enabled = true;
            shadow.effectDistance = new Vector2(1, -1);
         }
         foreach (Outline outline in _outline) {
            outline.enabled = true;
         }
      }
   }

   public virtual void OnPointerClick (PointerEventData eventData) {
      if (chatInfo.senderId > 0 && eventData.button == PointerEventData.InputButton.Left) {
         PanelManager.self.contextMenuPanel.showDefaultMenuForUser(chatInfo.senderId, chatInfo.sender);
      }
   }

   #region Private Variables

   // The outline component
   private Outline[] _outline = new Outline[0];

   // The shadow component
   private Shadow[] _shadow = new Shadow[0];

   #endregion
}
