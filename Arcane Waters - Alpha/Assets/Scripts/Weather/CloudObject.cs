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

   #endregion

   public void resetObject (WeatherEffectType weatherType, Direction direction, Vector3 startPosition, Vector3 rootPosition, bool isBattleBackgroundWeather) {
      this.weatherType = weatherType;
      currentThresold++;
      movementSpeed = Random.Range(.1f, .3f);
      this.direction = direction;
      this.rootPosition = rootPosition;
      this.isBattleBackgroundWeather = isBattleBackgroundWeather;
      transform.position = startPosition;

      Sprite[] sprites = WeatherManager.self.cloudSpriteList.Find(_ => _.weatherType == weatherType).spriteReferences;
      Sprite[] shadowSprites = WeatherManager.self.cloudSpriteList.Find(_ => _.weatherType == weatherType).spriteShadowReferences;
      int spriteRandomIndex = Random.Range(0, sprites.Length);
      spriteRenderer.sprite = sprites[spriteRandomIndex];
      if (shadowSprites.Length > 0) {
         shadowSpriteRenderer.sprite = shadowSprites[spriteRandomIndex];
      }
      isActive = true;

      if (isBattleBackgroundWeather) {
         shadowObj.SetActive(false);
      } else {
         shadowObj.SetActive(weatherType != WeatherEffectType.Mist);
      }
   }

   public void move () {
      if (!isActive) {
         return;
      }

      switch (direction) {
         case Direction.East:
            transform.position += transform.right * movementSpeed * Time.deltaTime;
            if (transform.position.x > rootPosition.x + (isBattleBackgroundWeather ? BattleBoard.maxRightPos : WeatherManager.maxRightPos)) {
               float newYValue = rootPosition.y; // transform.position.y;
               if (currentThresold >= RANDOMIZE_THRESOLD) {
                  newYValue = Random.Range(rootPosition.y + (isBattleBackgroundWeather ? BattleBoard.maxUpPos : WeatherManager.maxUpPos), 
                     rootPosition.y + (isBattleBackgroundWeather ? BattleBoard.maxDownPos : WeatherManager.maxDownPos));
                  currentThresold = 0;
               }
               float newXValue = rootPosition.x + (isBattleBackgroundWeather ? BattleBoard.maxLeftPos : WeatherManager.maxLeftPos);
              
               resetObject(weatherType, direction, new Vector3(newXValue, newYValue, transform.position.z), rootPosition, isBattleBackgroundWeather);
            }
            break;
         case Direction.West:
            transform.position -= transform.right * movementSpeed * Time.deltaTime;
            if (transform.position.x < rootPosition.x - (isBattleBackgroundWeather ? BattleBoard.maxRightPos : WeatherManager.maxRightPos)) {
               float newYValue = rootPosition.y; // transform.position.y;
               if (currentThresold >= RANDOMIZE_THRESOLD) {
                  newYValue = Random.Range(rootPosition.y + (isBattleBackgroundWeather ? BattleBoard.maxUpPos : WeatherManager.maxUpPos), 
                     rootPosition.y + (isBattleBackgroundWeather ? BattleBoard.maxDownPos : WeatherManager.maxDownPos));
                  currentThresold = 0;
               }
               float newXValue = rootPosition.x + (isBattleBackgroundWeather ? BattleBoard.maxRightPos : WeatherManager.maxRightPos);
             
               resetObject(weatherType, direction, new Vector3(newXValue, newYValue, transform.position.z), rootPosition, isBattleBackgroundWeather);
            }
            break;
         case Direction.South:
            transform.position -= transform.up* movementSpeed * Time.deltaTime;
            if (transform.position.y < rootPosition.y - WeatherManager.maxRightPos) {
               float newXValue = transform.position.x;
               if (currentThresold >= RANDOMIZE_THRESOLD) {
                  newXValue = Random.Range(rootPosition.x + WeatherManager.maxLeftPos, rootPosition.x + WeatherManager.maxRightPos);
                  currentThresold = 0;
               }
               resetObject(weatherType, direction, new Vector3(newXValue, rootPosition.y + WeatherManager.maxUpPos, transform.position.z), rootPosition, isBattleBackgroundWeather);
            }
            break;
         case Direction.North:
            transform.position += transform.up * movementSpeed * Time.deltaTime;
            if (transform.position.y > rootPosition.y + WeatherManager.maxRightPos) {
               float newXValue = transform.position.x;
               if (currentThresold >= RANDOMIZE_THRESOLD) {
                  newXValue = Random.Range(rootPosition.x + WeatherManager.maxLeftPos, rootPosition.x + WeatherManager.maxRightPos);
                  currentThresold = 0;
               }
               resetObject(weatherType, direction, new Vector3(newXValue, rootPosition.y - WeatherManager.maxUpPos, transform.position.z), rootPosition, isBattleBackgroundWeather);
            }
            break;
      }
   }

   #region Private Variables

   #endregion
}