using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RainSpawner : MonoBehaviour
{
   public GameObject rainPrefab;
   public int width, height;
   public float chanceToSpawn;
   public float distanceFromEachDrop = 4;
   public float alphaMin, alphaMax;
   public float offsetMax = 1;

   public int rainCounter = 0;
   void Start () {
      for (int x = -width / 2; x < width / 2; x++) {

         for (int y = -height / 2; y < height / 2; y++) {
            if (Random.value < chanceToSpawn) {
               GameObject ob = Instantiate(rainPrefab, new Vector3(x * distanceFromEachDrop, y * distanceFromEachDrop, 0), Quaternion.identity);
               SpriteRenderer rend = ob.GetComponent<SpriteRenderer>();
               rend.color = new Color(1, 1, 1, Random.Range(alphaMin, alphaMax));
               ob.transform.Translate(new Vector3(Random.Range(-offsetMax, offsetMax), Random.Range(-offsetMax, offsetMax)));
               rainCounter++;
            }
         }
      }
   }

   void Update () {

   }
}