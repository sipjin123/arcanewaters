using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System;
using Random = UnityEngine.Random;

public class WeatherManager : MonoBehaviour {
   #region Public Variables

   // Reference to self
   public static WeatherManager self;

   // The type of weather effect
   public WeatherEffectType weatherEffectType;

   // Screen effects
   public GameObject rainEffectObj, snowEffectObj;

   // The parent of the separate entities 
   public Transform mistParent, darkMistParent, cloudParent, genericCloudHolder;

   // The moving objects
   public CloudObject[] cloudObjects;

   // Starts the weather simulation
   public bool startWeatherSimulation;

   // The max horizontal position
   public static float maxRightPos = 5f, maxLeftPos = -5f;

   // The max vetical position
   public static float maxUpPos = 5f, maxDownPos = -5f;

   // List of cloud sprite combinations
   public List<CloudSpritePair> cloudSpriteList;

   // The root of the spawn-able clouds
   public Transform spawnRoot;

   // The direction the objects moves towards
   public Direction direction;

   // The main camera reference
   public Camera mainCam;

   #endregion

   private void Awake () {
      self = this;
      initializeCloudObjects(WeatherEffectType.DarkMist);

      mainCam = Camera.main;
   }

   public void setWeatherSimulation (WeatherEffectType weatherEffect) {
      foreach (CloudObject cloudObj in cloudObjects) {
         switch (weatherEffect) {
            case WeatherEffectType.Mist:
               cloudObj.transform.SetParent(mistParent);
               break;
            case WeatherEffectType.DarkMist:
               cloudObj.transform.SetParent(darkMistParent);
               break;
            case WeatherEffectType.Cloud:
               cloudObj.transform.SetParent(cloudParent);
               break;
            case WeatherEffectType.Snow:
               snowEffectObj.transform.position = mainCam.transform.position;
               snowEffectObj.gameObject.SetActive(true);
               break;
            case WeatherEffectType.Rain:
               rainEffectObj.transform.position = mainCam.transform.position;
               rainEffectObj.gameObject.SetActive(true);
               break;
         }
      }
   }

   private void initializeCloudObjects (WeatherEffectType weatherEffect) {
      List<CloudObject> newCloudObjs = new List<CloudObject>();
      foreach (Transform child in genericCloudHolder) {
         newCloudObjs.Add(child.GetComponent<CloudObject>());
      }
      cloudObjects = newCloudObjs.ToArray();
   }

   private void resetWeatherSimulation () {
      switch (weatherEffectType) {
         case WeatherEffectType.DarkMist:
         case WeatherEffectType.Mist:
         case WeatherEffectType.Cloud:
            foreach (CloudObject cloudObj in cloudObjects) {
               float spawnXPosition = 0;
               float spawnYPosition = 0;
               if (spawnRoot != null) {
                  spawnXPosition = Random.Range(spawnRoot.position.x - maxRightPos, spawnRoot.position.x + maxRightPos);
                  spawnYPosition = Random.Range(spawnRoot.position.y - maxUpPos, spawnRoot.position.y + maxUpPos);
               } else {
                  spawnXPosition = Random.Range(maxLeftPos, maxRightPos);
                  spawnYPosition = Random.Range(maxUpPos, maxDownPos);
               }
               cloudObj.resetObject(weatherEffectType, direction, new Vector2(spawnXPosition, spawnYPosition), spawnRoot ? new Vector2(spawnRoot.position.x, spawnRoot.position.y) : Vector2.zero);
            }
            break;
      }
   }

   private void Update () {
      if (SystemInfo.deviceName == NubisDataFetchTest.DEVICE_NAME) {
         if (Input.GetKeyDown(KeyCode.Alpha1)) {
            weatherEffectType = WeatherEffectType.DarkMist;
            resetWeatherSimulation();
            startWeatherSimulation = true;
         }
         if (Input.GetKeyDown(KeyCode.Alpha2)) {
            weatherEffectType = WeatherEffectType.Mist;
            resetWeatherSimulation();
            startWeatherSimulation = true;
         }
         if (Input.GetKeyDown(KeyCode.Alpha3)) {
            weatherEffectType = WeatherEffectType.Cloud;
            resetWeatherSimulation();
            startWeatherSimulation = true;
         }
         if (Input.GetKeyDown(KeyCode.Alpha4)) {
            weatherEffectType = WeatherEffectType.Rain;
            setWeatherSimulation(WeatherEffectType.Rain);
            startWeatherSimulation = true;
         }
         if (Input.GetKeyDown(KeyCode.Alpha5)) {
            weatherEffectType = WeatherEffectType.Snow;
            setWeatherSimulation(WeatherEffectType.Snow);
            startWeatherSimulation = true;
         }

         if (startWeatherSimulation) {
            switch (weatherEffectType) {
               case WeatherEffectType.DarkMist:
               case WeatherEffectType.Mist:
               case WeatherEffectType.Cloud:
                  foreach (CloudObject cloudObj in cloudObjects) {
                     cloudObj.move();
                  }
                  break;
            }
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
   DarkMist = 4,
   Snow = 5, 
   Cloud = 6
}

[Serializable]
public class CloudSpritePair {
   // Type of cloud
   public WeatherEffectType weatherType;

   // Reference to the sprite
   public Sprite[] spriteReferences;
}