using System;
using System.Collections;
using System.Collections.Generic;
using MapCreationTool;
using UnityEngine;

namespace MapCustomization
{
   public class PropIconCamera : ClientMonoBehaviour
   {
      #region Public Variables

      // How many pixels are in one length unit of a tile
      public const int PIXELS_PER_TILE = 16;

      // Were all required icons loaded already
      public bool fullyLoaded = false;

      #endregion

      private IEnumerator Start () {
         _cam = GetComponent<Camera>();

         // Get all entries from asset serialization maps
         AssetSerializationMaps.ensureLoaded();
         foreach (KeyValuePair<int, GameObject> indexPref in AssetSerializationMaps.allBiomes.indexToPrefab) {
            // Only prefabs with CustomizablePrefab component can be placed
            CustomizablePrefab cPref = indexPref.Value.GetComponent<CustomizablePrefab>();
            if (cPref != null) {
               yield return CO_RenderPrefab(indexPref.Key, cPref);
            }
         }

         fullyLoaded = true;
      }

      private IEnumerator CO_RenderPrefab (int serializationId, CustomizablePrefab prefab) {
         Vector2Int pixelSize = prefab.size * PIXELS_PER_TILE;
         Vector3 renderSpot = new Vector3(0, -5000f, 0);

         // Set camera and texture settings
         RenderTexture renTex = new RenderTexture(pixelSize.x, pixelSize.y, 16);
         renTex.filterMode = FilterMode.Point;
         _cam.targetTexture = renTex;
         _cam.orthographicSize = prefab.size.y * 0.5f * 0.16f;
         _cam.transform.position = renderSpot - Vector3.forward * 10f;

         // Instantiate the target prefab
         CustomizablePrefab prefInstance = Instantiate(prefab, renderSpot, Quaternion.identity);

         // Render the image and turn into texture
         _isCurrentlyRendering = true;
         _cam.Render();

         yield return new WaitWhile(() => _isCurrentlyRendering);

         // Copy the rendered texture and store it
         RenderTexture prev = RenderTexture.active;
         RenderTexture.active = renTex;
         Texture2D result = new Texture2D(renTex.width, renTex.height, TextureFormat.ARGB32, false);
         result.filterMode = FilterMode.Point;
         result.anisoLevel = 1;
         result.ReadPixels(new Rect(0, 0, renTex.width, renTex.height), 0, 0);
         result.Apply();

         // Clean up
         GL.Clear(false, true, new Color(0, 0, 0, 0));
         RenderTexture.active = prev;
         renTex.Release();
         Destroy(prefInstance.gameObject);

         // Cache the create sprite
         _prefabIcons.Add(serializationId, Sprite.Create(result, new Rect(Vector2.zero, pixelSize), Vector2.one * 0.5f));
      }

      private void OnPostRender () {
         _isCurrentlyRendering = false;
      }

      public Sprite getIcon (int serializationId) {
         if (_prefabIcons.TryGetValue(serializationId, out Sprite sprite)) {
            return sprite;
         }

         D.error("We do not have an icon rendered for prefab with serialization ID: " + serializationId);
         return null;
      }

      #region Private Variables

      // Is camera currently rendering
      private bool _isCurrentlyRendering = false;

      // Icons for prefabs, indexed by prefab serialization id
      private Dictionary<int, Sprite> _prefabIcons = new Dictionary<int, Sprite>();

      // Our target camera
      private Camera _cam;

      #endregion
   }
}
