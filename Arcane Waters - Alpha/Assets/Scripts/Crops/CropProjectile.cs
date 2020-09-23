using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CropProjectile : MonoBehaviour {
   #region Public Variables

   // Range of maximum height that projectile will reach at its peak
   public float archHeightMin;
   public float archHeightMax;

   // Range of life time (how long projectile will be in the air) of crop
   public float lifeTimeMin;
   public float lifeTimeMax;

   // Range of distance that projectile will move from initial position
   public float distanceMin;
   public float distanceMax;

   // Sprite object inside hierarchy
   public GameObject projectileSpriteObj;

   // The list of crops and their associated sprites
   public List<CropSprite> cropSpriteList;

   #endregion

   private void Update () {
      double timeAlive = Time.time - _startTime;
      float lerpTime = lerpTime = (float) (timeAlive / _lifeTime);

      float angleInDegrees = lerpTime * 180f;
      float cropHeight = Util.getSinOfAngle(angleInDegrees) * _archHeight;

      Util.setLocalY(projectileSpriteObj.transform, cropHeight);

      Util.setXY(this.transform, Vector2.Lerp(_startPos, _endPos, lerpTime));

      if (timeAlive > _lifeTime) {
         processDestruction();
      }
   }

   public void init(Vector2 startPos, Vector2 dir, CropSpot cropSpot) {
      _startTime = Time.time;
      _archHeight = Random.Range(archHeightMin, archHeightMax) * TILE_SIZE;
      _lifeTime = Random.Range(lifeTimeMin, lifeTimeMax);
      _distance = Random.Range(distanceMin, distanceMax) * TILE_SIZE;

      _startPos = startPos;
      _endPos = _startPos + dir * _distance;
      _cropSpot = cropSpot;
   }

   public void setSprite (Crop.Type cropType) {
      CropSprite cropSprite = cropSpriteList.Find(_ => _.cropType == cropType);
      if (cropSprite != null) {
         projectileSpriteObj.GetComponent<SpriteRenderer>().sprite = cropSprite.sprite;
      }
   }

   protected void processDestruction () {
      GameObject spawnedObj = Instantiate(PrefabsManager.self.cropPickupPrefab);
      spawnedObj.transform.position = transform.position;
      spawnedObj.transform.localScale = new Vector3(transform.localScale.x, 1, 1);

      CropPickup cropPickup = spawnedObj.GetComponent<CropPickup>();
      cropPickup.cropSpot = _cropSpot;
      cropPickup.spriteRender.sprite = projectileSpriteObj.GetComponent<SpriteRenderer>().sprite;

      Destroy(this.gameObject);
   }

   #region Private Variables

   // Time at which crop projectile started its life
   private float _startTime;

   // Maximum height that projectile will reach at its peak
   private float _archHeight;

   // Life time (how long projectile will be in the air) of crop
   private float _lifeTime;

   // distance that projectile will move from initial position
   private float _distance;

   // Initial position of projectile
   private Vector2 _startPos;

   // End position of projectile - crop pickup will be spawned here
   private Vector2 _endPos;

   // Related crop spot, previously occupied by this crop
   private CropSpot _cropSpot;

   // Standard size of tile in world
   private const float TILE_SIZE = 0.166666f;

   #endregion
}
