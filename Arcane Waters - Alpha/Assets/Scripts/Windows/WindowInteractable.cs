using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class WindowInteractable : NetworkBehaviour
{
   #region Public Variables

   // The particle effect animations
   public SimpleAnimation curtainEffect, windowParticle;

   // The open curtain asset
   public GameObject closedCurtain;

   // The type of biome
   [SyncVar]
   public Biome.Type biomeType;

   // The instance that this obj is in
   [SyncVar]
   public int instanceId;

   // The id
   [SyncVar]
   public int id;

   // If is open
   [SyncVar]
   public bool isOpen;

   // If this is a large window
   public bool isLargeWindow;

   // The area key assigned to this ore
   [SyncVar]
   public string areaKey;

   // The sprite biome pair list
   public List<GenericBiomeSpritePair> openSpriteBiomeList, closeSpriteBiomeList;

   // The sprite renderer reference
   public SpriteRenderer openSpriteRenderer, closedSpriteRenderer;

   #endregion

   private void Start () {
      // Reset interaction
      if (isOpen) {
         openWindow();
      } else {
         closeWindow();
      }

      GenericBiomeSpritePair openBiomeReference = openSpriteBiomeList.Find(_ => _.biomeType == biomeType);
      if (openBiomeReference != null) {
         openSpriteRenderer.sprite = openBiomeReference.sprite;
      }

      GenericBiomeSpritePair closedBiomeReference = closeSpriteBiomeList.Find(_ => _.biomeType == biomeType);
      if (closedBiomeReference != null) {
         closedSpriteRenderer.sprite = closedBiomeReference.sprite;
      }

      // Make the node a child of the Area
      StartCoroutine(CO_SetAreaParent());
   }

   public void interactWindow () {
      if (Global.player != null && isGlobalPlayerNearby()) {
         Global.player.rpc.Cmd_InteractWindow(id);
      }
   }

   public bool isGlobalPlayerNearby () {
      if (Global.player == null) {
         return false;
      }

      return (Vector2.Distance(Global.player.transform.position, this.transform.position) <= .45f);
   }

   public void openWindow () {
      closedCurtain.SetActive(false);
      curtainEffect.gameObject.SetActive(true);
      windowParticle.gameObject.SetActive(true);
   }

   public void closeWindow () {
      closedCurtain.SetActive(true);
      curtainEffect.gameObject.SetActive(false);
      windowParticle.gameObject.SetActive(false);
   }

   private IEnumerator CO_SetAreaParent () {
      if (AreaManager.self != null) {
         // Wait until we have finished instantiating the area
         while (AreaManager.self.getArea(areaKey) == null) {
            yield return 0;
         }

         // Set as a child of the area
         Area area = AreaManager.self.getArea(this.areaKey);
         bool worldPositionStays = area.cameraBounds.bounds.Contains((Vector2) transform.position);
         setAreaParent(area, worldPositionStays);
         area.interactableWindows.Add(this);

         if (TryGetComponent(out ZSnap snap)) {
            snap.snapZ();
         } else {
            D.warning("Could not find ZSnap for window");
         }
      }
   }

   public void setAreaParent (Area area, bool worldPositionStays) {
      this.transform.SetParent(area.prefabParent, worldPositionStays);
   }

   #region Private Variables

   #endregion
}
