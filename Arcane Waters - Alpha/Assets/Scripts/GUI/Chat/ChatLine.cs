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
   }

   public virtual void OnPointerClick (PointerEventData eventData) {
      if (chatInfo.senderId > 0 && eventData.button == PointerEventData.InputButton.Left) {
         PanelManager.self.contextMenuPanel.showDefaultMenuForUser(chatInfo.senderId, chatInfo.sender);
      }
   }

   #region Private Variables

   #endregion
}
