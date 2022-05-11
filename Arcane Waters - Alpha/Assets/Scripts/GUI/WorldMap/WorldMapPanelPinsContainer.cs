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

   // Prevent pins from overlapping?
   public bool avoidOverlappingPins;

   // Color of the label for the current player
   public Color defaultLabelColor = Color.white;

   // Color of the label for the current player
   public Color currentPlayerLabelColor = Color.red;

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
         nudgePin(newPin);
         updatePinLabel(newPin);
         positionPinLabel(newPin);
         newPin.setTooltip(spot.displayName);
      }

      applyFilter();
   }

   private WorldMapPanelPin createPin (WorldMapSpot spot) {
      GameObject o = Instantiate(pinPrefab);
      WorldMapPanelPin pin = o.GetComponent<WorldMapPanelPin>();
      pin.gameObject.name = $"World Map Panel Pin {spot.displayName}";
      pin.spot = spot;
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

   private void nudgePin (WorldMapPanelPin pin) {
      if (!avoidOverlappingPins) {
         return;
      }

      // Check if the pin is overlapping any of the pins already placed
      foreach (WorldMapPanelPin p in _pins) {
         if (p == pin) {
            continue;
         }

         for (int attempt = 0; attempt < 10; attempt++) {
            if (!arePinsOverlapping(pin, p, 10)) {
               break;
            }

            Util.setLocalX(pin.transform, pin.transform.localPosition.x - 10.0f);
            attempt++;
         }
      }
   }

   private void texturePin (WorldMapPanelPin pin) {
      // Default sprite
      pin.setSprite(unknownSprite);

      switch (pin.spot.type) {
         case WorldMapSpot.SpotType.Warp:
            if (pin.spot.specialType == (int) Area.SpecialType.Town) {
               pin.setSprite(townSprite);

            } else if (pin.spot.specialType == (int) Area.SpecialType.POI || pin.spot.specialType == (int) Area.SpecialType.TreasureSite) {
               pin.setSprite(pin.spot.discovered ? discoverySprite : unknownSprite);
            }

            break;
         case WorldMapSpot.SpotType.League:
            pin.setSprite(shipSprite);

            break;
         case WorldMapSpot.SpotType.Discovery:
            pin.setSprite(pin.spot.discovered ? discoverySprite : unknownSprite);

            break;
         case WorldMapSpot.SpotType.Player:
            pin.setSprite(discoverySprite);
            break;
      }
   }

   public void highlightPin (WorldMapSpot spot, bool show) {
      WorldMapPanelPin pin = _pins.FirstOrDefault(_ => _.spot == spot);

      if (pin != null && pin.rect != null) {
         pin.rect.localScale += show ? Vector3.one * 0.5f : -Vector3.one * 0.5f;
      }
   }

   private void updatePinLabel(WorldMapPanelPin pin) {
      // Set the label text
      pin.setLabel(string.Empty);

      if (pin.spot == null) {
         return;
      }

      if (pin.spot.type == WorldMapSpot.SpotType.Player) {
         pin.setLabel(pin.spot.displayName);

         // Change the label color
         bool isPlayerPin = Global.player != null && Util.areStringsEqual(pin.spot.displayName, Global.player.entityName);
         pin.setLabelColor(isPlayerPin ? currentPlayerLabelColor : defaultLabelColor);
      }
   }

   private void positionPinLabel (WorldMapPanelPin pin) {
      if (pin.spot == null || pin.label == null) {
         return;
      }

      // Reposition the label to improve visibility
      Vector2Int mapSize = WorldMapManager.self.getMapSize();
      bool isPinTop = pin.spot.worldY > mapSize.y * 0.5f;
      bool isPinRight = pin.spot.worldX > mapSize.x * 0.5f;
      float isPinTopValue = isPinRight ? 0.0f : 1.0f;
      float isPointerTopValue = isPinTop ? 0.0f : 1.0f;
      pin.label.rectTransform.anchorMin = new Vector2(isPinTopValue, isPointerTopValue);
      pin.label.rectTransform.anchorMax = pin.label.rectTransform.anchorMin;
      pin.label.rectTransform.pivot = Vector2.one - pin.label.rectTransform.anchorMin;
      pin.label.rectTransform.anchoredPosition = Vector2.zero;
   }

   public List<WorldMapPanelPin> getPinsWithinArea (WorldMapPanelAreaCoords mapPanelAreaCoords) {
      List<WorldMapPanelPin> areaPins = new List<WorldMapPanelPin>();

      foreach (WorldMapPanelPin pin in _pins) {
         WorldMapAreaCoords pinAreaCoords = new WorldMapAreaCoords(pin.spot.worldX, pin.spot.worldY);
         WorldMapPanelAreaCoords pinPanelAreaCoords = WorldMapPanel.self.transformCoords(pinAreaCoords);

         if (pinPanelAreaCoords == mapPanelAreaCoords) {
            areaPins.Add(pin);
         }
      }

      return areaPins;
   }

   private bool arePinsOverlapping (WorldMapPanelPin pinA, WorldMapPanelPin pinB, float maxDistance) {
      if (pinA == null || pinB == null) {
         return false;
      }

      // If the pins are within maxDistance from each other, they are considered to be overlapping
      Vector3 bLocalPosition = pinB.transform.localPosition;
      Vector3 aLocalPosition = pinA.transform.localPosition;

      return (bLocalPosition - aLocalPosition).sqrMagnitude < (maxDistance * maxDistance);
   }

   private void applyFilter () {
      foreach (WorldMapPanelPin pin in _pins) {
         pin.toggle(filter.Contains(pin.spot.type));
      }
   }

   #region Private Variables

   // Pins Registry
   private List<WorldMapPanelPin> _pins = new List<WorldMapPanelPin>();

   #endregion
}
