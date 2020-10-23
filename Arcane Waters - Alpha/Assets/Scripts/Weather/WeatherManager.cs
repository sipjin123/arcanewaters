using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System;
using Random = UnityEngine.Random;
using System.Xml;

public class WeatherManager : MonoBehaviour {
   #region Public Variables

   // Reference to self
   public static WeatherManager self;

   // The type of weather effect
   public WeatherEffectType weatherEffectType;

   // Screen effects
   public GameObject rainEffectObj, snowEffectObj;

   // The type of directions the weather entities can move towards
   public Direction[] weatherDirections = new Direction[4] { Direction.North, Direction.South, Direction.East, Direction.West };

   // The parent of the separate entities 
   public Transform mistParent, darkMistParent, cloudParent, genericCloudHolder, sunRayHolder;

   // The sun ray effect prefab
   public GameObject sunRayPrefab;

   // The moving objects
   public CloudObject[] cloudObjects;

   // Starts the weather simulation
   public bool startWeatherSimulation;

   // The max horizontal position
   public const float maxRightPos = 7f, maxLeftPos = -7f;

   // The max vetical position
   public const float maxUpPos = 5f, maxDownPos = -5f;

   // List of cloud sprite combinations
   public List<CloudSpritePair> cloudSpriteList;

   // List of cloud sprite combinations for battleboards
   public List<CloudBiomeSpritePair> battleBoardCloudSpriteList;

   // The root of the spawn-able clouds
   public Transform spawnRoot;

   // The direction the objects moves towards
   public Direction direction;

   // The main camera reference
   public Camera mainCam;

   // The sunray angles
   public static float SunRayRightAngle = -135;
   public static float SunRayLeftAngle = 135;

   // The max and min sunrays
   public static int MAX_SUNRAYS = 10;
   public static int MIN_SUNRAYS = 5;

   // Parameters for snow generation
   public float snowStartPosX, snowStartPosY = 0;
   public int snowCellCountX, snowCellCountY;
   public float cellSpacing = 0;
   public GameObject snowCellPrefab;
   public float[] snowRandomSpeed = new float[72];

   // The layer mask cache that will be switched depending if the area is an interior
   public LayerMask normalMask;
   public LayerMask mutedWeatherMask;

   #endregion

   private void Awake () {
      self = this;
      initializeCloudObjects();

      mainCam = Camera.main;

      for (int i = 0; i < snowRandomSpeed.Length; i++) {
         snowRandomSpeed[i] = Random.Range(.2f, .6f);
      }

      snowEffectObj.transform.SetParent(mainCam.transform, false);
      rainEffectObj.transform.SetParent(mainCam.transform, false);
   }

   public void muteWeather () {
      mainCam.cullingMask = mutedWeatherMask;
   }

   public void activateWeather () {
      mainCam.cullingMask = normalMask;
   }

   public void setWeatherSimulation (WeatherEffectType weatherEffect, Transform rootObj = null) {
      if (Util.isBatch()) {
         return;
      }

      this.weatherEffectType = weatherEffect;
      sunRayHolder.gameObject.DestroyChildren();
      spawnRoot = rootObj;
      rainEffectObj.SetActive(false);
      snowEffectObj.SetActive(false);
      foreach (CloudObject cloudObj in cloudObjects) {
         cloudObj.gameObject.SetActive(false);
      }
      direction = weatherDirections[Random.Range(0, weatherDirections.Length)];

      foreach (CloudObject cloudObj in cloudObjects) {
         switch (weatherEffect) {
            case WeatherEffectType.Mist:
               cloudObj.transform.SetParent(mistParent);
               resetWeatherSimulation();
               break;
            case WeatherEffectType.DarkCloud:
               cloudObj.transform.SetParent(darkMistParent);
               resetWeatherSimulation();
               break;
            case WeatherEffectType.Cloud:
               cloudObj.transform.SetParent(cloudParent);
               resetWeatherSimulation();
               break;
            case WeatherEffectType.Snow:
               snowEffectObj.gameObject.SetActive(true);
               break;
            case WeatherEffectType.Rain:
               rainEffectObj.SetActive(true);
               break;
            case WeatherEffectType.Sunny:
               int randomizedSunRayCount = Random.Range(MIN_SUNRAYS, MAX_SUNRAYS);
               for (int i = 0; i < randomizedSunRayCount; i++) {
                  GameObject sunRayObj = Instantiate(sunRayPrefab, sunRayHolder);
                  switch (direction) {
                     case Direction.East:
                     case Direction.North:
                        sunRayObj.transform.GetComponentInChildren<SpriteRenderer>().flipX = false;
                        sunRayObj.transform.GetChild(0).localEulerAngles = new Vector3(0, 0, SunRayRightAngle);
                        break;
                     case Direction.West:
                     case Direction.South:
                        sunRayObj.transform.GetComponentInChildren<SpriteRenderer>().flipX = true;
                        sunRayObj.transform.GetChild(0).localEulerAngles = new Vector3(0, 0, SunRayLeftAngle);
                        break;
                  }

                  float newXPosition = 0;
                  float newYPosition = 0;
                  if (spawnRoot != null) {
                     newXPosition = Random.Range(spawnRoot.transform.position.x + maxLeftPos, spawnRoot.transform.position.x + maxRightPos);
                     newYPosition = Random.Range(spawnRoot.transform.position.y + maxDownPos, spawnRoot.transform.position.y + maxUpPos);
                  } else {
                     newXPosition = Random.Range(maxLeftPos, maxRightPos);
                     newYPosition = Random.Range(maxDownPos, maxUpPos);
                  }

                  sunRayObj.transform.position = new Vector3(newXPosition, newYPosition, 0);
               }
               break;
         }
      }
      startWeatherSimulation = true;
   }

