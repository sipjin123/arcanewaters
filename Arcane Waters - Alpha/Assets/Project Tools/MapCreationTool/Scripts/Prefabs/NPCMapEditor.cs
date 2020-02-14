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
            Texture2D npcTexture = NPCManager.instance.getTexture(int.Parse(value.Split(':')[0]));

            if (npcTexture != null) {
               spriteSwapper.newTexture = npcTexture;
            }
         }
      }

      public override void createdForPrieview () {
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

      public void setHighlight (bool hovered, bool selected) {
         setOutlineHighlight(outline, hovered, selected);
      }
   }
}
