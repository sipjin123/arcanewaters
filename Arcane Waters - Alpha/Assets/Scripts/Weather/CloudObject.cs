using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CloudObject : MonoBehaviour {
   #region Public Variables

   // The direction the cloud moves to
   public Direction direction;

   // The movement speed
   public float movementSpeed = .1f;

   // The randomization thresold
   public static int RANDOMIZE_THRESOLD = 2;

   // The current thresold
   public int currentThresold = 0;

   // Type of weather
   public WeatherEffectType weatherType;

   // Reference to the sprite renderer
   public SpriteRenderer spriteRenderer, shadowSpriteRenderer;

   // If this obj is active
   public bool isActive;

   // The root position
   public Vector3 rootPosition;

   // The shadow object
   public GameObject shadowObj;

   // If the weather type is for the battle background
   public bool isBattleBackgroundWeather;

   // The biome type
   public Biome.Type biomeType;

   // The type of cloud { low / medium / high }
   public CloudType cloudType = CloudType.None;

   #endregion

   public void resetObject (WeatherEffectType weatherType, Direction direction, Vector3 startPosition, Vector3 rootPosition, bool isBattleBackgroundWeather, Biome.Type biomeType = Biome.Type.None) {
      this.weatherType = weatherType;
      this.biomeType = biomeType;
      this.isBattleBackgroundWeather = isBattleBackgroundWeather;
      currentThresold++;

      // Movement setup
      movementSpeed = Random.Range(.05f, .1f);
      this.direction = direction;
      this.rootPosition = rootPosition;
      transform.position = startPosition;

      // Sprite setup
      Sprite[] sprites = getSprites();
      int spriteRandomIndex = Random.Range(0, sprites.Length);
      spriteRenderer.sprite = sprites[spriteRandomIndex];
      spriteRenderer.maskInteraction = isBattleBackgroundWeather ? SpriteMaskInteraction.VisibleInsideMask : SpriteMaskInteraction.None;

      // Setup shadows
      if (isBattleBackgroundWeather) {
         shadowObj.SetActive(false);
         Vector3 cachedLocalPosition = transform.localPosition;
         if (spriteRandomIndex == 0 || spriteRandomIndex == 1 || spriteRandomIndex == 2) {
            cloudType = CloudType.HighCloud;
            float highCloudYPos = 0f;
            transform.localPosition = new Vector3(cachedLocalPosition.x, highCloudYPos, cachedLocalPosition.z);
         } else if (spriteRandomIndex == 3 || spriteRandomIndex == 4 || spriteRandomIndex == 5) {
            cloudType = CloudType.LowCloud;
            float lowCloudYPos = -0.275f;
            transform.localPosition = new Vector3(cachedLocalPosition.x, lowCloudYPos, cachedLocalPosition.z);
         } else if (spriteRandomIndex == 6 || spriteRandomIndex == 7 || spriteRandomIndex == 8) {
            cloudType = CloudType.MediumCloud;
            float mediumCloudYPos = -0.1f;
            transform.localPosition = new Vector3(cachedLocalPosition.x, mediumCloudYPos, cachedLocalPosition.z);
         } else {
            cloudType = CloudType.None;
         }
      } else {
         shadowObj.SetActive(weatherType != WeatherEffectType.Mist); 
         Sprite[] shadowSprites = WeatherManager.self.cloudSpriteList.Find(_ => _.weatherType == weatherType).spriteShadowReferences;
         if (shadowSprites.Length > 0) {
            shadowSpriteRenderer.sprite = shadowSprites[spriteRandomIndex];
         }
      }

      isActive = true;
   }

   private Sprite[] getSprites () {
      Sprite[] newSprites;

      newSprites = WeatherManager.self.cloudSpriteList.Find(_ => _.weatherType == weatherType).spriteReferences;
      switch (biomeType) {
         case Biome.Type.Forest:
         case Biome.Type.Desert:
         case Biome.Type.Snow:
         case Biome.Type.Lava:
         case Biome.Type.Mushroom:
            CloudBiomeSpritePair cloudBiomePair = WeatherManager.self.battleBoardCloudSpriteList.Find(_ => _.biomeType == biomeType);
            newSprites = cloudBiomePair.spriteReferences;
            break;
      }
      spriteRenderer.enabled = true;
      return newSprites;
   } 

   public void move () {
      if (!isActive) {
         return;
      }

      switch (direction) {
         case Direction.East:
            transform.position += transform.right * movementSpeed * Time.deltaTime;
            if (transform.position.x > rootPosition.x + (isBattleBackgroundWeather ? BattleBoard.maxRightPos : WeatherManager.maxRightPos)) {
               float newYValue = isBattleBackgroundWeather ? rootPosition.y : transform.position.y; 
               if (currentThresold >= RANDOMIZE_THRESOLD) {
                  newYValue = Random.Range(rootPosition.y + (isBattleBackgroundWeather ? BattleBoard.maxUpPos : WeatherManager.maxUpPos), 
                     rootPosition.y + (isBattleBackgroundWeather ? BattleBoard.maxDownPos : WeatherManager.maxDownPos));
                  currentThresold = 0;
               }
               float newXValue = rootPosition.x + (isBattleBackgroundWeather ? BattleBoard.maxLeftPos : WeatherManager.maxLeftPos);
              
               resetObject(weatherType, direction, new Vector3(newXValue, newYValue, transform.position.z), rootPosition, isBattleBackgroundWeather, biomeType);
            }
            break;
         case Direction.West:
            transform.position -= transform.right * movementSpeed * Time.deltaTime;
            if (transform.position.x < rootPosition.x - (isBattleBackgroundWeather ? BattleBoard.maxRightPos : WeatherManager.maxRightPos)) {
               float newYValue = isBattleBackgroundWeather ? rootPosition.y : transform.position.y;
               if (currentThresold >= RANDOMIZE_THRESOLD) {
                  newYValue = Random.Range(rootPosition.y + (isBattleBackgroundWeather ? BattleBoard.maxUpPos : WeatherManager.maxUpPos), 
                     rootPosition.y + (isBattleBackgroundWeather ? BattleBoard.maxDownPos : WeatherManager.maxDownPos));
                  currentThresold = 0;
               }
               float newXValue = rootPosition.x + (isBattleBackgroundWeather ? BattleBoard.maxRightPos : WeatherManager.maxRightPos);
             
               resetObject(weatherType, direction, new Vector3(newXValue, newYValue, transform.position.z), rootPosition, isBattleBackgroundWeather, biomeType);
            }
            break;
         case Direction.South:
            transform.position -= transform.up* movementSpeed * Time.deltaTime;
            if (transform.position.y < rootPosition.y - WeatherManager.maxRightPos) {
               float newXValue = isBattleBackgroundWeather ? rootPosition.x : transform.position.x;
               if (currentThresold >= RANDOMIZE_THRESOLD) {
                  newXValue = Random.Range(rootPosition.x + WeatherManager.maxLeftPos, rootPosition.x + WeatherManager.maxRightPos);
                  currentThresold = 0;
               }

               resetObject(weatherType, direction, new Vector3(newXValue, rootPosition.y + WeatherManager.maxUpPos, transform.position.z), rootPosition, isBattleBackgroundWeather, biomeType);
            }
            break;
         case Direction.North:
            transform.position += transform.up * movementSpeed * Time.deltaTime;
            if (transform.position.y > rootPosition.y + WeatherManager.maxRightPos) {
               float newXValue = isBattleBackgroundWeather ? rootPosition.x : transform.position.x;
               if (currentThresold >= RANDOMIZE_THRESOLD) {
                  newXValue = Random.Range(rootPosition.x + WeatherManager.maxLeftPos, rootPosition.x + WeatherManager.maxRightPos);
                  currentThresold = 0;
               }

               resetObject(weatherType, direction, new Vector3(newXValue, rootPosition.y - WeatherManager.maxUpPos, transform.position.z), rootPosition, isBattleBackgroundWeather, biomeType);
            }
            break;
      }
   }

   #region Private Variables

   #endregion
}