using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class HighlightItemTemplate : MonoBehaviour, IPointerEnterHandler
{
   #region Public Variables

   // The image references
   public Image previewImage;
   public Image spriteImage;

   #endregion

   public void OnPointerEnter (PointerEventData eventData) {
      previewImage.sprite = spriteImage.sprite;
   }
}
