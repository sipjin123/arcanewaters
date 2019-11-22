using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Tilemaps;

public class AttackRangeCircle : MonoBehaviour
{
   #region Public Variables
   
   // The maximum distance between two dots in the circle
   public static float MAX_ARC_LENGTH = 0.15f;

   // The rotation speed
   public static float ROTATION_SPEED = 1f;

   // The prefab we use to create dots
   public AttackRangeDot dotPrefab;

   // The container for the dots
   public GameObject dotContainer;

   #endregion

   public void draw (float radius) {
      // Refresh the land tile maps if necessary
      updateCurrentArea();

      // Calculate the circumference of the circle
      float circumference = 2 * Mathf.PI * radius;

      // Calculate the number of dots needed
      int dotCount = Mathf.CeilToInt(circumference / MAX_ARC_LENGTH);

      // Calculate the angle between each dot
      float angleStep = 360f / dotCount;

      // Destroy any existing dot
      dotContainer.DestroyChildren();

      // Clear the dot list
      _dots.Clear();

      // Draw the circle
      float angle = 0f;
      for (int i = 0; i < dotCount; i++) {
         // Instantiate a new dot
         AttackRangeDot dot = Instantiate(dotPrefab, dotContainer.transform);

         // Set the dot position
         dot.setPosition(this, angle, radius);

         // Add the dot to the list
         _dots.Add(dot);

         // Increase the angle
         angle += angleStep;
      }
   }

   public void Update () {
      // Slowly rotate while active
      transform.Rotate(Vector3.forward, ROTATION_SPEED * Time.deltaTime);

      // Refresh the land tile maps if necessary
      updateCurrentArea();
   }

   public void show () {
      if (!gameObject.activeSelf) {
         gameObject.SetActive(true);
         foreach (AttackRangeDot dot in _dots) {
            dot.show();
         }
      }
   }

   public void hide () {
      if (gameObject.activeSelf) {
         foreach (AttackRangeDot dot in _dots) {
            dot.hide();
         }
         gameObject.SetActive(false);
      }
   }

   public bool isOverLandTile (Vector2 pos) {
      // Get the cell for the given world position
      Vector3Int cellPos = _currentGrid.WorldToCell(pos);

      // Check if any of the land maps has a tile in that cell
      foreach (Tilemap tilemap in _landTilemaps) {
         TileBase tile = tilemap.GetTile(cellPos);
         if (tile != null) {
            return true;
         }
      }
      return false;
   }

   private void updateCurrentArea () {
      if (AreaManager.self != null && Global.player != null) {
         // Verify if the area has changed
         Area area = AreaManager.self.getArea(Global.player.areaKey);
         if (area != _currentArea) {
            _currentArea = area;
            _currentGrid = area.GetComponentInChildren<Grid>();

            // Get the land tiles of this area
            _landTilemaps.Clear();
            foreach (Tilemap tilemap in _currentArea.GetComponentsInChildren<Tilemap>()) {
               if (tilemap.name.StartsWith("Land")) {
                  _landTilemaps.Add(tilemap);
               }
            }
         }
      }
   }

   #region Private Variables

   // A reference to all the dots
   private List<AttackRangeDot> _dots = new List<AttackRangeDot>();

   // A reference to the current area
   private Area _currentArea;

   // A reference to the current terrain grid
   private Grid _currentGrid;

   // A reference to all the land tiles of the current area
   private List<Tilemap> _landTilemaps = new List<Tilemap>();

   #endregion
}