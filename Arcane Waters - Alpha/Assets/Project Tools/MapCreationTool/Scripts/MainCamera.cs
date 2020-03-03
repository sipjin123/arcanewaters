using System;
using UnityEngine;

namespace MapCreationTool
{
   public class MainCamera : MonoBehaviour
   {
      public static event Action<Vector2Int> SizeChanged;

      public static MainCamera instance { get; private set; }
      public static Vector2Int size { get; private set; }

      [SerializeField]
      private float _minZoom = 0;
      [SerializeField]
      private float _maxZoom = 0;
      [SerializeField]
      private AnimationCurve _zoomSpeed = null;

      static Camera cam;
      static Rect camBounds = new Rect(new Vector2(-1000, -1000), new Vector2(2000, 2000));

      static float minZoom = 0f;
      static float maxZoom = 0f;
      static AnimationCurve zoomSpeed = null;

      private void Awake () {
         instance = this;
         cam = GetComponent<Camera>();
         size = new Vector2Int(cam.pixelWidth, cam.pixelHeight);

         minZoom = _minZoom;
         maxZoom = _maxZoom;
         zoomSpeed = _zoomSpeed;
      }

      private void Update () {
         if (cam.pixelWidth != size.x || cam.pixelHeight != size.y) {
            size = new Vector2Int(cam.pixelWidth, cam.pixelHeight);
            SizeChanged?.Invoke(size);

            clampCamPos();
         }
      }

      static void clampCamPos () {
         Vector2 camHalfSize = new Vector2(
             cam.orthographicSize * cam.pixelWidth / cam.pixelHeight,
             cam.orthographicSize);

         if (camHalfSize.x * 2 > camBounds.width || camHalfSize.y * 2 > camBounds.height) {
            cam.orthographicSize = Mathf.Min(
                camBounds.width * 0.5f * cam.pixelHeight / cam.pixelWidth,
                camBounds.height * 0.5f);

            camHalfSize = new Vector2(
            cam.orthographicSize * cam.pixelWidth / cam.pixelHeight,
            cam.orthographicSize);
         }

         cam.transform.localPosition = new Vector3(
                 Mathf.Clamp(
                     cam.transform.localPosition.x,
                     camBounds.x + camHalfSize.x,
                     camBounds.x + camBounds.width - camHalfSize.x),
                 Mathf.Clamp(
                     cam.transform.localPosition.y,
                     camBounds.y + camHalfSize.y,
                     camBounds.y + camBounds.height - camHalfSize.y),
                 cam.transform.localPosition.z);
      }

      public static void pan (Vector3 transition) {
         cam.transform.localPosition += transition;
         clampCamPos();
      }

      public static void zoom (float zoom) {
         cam.orthographicSize = Mathf.Clamp(cam.orthographicSize + zoom * zoomSpeed.Evaluate(cam.orthographicSize), minZoom, maxZoom);
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
