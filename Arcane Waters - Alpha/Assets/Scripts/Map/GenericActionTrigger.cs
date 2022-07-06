using UnityEngine;
using System;
using System.Collections.Generic;
using MapCreationTool.Serialization;
using MapCreationTool;
using UnityEngine.UI;
using TMPro;

public class GenericActionTrigger : MonoBehaviour, IMapEditorDataReceiver
{
   public enum InteractionType
   {
      Enter = 1,
      Exit = 2,
      Stay = 3
   }

   #region Public Variables

   // If this action trigger is sprite based or image based
   public bool isSpriteBase;

   // The sprite renderer assigned to this object
   public SpriteRenderer spriteRender;

   // The canvas group reference
   public CanvasGroup canvasGroup, alternativeCanvasGroup;

   // The image assigned to this object
   public Image genericImage;

   // The biome sprite pair
   public List<GenericBiomeSpritePair> biomeSpritePair;

   // The distance between this object to trigger interaction
   public const float INTERACT_DIST = 1f;

   // The distance between player and this object for the visibility of the sprite to render
   public const float VISIBLITY_DIST = 3;

   // Hardcoded action strings
   public static string WARP_TO_LEAGUE_ACTION = "Warp To League";

   // The list of actions that can be defined and triggered
   public static Dictionary<string, Action<NetEntity>> actions = new Dictionary<string, Action<NetEntity>> {
      { "Voyage Panel", showVoyagePanel },
      { WARP_TO_LEAGUE_ACTION, warpToVoyage },
      { "Exit League", exitVoyage }
   };

   // The type of interaction that is needed to trigger the action
   public InteractionType interactionType;

   // The direction pointed by the arrow
   public Direction arrowDirection = Direction.South;

   // The name of the action that should be triggered
   public string actionName;

   // Arrow that is showed if this is a voyage trigger region
   public GameObject voyageArrow;

   // The voyage collider
   public CircleCollider2D circleVoyageCollider;

   // Determine if this is within render bounds
   public bool withinRenderBounds;

   // The fade duration of the sprite
   public const float FADE_SPEED = .5f;

   // The current biome of this action trigger
   public Biome.Type biomeType;

   #endregion

   private static void showVoyagePanel (NetEntity entity) {
      GroupInstanceManager.self.showVoyagePanel(entity);
   }

   private static void warpToVoyage (NetEntity entity) {
      GroupInstanceManager.self.warpToVoyage(entity);
   }

   private static void exitVoyage (NetEntity entity) {
      GroupInstanceManager.self.returnToTownFromVoyage(entity);
   }

   private void Awake () {
      _collider = GetComponent<BoxCollider2D>();
   }

   private void Start () {
      if (actionName == WARP_TO_LEAGUE_ACTION) {
         spriteRender.enabled = false;
         alternativeCanvasGroup.alpha = 0;
         canvasGroup.alpha = 0;
         if (circleVoyageCollider != null) {
            circleVoyageCollider.enabled = true;
         }
         _collider.enabled = false;
         /* Old method where sprite displays in world
         if (biomeType != Biome.Type.None) {
            spriteRender.gameObject.SetActive(true);
            GenericBiomeSpritePair spritePairData = biomeSpritePair.Find(_ => _.biomeType == biomeType);
            if (isSpriteBase) {
               spriteRender.sprite = spritePairData == null ? null : spritePairData.sprite;
            } else {
               genericImage.sprite = spritePairData == null ? null : spritePairData.sprite;
            }
            if (circleVoyageCollider != null) {
               circleVoyageCollider.enabled = true;
            }
            _collider.enabled = false;
         }*/
      }
   }

