using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class Whale : ClientMonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   #endregion

   private void Start () {
      _originalPosition = transform.position;
      _animation.gameObject.SetActive(false);
      pickNextAppearTime();
   }

   private void pickNextAppearTime () {
      _nextAppearTime = (float) NetworkTime.time + Random.Range(_appearDelay * 0.5f, _appearDelay * 1.5f);
   }

   private void appear () {
      Vector2? pos = pickAppearPosition();
      if (pos == null) {
         return;
      }

      transform.position = pos.Value;
      _zsnap.snapZ();

      if (Global.player != null && AreaManager.self.tryGetArea(Global.player.areaKey, out Area area)) {
         if (area.hasTileAttribute(TileAttributes.Type.DeepWater, transform.position)) {
            _animation.setNewTexture(_darkWhaleTexture);
         } else {
            _animation.setNewTexture(_lightWhaleTexture);
         }
      }

      _animation.gameObject.SetActive(true);
      _animation.resetAnimation();
      _animation.setIndex(0);
      _animation.isPaused = false;
      _spriteRenderer.flipX = Random.value > 0.5f;
   }

   private void Update () {
      if (NetworkTime.time > _nextAppearTime) {
         pickNextAppearTime();
         appear();
      }
   }

   private Vector2? pickAppearPosition () {
      for (int i = 0; i < 10; i++) {
         // 0.16f because radius is in tiles
         Vector2 point = _originalPosition + Random.insideUnitCircle * _appearRadius;
         if (_spaceRequirer.wouldHaveSpace(point)) {
            return point;
         }
      }

      return null;
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField f in dataFields) {
         if (f.isKey(DataField.WHALE_RADIUS_KEY)) {
            if (f.tryGetFloatValue(out float val)) {
               // Change units from tiles to Unity units
               _appearRadius = val * 0.16f;
            } else {
               D.warning("Could not extract datafield value");
            }
         } else if (f.isKey(DataField.WHALE_DELAY_KEY)) {
            if (f.tryGetFloatValue(out float val)) {
               _appearDelay = val;
            } else {
               D.warning("Could not extract datafield value");
            }
         }
      }
   }

   #region Private Variables

   // The position in which this whale was placed originally
   private Vector2 _originalPosition;

   // The radius in which the whale can appear
   private float _appearRadius = 1f;

   // The average delay in seconds of the whale appearance
   private float _appearDelay = 10f;

   // At what time next is the whale supposed to appear
   private float _nextAppearTime;

   // The space requirer used to check if we have space at a given position
   [SerializeField] private SpaceRequirer _spaceRequirer = null;

   // The main animator of the whale
   [SerializeField] private SimpleAnimation _animation = null;

   // The main sprite of the whale
   [SerializeField] private SpriteRenderer _spriteRenderer = null;

   // The ZSnap component
   [SerializeField] private ZSnap _zsnap = null;

   // The different versions of whale
   [SerializeField] private Texture2D _lightWhaleTexture = null;
   [SerializeField] private Texture2D _darkWhaleTexture = null;

   #endregion
}
