using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Tilemaps;

public class Minimap : ClientMonoBehaviour {
   #region Public Variables

   // The distance scale the minimap is using
   public static float SCALE = .10f;

   // The prefab we use for creating NPC icons
   public MM_Icon npcIconPrefab;

   // The prefab we use for creating Building icons
   public MM_Icon buildingIconPrefab;

   // The prefab we use for showing Tutorial objectives
   public MM_TutorialIcon tutorialIconPrefab;

   // The prefab we use for showing impassable areas
   public MM_Icon impassableIconPrefab;

   // The icon to use for NPCs
   public Sprite npcIcon;

   // The Container for the icons we create
   public GameObject iconContainer;

   // Image we're using for the map background
   public Image backgroundImage;

   // Self
   public static Minimap self;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;
   }

   protected void Start () {
      // Look up components
      _rect = GetComponent<RectTransform>();
      _canvasGroup = GetComponent<CanvasGroup>();
   }

   void Update () {
      // Hide the minimap if there's no player
      _canvasGroup.alpha = (Global.player == null || Global.isInBattle()) ? 0f : 1f;

      // Don't do anything else if there's no player set right now
      if (Global.player == null) {
         return;
      }

      // If our area changes, update the markers we've created
      if (Global.player.areaType != _previousAreaType) {
         updateMinimapForNewArea();
      }
   }

   public float getMaxDistance () {
      return _rect.sizeDelta.x / 2f * SCALE;
   }

   protected void updateMinimapForNewArea () {
      Area area = AreaManager.self.getArea(Global.player.areaType);

      // For random sea map - create new minimap
      if (Area.isRandom(Global.player.areaType)) {
         CreateSeaRandomMinimap(Global.player.areaType);
      } else {
         // Change the background image
         backgroundImage.sprite = ImageManager.getSprite("Minimaps/" + area.areaType);

         // If we didn't find a background image, just use a black background
         if (backgroundImage.sprite == null) {
            backgroundImage.sprite = ImageManager.getSprite("Minimaps/Black");
         }
      }

      // Delete any old markers we created
      iconContainer.DestroyChildren();

      // Create icons for any impassable areas
      /*for (float y = area.cameraBounds.bounds.min.y; y < area.cameraBounds.bounds.max.y; y += (4f * SCALE)) {
         for (float x = area.cameraBounds.bounds.min.x; x < area.cameraBounds.bounds.max.x; x += (4f * SCALE)) {
            bool impassable = false;
            Vector2 pos = new Vector2(x, y);
            
            foreach (Collider2D hit in Physics2D.OverlapAreaAll(pos, pos + new Vector2(2f*SCALE, 2f*SCALE))) {
               if (!hit.isTrigger) {
                  impassable = true;
               }
            }
            
            if (impassable) {
               MM_Icon icon = Instantiate(impassableIconPrefab, this.iconContainer.transform);
               icon.transform.position = pos;
               icon.targetPosition = pos;
            }
         }
      }*/

      // Create new icons for all NPCs
      foreach (NPC npc in area.GetComponentsInChildren<NPC>()) {
         MM_Icon icon = Instantiate(npcIconPrefab, this.iconContainer.transform);
         icon.target = npc.gameObject;
      }

      // Create new icons for all buildings
      List<string> buildings = new List<string>() { "Shipyard", "Merchant", "Weapons" };
      foreach (Spawn spawn in area.GetComponentsInChildren<Spawn>()) {
         foreach (string building in buildings) {
            if (spawn.spawnType.ToString().Contains(building)) {
               MM_Icon icon = Instantiate(buildingIconPrefab, this.iconContainer.transform);
               icon.target = spawn.gameObject;
               icon.tooltip.text = building;
            }
         }
      }

      // Create icons for all tutorial objectives
      foreach (TutorialLocation loc in area.GetComponentsInChildren<TutorialLocation>()) {
         MM_TutorialIcon icon = Instantiate(tutorialIconPrefab, this.iconContainer.transform);
         icon.tutorialStepType = loc.tutorialStepType;
         icon.target = loc.gameObject;
      }

      // Note the new area type
      _previousAreaType = Global.player.areaType;
   }

   private void CreateSeaRandomMinimap (Area.Type areaType) {
      Area area = AreaManager.self.getArea(areaType);
      if (area == null) {
         D.error("Area not found - minimap creation");
         return;
      }

      // Create a blank texture we can write to
      Texture2D map = new Texture2D(64, 64);

      foreach (Tilemap tilemap in area.GetComponentsInChildren<Tilemap>()) {
         // Cycle over all the Tile positions in this Tilemap layer
         for (int y = 0; y < 64; y++) {
            for (int x = 0; x < 64; x++) {
               // Check which Tile is at the cell position
               Vector3Int cellPos = new Vector3Int(x, y, 0);
               TileBase tile = tilemap.GetTile(cellPos);

               if (tile != null) {
                  // Depending on which layer the tile is in, color the minimap differently
                  if (tilemap.name.EndsWith("Base Ground Layer")) {
                     // map.SetPixel(x, 64 - y, Color.grey);
                  } else if (tilemap.name.Contains("Mountains")) {
                     Color brown = new Color(114f / 255f, 74f / 255f, 10f / 255f);
                     map.SetPixel(x, y, brown);
                  } else if (tilemap.name.Contains("Water")) {
                     map.SetPixel(x, y, Color.blue);
                  } else if (tilemap.name.Contains("Shrubs")) {
                     map.SetPixel(x, y, Color.green);
                  } else if (tilemap.name.Contains("Stairs") || tilemap.name.Contains("Bridge")) {
                     map.SetPixel(x, y, Color.black);
                  } else if (tilemap.name.Contains("Land")) {
                     map.SetPixel(x, y, Color.green);
                  }
               }
            }
         }
      }
      // Apply the texture changes
      map.Apply();
      _seaRandomSprite = Sprite.Create(map, new Rect(0, 0, 64, 64), new Vector2(0.0f, 0.0f));
      backgroundImage.sprite = _seaRandomSprite;
   }


   #region Private Variables

   // Create random sea map sprite
   private Sprite _seaRandomSprite;

   // Our Rect Transform
   protected RectTransform _rect;

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   // The previous area we were in
   protected Area.Type _previousAreaType;

   #endregion
}