   private void Update () {
      /* Old method where sprite displays in world
      if (actionName == WARP_TO_LEAGUE_ACTION) {
         Color currColor = spriteRender.color;
         float alphaValue = isSpriteBase ? currColor.a : canvasGroup.alpha;
         if (withinRenderBounds && alphaValue < 1) {
            currColor.a += Time.deltaTime * FADE_SPEED;
            if (isSpriteBase) {
               spriteRender.color = currColor;
               alternativeCanvasGroup.alpha += Time.deltaTime * FADE_SPEED;
            } else {
               canvasGroup.alpha += Time.deltaTime * FADE_SPEED;
            }
         } else if (!withinRenderBounds && alphaValue > 0) {
            currColor.a -= Time.deltaTime * FADE_SPEED;
            if (isSpriteBase) {
               spriteRender.color = currColor;
               alternativeCanvasGroup.alpha -= Time.deltaTime * FADE_SPEED;
            } else {
               canvasGroup.alpha -= Time.deltaTime * FADE_SPEED;
            }
         }
      }*/
   }

   public void receiveData (DataField[] dataFields) {
      float newWidthKey = 0, newHeightKey = 0;
      foreach (DataField field in dataFields) {
         switch (field.k.ToLower()) {
            case DataField.GENERIC_ACTION_TRIGGER_INTERACTION_TYPE:
               if (field.tryGetInteractionTypeValue(out InteractionType value)) {
                  interactionType = value;
               }
               break;
            case DataField.GENERIC_ACTION_TRIGGER_ACTION_NAME:
               actionName = field.v.Trim(' ');
               break;
            case DataField.GENERIC_ACTION_TRIGGER_ARROW_DIRECTION:
               if (field.tryGetDirectionValue(out Direction dir)) {
                  arrowDirection = dir;
               }
               break;
            case DataField.GENERIC_ACTION_TRIGGER_WIDTH_KEY:
               newHeightKey = field.floatValue;
               break;
            case DataField.GENERIC_ACTION_TRIGGER_HEIGHT_KEY:
               newHeightKey = field.floatValue;
               break;
         }
      }
      if (actionName != WARP_TO_LEAGUE_ACTION) {
         _collider.size = new Vector2(newWidthKey, newHeightKey);
      }

      // Configure the optional arrow
      voyageArrow.SetActive(actionName.ToLower().Contains("voyage") || actionName.ToLower().Contains("league"));
      updateArrow();
   }

   /// <summary>
   /// Updates the arrow color and direction, based on this area type and target area type
   /// </summary>
   public void updateArrow () {
      string dir = arrowDirection.ToString().ToLower();
      string color = "unrecognized";

      string thisArea = transform.GetComponentInParent<Area>()?.areaKey;
      if (thisArea != null) {
         if (AreaManager.self.isSeaArea(thisArea)) {
            color = "blue";
         } else {
            color = "gold";
         }
      }

      string spriteName = $"warp_{color}_{dir}";

      Sprite arrowSprite = ImageManager.getSprite("Map/Warp Arrows/" + spriteName);
      if (arrowSprite != null) {
         SpriteRenderer ren = voyageArrow.GetComponent<SpriteRenderer>();
         if (ren != null) {
            ren.sprite = arrowSprite;
         }
      } else {
         D.warning("Could not find sprite for warp arrow. Target sprite name: " + spriteName);
      }

      if (actionName != WARP_TO_LEAGUE_ACTION) {
         voyageArrow.transform.localPosition = -DirectionUtil.getVectorForDirection(arrowDirection);
      }
   }

   public bool hasCollider () {
      return _collider;
   }

   public Bounds getColliderBounds () {
      return _collider ? _collider.bounds : new Bounds();
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      NetEntity entity = collision.GetComponent<NetEntity>();

      // Warping to league uses GUI button to trigger
      if (actionName == WARP_TO_LEAGUE_ACTION) {
         if (entity != null && Global.player == entity) {
            GenericBiomeSpritePair spritePairData = biomeSpritePair.Find(_ => _.biomeType == biomeType);
            VoyageTriggerPopup.self.enableVoyageGUI(true, spritePairData == null ? null : spritePairData.sprite);
            VoyageTriggerPopup.self.voyageStatusConfirm.onClick.RemoveAllListeners();
            VoyageTriggerPopup.self.voyageStatusConfirm.onClick.AddListener(() => {
               VoyageTriggerPopup.self.enableVoyageGUI(false);
               if (canActivateTrigger(entity)) {
                  if (actions.TryGetValue(actionName, out Action<NetEntity> action)) {
                     action.Invoke(entity);
                  }
               } else {
                  D.debug("Cannot active this trigger");
               }
            });
            return;
         }
      } else {
         if (entity != null && interactionType == InteractionType.Enter && actionName != WARP_TO_LEAGUE_ACTION && canActivateTrigger(entity)) {
            if (actions.TryGetValue(actionName, out Action<NetEntity> action)) {
               action.Invoke(entity);
            }
         }
      }
   }

