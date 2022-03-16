using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class WorldMapPanelPinsContainer : MonoBehaviour
{
   #region Public Variables

   // Reference to the prefab used to create pins
   public GameObject pinPrefab;

   // The sprite used to represent town warps
   public Sprite townSprite;

   // The sprite used to represent unknown locations
   public Sprite unknownSprite;

   // The sprite used to represent ship-related locations
   public Sprite shipSprite;

   // The sprite used to represent discoveries
   public Sprite discoverySprite;

   // Filters pins by spot type
   public WorldMapSpot.SpotType[] filter;

   #endregion

   public void clearPins () {
      foreach (WorldMapPanelPin pin in _pins) {
         Destroy(pin.gameObject);
      }

      _pins.Clear();
   }

   public void addPins (IEnumerable<WorldMapSpot> spots) {
      foreach (WorldMapSpot spot in spots) {
         WorldMapPanelPin newPin = createPin(spot);
         positionPin(newPin);
         texturePin(newPin);
      }

      applyFilter();
   }

   private WorldMapPanelPin createPin (WorldMapSpot site) {
      GameObject o = Instantiate(pinPrefab);
      WorldMapPanelPin pin = o.GetComponent<WorldMapPanelPin>();
      pin.spot = site;
      _pins.Add(pin);
      return pin;
   }

   private void positionPin (WorldMapPanelPin pin) {
      // Add the pin to the container
      pin.transform.SetParent(transform);

      // Set position of the pin
      Vector2Int cellSize = WorldMapPanel.self.cellSize;
      Vector2Int mapDimensions = WorldMapPanel.self.mapDimensions;

      float computedX = pin.spot.worldX * cellSize.x + pin.spot.areaX / pin.spot.areaWidth * cellSize.x;
      float computedY = (mapDimensions.y - 1 - pin.spot.worldY) * cellSize.y - pin.spot.areaY / pin.spot.areaHeight * cellSize.y;

      pin.transform.localPosition = new Vector3(computedX, -computedY);
   }

   private void texturePin (WorldMapPanelPin pin) {
      // Default sprite
      pin.setSprite(unknownSprite);

      if (pin.spot.type == WorldMapSpot.SpotType.Warp) {
         if (pin.spot.specialType == (int) Area.SpecialType.Town) {
            pin.setSprite(townSprite);
         } else if (pin.spot.specialType == (int) Area.SpecialType.POI || pin.spot.specialType == (int) Area.SpecialType.TreasureSite) {
            pin.setSprite(pin.spot.discovered ? discoverySprite : unknownSprite);
         }
      } else if (pin.spot.type == WorldMapSpot.SpotType.League) {
         pin.setSprite(shipSprite);
      } else if (pin.spot.type == WorldMapSpot.SpotType.Discovery) {
         pin.setSprite(pin.spot.discovered ? discoverySprite : unknownSprite);
      }
   }

   public void applyFilter () {
      foreach (WorldMapPanelPin pin in _pins) {
         pin.toggle(filter.Contains(pin.spot.type));
      }
   }

   public List<WorldMapPanelPin> getPinsWithinArea (WorldMapPanelAreaCoords mapPanelAreaCoords) {
      List<WorldMapPanelPin> results = new List<WorldMapPanelPin>();

      foreach (WorldMapPanelPin pin in _pins) {
         WorldMapAreaCoords pinAreaCoords = new WorldMapAreaCoords(pin.spot.worldX, pin.spot.worldY);
         WorldMapPanelAreaCoords pinPanelAreaCoords = WorldMapPanel.self.transformCoords(pinAreaCoords);

         if (pinPanelAreaCoords == mapPanelAreaCoords) {
            results.Add(pin);
         }
      }

      return results;
   }

   public void highlightPin (WorldMapSpot spot, bool show) {
      WorldMapPanelPin pin = _pins.FirstOrDefault(_ => _.spot == spot);

      if (pin != null && pin.rect != null) {
         pin.rect.localScale += show ? Vector3.one * 0.5f : -Vector3.one * 0.5f;
      }
   }

   #region Private Variables

   // Pins Registry
   private List<WorldMapPanelPin> _pins = new List<WorldMapPanelPin>();

   #endregion
}