   private void generateSnowCells () {
      snowEffectObj.DestroyChildren();
      int indexCount = 0;
      int intervalCount = 0;
      for (int snowIndexHorizontal = 0; snowIndexHorizontal < snowCellCountX; snowIndexHorizontal++) {
         for (int snowIndexVertical = 0; snowIndexVertical < snowCellCountY; snowIndexVertical++) {
            GameObject snowCellObj = Instantiate(snowCellPrefab, snowEffectObj.transform);
            snowCellObj.transform.localPosition = new Vector3(snowStartPosX + (snowIndexHorizontal * cellSpacing), snowStartPosY + (snowIndexVertical * cellSpacing), 0);
            snowCellObj.SetActive(true);
            indexCount++;
            if (indexCount % 2 == 0) {
               snowCellObj.GetComponent<SimpleAnimation>().modifyAnimSpeed(snowRandomSpeed[intervalCount]);
               intervalCount++;
            }
         }
      }
   }

   private void initializeCloudObjects () {
      List<CloudObject> newCloudObjs = new List<CloudObject>();
      foreach (Transform child in genericCloudHolder) {
         newCloudObjs.Add(child.GetComponent<CloudObject>());
         child.gameObject.SetActive(false);
      }
      cloudObjects = newCloudObjs.ToArray();
   }

   private void resetWeatherSimulation () {
      if (Util.isBatch()) {
         return;
      }
      direction = weatherDirections[Random.Range(0, weatherDirections.Length)];

      if (weatherEffectType == WeatherEffectType.Mist) {
         // Mist weather can only move left and right
         if (direction == Direction.North) {
            direction = Direction.East;
         } else if (direction == Direction.South) {
            direction = Direction.West;
         }
      }

      switch (weatherEffectType) {
         case WeatherEffectType.DarkCloud:
         case WeatherEffectType.Mist:
         case WeatherEffectType.Cloud:
            rainEffectObj.SetActive(false);
            snowEffectObj.SetActive(false);
            foreach (CloudObject cloudObj in cloudObjects) {
               cloudObj.gameObject.SetActive(true);
               float spawnXPosition = 0;
               float spawnYPosition = 0;
               if (spawnRoot != null) {
                  spawnXPosition = Random.Range(spawnRoot.position.x - maxRightPos, spawnRoot.position.x + maxRightPos);
                  spawnYPosition = Random.Range(spawnRoot.position.y - maxUpPos, spawnRoot.position.y + maxUpPos);
               } else {
                  spawnXPosition = Random.Range(maxLeftPos, maxRightPos);
                  spawnYPosition = Random.Range(maxUpPos, maxDownPos);
               }
               cloudObj.resetObject(weatherEffectType, direction, new Vector2(spawnXPosition, spawnYPosition), spawnRoot ? new Vector2(spawnRoot.position.x, spawnRoot.position.y) : Vector2.zero, false);
            }
            break;
         case WeatherEffectType.Rain:
         case WeatherEffectType.Snow:
            foreach (CloudObject cloudObj in cloudObjects) {
               cloudObj.gameObject.SetActive(false);
            }
            break;
      }
   }

   public void loadWeatherForArea (Area area) {
      WeatherEffectType weatherType = AreaManager.self.getAreaWeatherEffectType(area.areaKey);
      if (!area.isInterior) {
         if (area.isSea) {
            if (weatherType == WeatherEffectType.Cloud || weatherType == WeatherEffectType.DarkCloud) {
               setWeatherSimulation(WeatherEffectType.None, null);
               area.cloudManager.weatherEffectType = weatherType;
               area.cloudManager.enabled = true;
            } else if (weatherType == WeatherEffectType.Rain || weatherType == WeatherEffectType.Snow) {
               setWeatherSimulation(weatherType, area.transform);
            }
         } else {
            Area.SpecialType specialType = AreaManager.self.getAreaSpecialType(area.areaKey);
            if (specialType == Area.SpecialType.TreasureSite || specialType == Area.SpecialType.Town) {
               if (weatherType == WeatherEffectType.Rain || weatherType == WeatherEffectType.Snow) {
                  setWeatherSimulation(weatherType, null);
               } else {
                  area.cloudShadowManager.enabled = true;
                  setWeatherSimulation(WeatherEffectType.None, null);
               }
            } else {
               setWeatherSimulation(weatherType, area.transform);
            }
         }
      } else {
         setWeatherSimulation(WeatherEffectType.None);
      }
   }

   private void Update () {
      if (startWeatherSimulation) {
         switch (weatherEffectType) {
            case WeatherEffectType.DarkCloud:
            case WeatherEffectType.Mist:
            case WeatherEffectType.Cloud:
               foreach (CloudObject cloudObj in cloudObjects) {
                  cloudObj.move();
               }
               break;
         }
      }
   }

   #region Private Variables

   #endregion
}

public enum WeatherEffectType {
   None = 0,
   Sunny = 1,
   Rain = 2,
   Mist = 3,
   DarkCloud = 4,
   Snow = 5, 
   Cloud = 6
}

[Serializable]
public class CloudSpritePair {
   // Type of cloud
   public WeatherEffectType weatherType;

   // Reference to the sprite
   public Sprite[] spriteReferences;

   // Reference to the sprite shadow
   public Sprite[] spriteShadowReferences;
}

[Serializable]
public class CloudBiomeSpritePair
{
   // Type of biome
   public Biome.Type biomeType;

   // Reference to the sprite
   public Sprite[] spriteReferences;
}

public enum CloudType {
   None = 0,
   LowCloud = 1,
   MediumCloud = 2,
   HighCloud = 3
}