   private void OnTriggerExit2D (Collider2D collision) {
      NetEntity entity = collision.GetComponent<NetEntity>();
      if (entity != null && Global.player == entity) {
         withinRenderBounds = false;
      }

      // Warping to league uses GUI button to trigger
      if (actionName == WARP_TO_LEAGUE_ACTION) {
         VoyageTriggerPopup.self.enableVoyageGUI(false);
         return;
      }
      if (entity != null && interactionType == InteractionType.Exit && canActivateTrigger(entity)) {
         if (actions.TryGetValue(actionName, out Action<NetEntity> action)) {
            action.Invoke(entity);
         }
      }
   }

   private void OnTriggerStay2D (Collider2D collision) {
      NetEntity entity = collision.GetComponent<NetEntity>();
      float distanceBetweenPlayer = entity == null ? 0 : Vector2.Distance(transform.position, entity.transform.position);

      if (entity != null && (interactionType == InteractionType.Stay || (interactionType == InteractionType.Enter && actionName == WARP_TO_LEAGUE_ACTION))) {
         // Warping to league uses GUI button to trigger
         if (actionName != WARP_TO_LEAGUE_ACTION) {
            if (canActivateTrigger(entity)) {
               if (actions.TryGetValue(actionName, out Action<NetEntity> action)) {
                  action.Invoke(entity);
               }
            }
         }

         /* Old method where sprite displays in world
         if (distanceBetweenPlayer < INTERACT_DIST) {
            // Warping to league uses GUI button to trigger
            if (actionName == WARP_TO_LEAGUE_ACTION) {
               return;
            }

            if (canActivateTrigger(entity)) {
               if (actions.TryGetValue(actionName, out Action<NetEntity> action)) {
                  action.Invoke(entity);
               }
            }
         } else {
            if (distanceBetweenPlayer < VISIBLITY_DIST && actionName == WARP_TO_LEAGUE_ACTION) {
               withinRenderBounds = true;
            }
         }*/
      }
   }

   public void triggerAction () {
      if (Global.player == null) {
         return;
      }
      string message = "";
      float distanceBetweenPlayer = Vector2.Distance(transform.position, Global.player.transform.position);
      if (distanceBetweenPlayer < INTERACT_DIST) {
         if (canActivateTrigger(Global.player)) {
            if (actions.TryGetValue(actionName, out Action<NetEntity> action)) {
               action.Invoke(Global.player);
               return;
            }
         } else {
            message = "User cannot activate this!";
         }
      } else {
         message = "Too far away!";
      }

      Vector3 pos = Global.player.transform.position + new Vector3(0f, .32f);
      GameObject messageCanvas = Instantiate(PrefabsManager.self.warningTextPrefab);
      messageCanvas.transform.position = pos;
      messageCanvas.GetComponentInChildren<TextMeshProUGUI>().text = message;
   }

   private bool canActivateTrigger (NetEntity entity) {
      bool hasAlreadyTriggeredThisFrame = false;

      // Ignore the trigger if it has already been activated this frame
      if (_lastTriggerTime.TryGetValue(entity, out float lastActivationTime)) {
         if (Time.time - lastActivationTime < 0.1f) {
            hasAlreadyTriggeredThisFrame = true;
         }
      }

      _lastTriggerTime[entity] = Time.time;

      return !hasAlreadyTriggeredThisFrame;
   }

   #region Private Variables

   // The collider, which triggers the action
   private BoxCollider2D _collider;

   // The last time an entity has activated the trigger
   private Dictionary<NetEntity, float> _lastTriggerTime = new Dictionary<NetEntity, float>();

   #endregion
}