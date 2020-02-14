using UnityEngine;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class EnemyMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable
   {
      [SerializeField]
      private bool isSea = false;

      private SpriteRenderer ren;
      private SpriteOutline outline;

      private void Awake () {
         ren = GetComponentInChildren<SpriteRenderer>();
         outline = GetComponentInChildren<SpriteOutline>();
      }

      public void dataFieldChanged (string key, string value) {
         if (key.CompareTo(isSea ? DataField.SEA_ENEMY_DATA_KEY : DataField.LAND_ENEMY_DATA_KEY) == 0) {
            Texture2D texture = isSea
               ? MonsterManager.instance.getSeaMonsterTexture(int.Parse(value.Split(':')[0]))
               : MonsterManager.instance.getLandMonsterTexture(int.Parse(value.Split(':')[0]));

            if (texture != null) {
               ren.sprite = ImageManager.getSprites(texture)[0];
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
         Texture2D texture = isSea
            ? MonsterManager.instance.getFirstSeaMonsterTexture()
            : MonsterManager.instance.getFirstLandMonsterTexture();

         if (texture != null) {
            ren.sprite = ImageManager.getSprites(texture)[0];
         }
      }

      public void setHighlight (bool hovered, bool selected) {
         setOutlineHighlight(outline, hovered, selected);
      }
   }
}
