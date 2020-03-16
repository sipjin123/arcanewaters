using UnityEngine;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class NPCMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable
   {
      private SpriteSwap spriteSwapper;
      private SpriteOutline outline;

      private void Awake () {
         spriteSwapper = GetComponentInChildren<SpriteSwap>();
         outline = GetComponentInChildren<SpriteOutline>();
      }

      public void dataFieldChanged (string key, string value) {
         if (key.CompareTo(DataField.NPC_DATA_KEY) == 0) {
            if (int.TryParse(value, out int npcId)) {
               Texture2D npcTexture = NPCManager.instance.getTexture(npcId);

               if (npcTexture != null) {
                  spriteSwapper.newTexture = npcTexture;
               }
            }
         }
      }

      public override void createdForPreview () {
         setDefaultSprite();
      }

      public override void createdInPalette () {
         setDefaultSprite();
      }

      public void setDefaultSprite () {
         Texture2D npcTexture = NPCManager.instance.getFirstNpcTexture();

         if (npcTexture != null) {
            spriteSwapper.newTexture = npcTexture;
         }
      }

      public void setHighlight (bool hovered, bool selected, bool deleting) {
         setOutlineHighlight(outline, hovered, selected, deleting);
      }
   }
}
