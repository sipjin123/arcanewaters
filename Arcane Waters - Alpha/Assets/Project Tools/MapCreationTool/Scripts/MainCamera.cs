using System;
using UnityEngine;

namespace MapCreationTool
{
    public class MainCamera : MonoBehaviour
    {
        public static event Action<Vector2Int> SizeChanged;

        public static MainCamera Instance { get; private set; }
        public static Vector2Int Size { get; private set; }

        static Camera cam;

        private void Awake()
        {
            Instance = this;
            cam = GetComponent<Camera>();
            Size = new Vector2Int(cam.pixelWidth, cam.pixelHeight);
        }

        private void Update()
        {
            if (cam.pixelWidth != Size.x || cam.pixelHeight != Size.y)
            {
                Size = new Vector2Int(cam.pixelWidth, cam.pixelHeight);
                SizeChanged?.Invoke(Size);
            }
        }
    }
}
