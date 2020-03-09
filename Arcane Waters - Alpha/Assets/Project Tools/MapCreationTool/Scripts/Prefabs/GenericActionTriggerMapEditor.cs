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

      public void dataFieldChanged (string key, string value) {
         if (key.CompareTo(DataField.GENERIC_ACTION_TRIGGER_WIDTH_KEY) == 0) {
            width = value.CompareTo(string.Empty) == 0 ? 1 : Mathf.Clamp(float.Parse(value), 1, 100);
            updateBoundsSize();
         } else if (key.CompareTo(DataField.GENERIC_ACTION_TRIGGER_HEIGHT_KEY) == 0) {
            height = value.CompareTo(string.Empty) == 0 ? 1 : Mathf.Clamp(float.Parse(value), 1, 100);
            updateBoundsSize();
         } else if (key.CompareTo(DataField.GENERIC_ACTION_TRIGGER_INTERACTION_TYPE) == 0) {
            interactionType = MapImporter.parseInteractionType(value);
            updateText();
         } else if (key.CompareTo(DataField.GENERIC_ACTION_TRIGGER_ACTION_NAME) == 0) {
            actionName = value;
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
