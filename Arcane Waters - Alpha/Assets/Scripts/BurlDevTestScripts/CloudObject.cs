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
   public SpriteRenderer spriteRenderer;

   // If this obj is active
   public bool isActive;

   // The root position
   public Vector2 rootPosition;

   #endregion

   public void resetObject (WeatherEffectType weatherType, Direction direction, Vector2 startPosition, Vector2 rootPosition) {
      this.weatherType = weatherType;
      currentThresold++;
      movementSpeed = Random.Range(.1f, .3f);
      this.direction = direction;
      this.rootPosition = rootPosition;
      transform.position = startPosition;

      Sprite[] sprites = WeatherManager.self.cloudSpriteList.Find(_ => _.weatherType == weatherType).spriteReferences;
      spriteRenderer.sprite = sprites[Random.Range(0, sprites.Length)];
      isActive = true;
   }

   public void move () {
      if (!isActive) {
         return;
      }

      switch (direction) {
         case Direction.East:
            transform.position += transform.right * movementSpeed * Time.deltaTime;
            if (transform.position.x > rootPosition.x + WeatherManager.maxRightPos) {
               float newYValue = transform.position.y;
               if (currentThresold >= RANDOMIZE_THRESOLD) {
                  newYValue = Random.Range(rootPosition.y + WeatherManager.maxUpPos, rootPosition.y + WeatherManager.maxDownPos);
                  currentThresold = 0;
               }
               resetObject(weatherType, direction, new Vector2(rootPosition.x + WeatherManager.maxLeftPos, newYValue), rootPosition);
            }
            break;
         case Direction.West:
            transform.position -= transform.right * movementSpeed * Time.deltaTime;
            if (transform.position.x < rootPosition.x - WeatherManager.maxRightPos) {
               float newYValue = transform.position.y;
               if (currentThresold >= RANDOMIZE_THRESOLD) {
                  newYValue = Random.Range(rootPosition.y + WeatherManager.maxUpPos, rootPosition.y + WeatherManager.maxDownPos);
                  currentThresold = 0;
               }
               resetObject(weatherType, direction, new Vector2(rootPosition.x + WeatherManager.maxRightPos, newYValue), rootPosition);
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
               resetObject(weatherType, direction, new Vector2(newXValue, rootPosition.y + WeatherManager.maxUpPos), rootPosition);
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
               resetObject(weatherType, direction, new Vector2(newXValue, rootPosition.y - WeatherManager.maxUpPos), rootPosition);
            }
            break;
      }
   }

   #region Private Variables

   #endregion
}