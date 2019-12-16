using UnityEngine;


namespace MapCreationTool
{
   public class NPCMapEditor : MonoBehaviour, IPrefabDataListener, IHighlightable
   {
      private SpriteSwapper spriteSwapper;
      private SpriteOutline outline;

      private void Awake () {
         spriteSwapper = GetComponentInChildren<SpriteSwapper>();
         outline = GetComponentInChildren<SpriteOutline>();
      }

      public void dataFieldChanged (string key, string value) {
         if (key.CompareTo("npc data") == 0) {
            int id = int.Parse(value.Split(':')[0]);

            Texture2D npcTexture = NPCManager.instance.getTexture(id);

            if (npcTexture != null) {
               spriteSwapper.newTexture = npcTexture;
            }
         } 
      }

      public void setHighlight (bool hovered, bool selected) {
         if (!hovered && !selected) {
            outline.setVisibility(false);
         } else if (hovered) {
            outline.setVisibility(true);
            outline.setNewColor(Color.white);
            outline.Regenerate();
         } else if (selected) {
            outline.setVisibility(true);
            outline.setNewColor(Color.green);
            outline.Regenerate();
         }
      }
   }

}
