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

   // Cached simple animation
   public SimpleAnimation simpleAnimCache;

   #endregion

   public void OnPointerEnter (PointerEventData eventData) {
      if (simpleAnimCache == null) {
         simpleAnimCache = previewImage.GetComponent<SimpleAnimation>();
      }

      if (simpleAnimCache != null) {
         simpleAnimCache.updateIndexMinMax(0, 1000);
      }

      previewImage.sprite = spriteImage.sprite;
   }
}
