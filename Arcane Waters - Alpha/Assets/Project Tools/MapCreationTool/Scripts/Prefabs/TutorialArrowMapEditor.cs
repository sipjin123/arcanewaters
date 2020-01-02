using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

namespace MapCreationTool
{
   public class TutorialArrowMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable
   {
      private SpriteRenderer objSprite;
      private TutorialItem tutorialItem;

      private void Awake () {
         tutorialItem = GetComponent<TutorialItem>();
         tutorialItem.isSetInMapEditor = true;
         tutorialItem.transform.localScale = new Vector3(2, 2, 2);
         objSprite = GetComponentInChildren<SpriteRenderer>();
      }

      public void dataFieldChanged (string key, string value) {
         if (key.CompareTo("tutorial data") == 0) {
         }
      }

      public override void createdForPrieview () {
         setDefaultSprite();
      }

      public void setDefaultSprite () {
      }

      public void setHighlight (bool hovered, bool selected) {

      }
   }
}
