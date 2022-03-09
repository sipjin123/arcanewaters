using UnityEngine;
using System.Collections.Generic;

public class WorldMapPanelPinsContainer : MonoBehaviour
{
   #region Public Variables

   // Reference to the prefab used to create indicators
   public GameObject indicatorPrefab;

   // The sprite used to represent town warps
   public Sprite townSprite;

   // The sprite used to represent unknown locations
   public Sprite unknownSprite;

   // The sprite used to represent ship-related locations
   public Sprite shipSprite;

   // The sprite used to represent discoveries
   public Sprite discoverySprite;

   // The pin filter
   public WorldMapPanelPin.PinTypes[] filter;

   #endregion

   public void clearPins () {
      foreach (WorldMapPanelPin pin in _pins) {
         Destroy(pin.gameObject);
      }

      _pins.Clear();
   }

   public void addPins(IEnumerable<WorldMapPanelPinInfo> pinInfos) {
      foreach (WorldMapPanelPinInfo pinInfo in pinInfos) {
         WorldMapPanelPin newPin = createPin(pinInfo);
         positionPin(newPin);
         texturePin(newPin);
      }

      applyFilter();
   }

   private WorldMapPanelPin createPin (WorldMapPanelPinInfo pinInfo) {
      GameObject o = Instantiate(indicatorPrefab);
      WorldMapPanelPin pin = o.GetComponent<WorldMapPanelPin>();
      pin.info = pinInfo;
      _pins.Add(pin);
      return pin;
   }

   private void positionPin(WorldMapPanelPin pin) {
      // Add the pin to the container
      pin.transform.SetParent(transform);

      // Set position of the pin
      Vector2Int cellSize = WorldMapPanel.self.cellSize;
      Vector2Int mapDimensions = WorldMapPanel.self.mapDimensions;

      float computedX = pin.info.areaX * cellSize.x + pin.info.x / pin.info.areaWidth * cellSize.x;
      float computedY = (mapDimensions.y - 1 - pin.info.areaY) * cellSize.y - pin.info.y / pin.info.areaHeight * cellSize.y;

      pin.transform.localPosition = new Vector3(computedX, -computedY);
   }

   private void texturePin(WorldMapPanelPin pin) {
      // Default sprite
      pin.setSprite(unknownSprite);

      if (pin.info.pinType == WorldMapPanelPin.PinTypes.Warp) {
         if (pin.info.specialType == (int) Area.SpecialType.Town) {
            pin.setSprite(townSprite);
         } else if (pin.info.specialType == (int) Area.SpecialType.POI || pin.info.specialType == (int) Area.SpecialType.TreasureSite) {
            pin.setSprite(pin.info.discovered ? discoverySprite : unknownSprite);
         }
      } else if (pin.info.pinType == WorldMapPanelPin.PinTypes.League) {
         pin.setSprite(shipSprite);
      } else if (pin.info.pinType == WorldMapPanelPin.PinTypes.Discovery) {
         pin.setSprite(pin.info.discovered ? discoverySprite : unknownSprite);
      }
   }

   public void applyFilter () {
      foreach (WorldMapPanelPin pin in _pins) {
         pin.toggle(filter.Contains(pin.info.pinType));
      }
   }

   public List<WorldMapPanelPin> getPinsWithinArea(Vector2Int areaCoords) {
      List<WorldMapPanelPin> results = new List<WorldMapPanelPin>();

      foreach (WorldMapPanelPin pin in _pins) {
         if (pin.info.areaX == areaCoords.x && (WorldMapPanel.self.mapDimensions.y - 1 - pin.info.areaY) == areaCoords.y) {
            results.Add(pin);
         }
      }

      return results;
   }

   #region Private Variables

   // Reference to the registry of the pins
   private List<WorldMapPanelPin> _pins = new List<WorldMapPanelPin>();

   #endregion
}
