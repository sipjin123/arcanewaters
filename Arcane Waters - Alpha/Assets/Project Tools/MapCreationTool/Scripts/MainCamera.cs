using System;
using UnityEngine;

namespace MapCreationTool
{
   public class MainCamera : MonoBehaviour
   {
      public static event Action<Vector2Int> SizeChanged;

      public static MainCamera instance { get; private set; }
      public static Vector2Int size { get; private set; }

      static Camera cam;

      private void Awake () {
         instance = this;
         cam = GetComponent<Camera>();
         size = new Vector2Int(cam.pixelWidth, cam.pixelHeight);
      }

      private void Update () {
         if (cam.pixelWidth != size.x || cam.pixelHeight != size.y) {
            size = new Vector2Int(cam.pixelWidth, cam.pixelHeight);
            SizeChanged?.Invoke(size);
         }
      }

      /// <summary>
      /// Screen to world point
      /// </summary>
      /// <param name="screenPoint"></param>
      /// <returns></returns>
      public static Vector3 stwp (Vector3 screenPoint) {
         return cam.ScreenToWorldPoint(screenPoint);
      }
   }
}
