using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class TutorialArrowMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable
   {
      [SerializeField]
      private SpriteRenderer highlight = null;

      private TutorialItem tutorialItem;
      private Text text;

      private void Awake () {
         tutorialItem = GetComponent<TutorialItem>();
         tutorialItem.isSetInMapEditor = true;
         tutorialItem.transform.localScale = new Vector3(2, 2, 2);
         text = GetComponentInChildren<Text>();
         text.text = "Step ID: -";
      }

      public void dataFieldChanged (DataField field) {
         if (field.k.CompareTo(DataField.TUTORIAL_ITEM_STEP_ID_KEY) == 0) {
            text.text = "Step ID: " + field.v;
         }
      }

      public override void createdForPreview () {
         setDefaultSprite();
      }

      public void setDefaultSprite () {
      }

      public void setHighlight (bool hovered, bool selected, bool deleting) {
         setSpriteOutline(highlight, hovered, selected, deleting);
      }
   }
}
