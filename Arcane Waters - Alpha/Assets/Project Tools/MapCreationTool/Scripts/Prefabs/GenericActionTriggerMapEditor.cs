using MapCreationTool.Serialization;
using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class GenericActionTriggerMapEditor : MapEditorPrefab, IPrefabDataListener
   {
      [SerializeField]
      private RectTransform boundsRect = null;
      [SerializeField]
      private Text text = null;

      public GenericActionTrigger.InteractionType interactionType { get; private set; }
      public string actionName { get; private set; }

      private float width = 1f;
      private float height = 1f;

      public void dataFieldChanged (DataField field) {
         if (field.k.CompareTo(DataField.GENERIC_ACTION_TRIGGER_WIDTH_KEY) == 0) {
            if (field.tryGetFloatValue(out float w)) {
               width = Mathf.Clamp(w, 0.1f, 100);
               updateBoundsSize();
            }
         } else if (field.k.CompareTo(DataField.GENERIC_ACTION_TRIGGER_HEIGHT_KEY) == 0) {
            if (field.tryGetFloatValue(out float h)) {
               height = Mathf.Clamp(h, 0.1f, 100);
               updateBoundsSize();
            }
         } else if (field.k.CompareTo(DataField.GENERIC_ACTION_TRIGGER_INTERACTION_TYPE) == 0) {
            if (field.tryGetInteractionTypeValue(out GenericActionTrigger.InteractionType type)) {
               interactionType = type;
            }
            updateText();
         } else if (field.k.CompareTo(DataField.GENERIC_ACTION_TRIGGER_ACTION_NAME) == 0) {
            actionName = field.v;
            updateText();
         }
      }

      public override void createdInPalette () {
         boundsRect.gameObject.SetActive(false);
      }

      public override void createdForPreview () {
         boundsRect.gameObject.SetActive(false);
      }

      public override void placedInEditor () {
         boundsRect.gameObject.SetActive(false);
      }

      public override void setHovered (bool hovered) {
         base.setHovered(hovered);
         boundsRect.gameObject.SetActive(hovered || selected);
      }

      private void updateBoundsSize () {
         boundsRect.sizeDelta = new Vector2(width * 100, height * 100);
      }

      private void updateText () {
         text.text = $"on { interactionType.ToString() }\n{ actionName }";
      }
   }
}
