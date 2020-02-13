using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using ProceduralMap;
using Mirror;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class CharacterPortrait : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
   #region Public Variables

   // The character stack
   public CharacterStack characterStack;

   // The icon displayed when the portrait info is not available
   public GameObject unknownIcon;

   // The tooltip
   public Tooltipped tooltip;

   // The click events
   public UnityEvent pointerEnterEvent;
   public UnityEvent pointerExitEvent;

   #endregion

   public void Awake () {
      characterStack.pauseAnimation();
   }

   public void setPortrait (NetEntity entity) {
      // If the entity is null, display a question mark
      if (entity == null) {
         unknownIcon.SetActive(true);
         return;
      }
      
      // Update the character stack
      characterStack.updateLayers(entity);
      characterStack.setDirection(Direction.East);

      // Set the user name in the tooltip
      tooltip.text = entity.entityName;

      // Hide the question mark icon
      unknownIcon.SetActive(false);
   }

   public void disableMouseInteraction () {
      pointerEnterEvent.RemoveAllListeners();
      pointerExitEvent.RemoveAllListeners();
      Destroy(tooltip);
   }

   public void OnPointerExit (PointerEventData eventData) {
      pointerEnterEvent.Invoke();
   }

   public void OnPointerEnter (PointerEventData eventData) {
      pointerExitEvent.Invoke();
   }

   #region Private Variables

   #endregion
}
