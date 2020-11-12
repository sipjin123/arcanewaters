using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using System;
using System.Linq;

public class SecretEntranceHolder : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   // Information about targeted map, can be null if unset
   public Map targetInfo;

   // The facing direction we should have after spawning
   public Direction newFacingDirection = Direction.South;

   // The id of this node
   public int secretsId;

   // The area for this warp
   public string areaTarget;

   // The spawn for this warp
   public string spawnTarget;

   // The unique id for each secret entrance per instance id
   public int spawnId;

   // If the sprites can blend with the assets behind it
   public bool canBlend, canBlendInteract2;

   // The secret type
   public SecretType secretType;

   // The area key assigned to this node
   public string areaKey;

   // Determines if the object is interacted
   public bool isInteracted = false;

   // If the animation is finished
   public bool isFinishedAnimating;

   // List of secret entrance data for spawning
   public List<SecretsPrefabCollection> secretEntranceDataList;

   // Cached secret entrance
   public SecretEntrance cachedSecretEntrance;

   // Holds the spawnable secret obj
   public Transform secretObjHolder;

   // If the coroutine animation is running
   public bool isRunningCoroutineAnimation;

   // Closes the entrance after 30 seconds has passed
   public const int CLOSE_ENTRANCE_TIMER = 30;

   #endregion

   private void Start () {
      // Make the node a child of the Area
      StartCoroutine(CO_SetAreaParent());

      processSecretEntrance();
   }

   public void setAreaParent (Area area, bool worldPositionStays) {
      this.transform.SetParent(area.secretsParent, worldPositionStays);
   }

   private IEnumerator CO_SetAreaParent () {
      // Wait until we have finished instantiating the area
      while (AreaManager.self.getArea(areaKey) == null) {
         yield return 0;
      }

      // Set as a child of the area
      Area area = AreaManager.self.getArea(this.areaKey);
      bool worldPositionStays = area.cameraBounds.bounds.Contains((Vector2) transform.position);
      setAreaParent(area, worldPositionStays);
   }

   public void completeInteraction () {
      if (!isInteracted) {
         isInteracted = true;
         cachedSecretEntrance.processInteraction();
         StartCoroutine(CO_CloseEntrance());
      } 
   }

   private IEnumerator CO_CloseEntrance () {
      yield return new WaitForSeconds(CLOSE_ENTRANCE_TIMER);
      isInteracted = false;
      isFinishedAnimating = false;
      cachedSecretEntrance.warp.gameObject.SetActive(false);
      cachedSecretEntrance.closeEntrance();
      closeAnimation();
   }

   public void closeAnimation () {
      // Sends animation commands to all clients
      if (!isRunningCoroutineAnimation) {
         cachedSecretEntrance.closeEntrance();
      }
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         string value = field.v.Split(':')[0];
         switch (field.k.ToLower()) {
            case DataField.SECRETS_TYPE_ID:
               secretType = (SecretType) Enum.Parse(typeof(SecretType), value);
               secretsId = (int) secretType;
               processSecretEntrance();
               break;
            case DataField.SECRETS_CAN_BLEND:
               canBlend = field.v.ToLower() == "true";
               break;
            case DataField.SECRETS_CAN_BLEND_INTERACTED:
               canBlendInteract2 = field.v.ToLower() == "true";
               break;
            case DataField.WARP_TARGET_MAP_KEY:
               try {
                  string areaName = AreaManager.self.getAreaName(int.Parse(value));
                  areaTarget = areaName;
                  cachedSecretEntrance.warpAreaText.text = areaTarget;
               } catch {
                  D.editorLog("Cant get warp key: " + value, Color.red);
               }
               break;
            case DataField.WARP_TARGET_SPAWN_KEY:
               spawnTarget = value;
               break;
            case DataField.TARGET_MAP_INFO_KEY:
               targetInfo = field.objectValue<Map>();
               break;
            case DataField.WARP_ARRIVE_FACING_KEY:
               if (field.tryGetDirectionValue(out Direction dir)) {
                  newFacingDirection = dir;
               }
               break;
         }
      }

      // Assign data to the warp entity associated with this secret entrance
      if (cachedSecretEntrance != null) {
         if (cachedSecretEntrance.warp != null) {
            cachedSecretEntrance.warp.targetInfo = targetInfo;
            cachedSecretEntrance.warp.areaTarget = targetInfo.name;
            cachedSecretEntrance.warp.spawnTarget = spawnTarget;
         } else {
            D.debug("Secret entrance has no warp!");
         }
      } else {
         D.debug("Secret entrance is not Cached!");
      }
   }

   private void processSecretEntrance () {
      if (!_hasReceivedMapData && secretType != SecretType.None && cachedSecretEntrance == null) {
         GameObject secretObjVariant = Instantiate(secretEntranceDataList.Find(_ => _.secretType == secretType).secretPrefabVariant, secretObjHolder);
         SecretEntrance secretEntrance = secretObjVariant.GetComponent<SecretEntrance>();
         cachedSecretEntrance = secretEntrance;
         secretEntrance.secretEntranceHolder = this;
         secretEntrance.enabled = true;
         _hasReceivedMapData = true;
      }
   }

   #region Private Variables

   // If this map entity has received its data
   private bool _hasReceivedMapData = false;

   #endregion
}