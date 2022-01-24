using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Rendering;
using System;

public class SpriteOutlineRenderer : MonoBehaviour
{
   #region Public Variables

   #endregion

   private void Start () {
      _pixelSizePropertyID = Shader.PropertyToID("_PixelSize");
      _outlineColorPropertyID = Shader.PropertyToID("_OutlineColor");
   }

   private bool resizeRenderTexture () {
      if (_renderTexture == null || _renderTexture.width != Screen.width || _renderTexture.height != Screen.height) {
         _renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
         _renderTexture.filterMode = FilterMode.Point;
         _quadRenderer.material.SetTexture("_MainTex", _renderTexture);
         return true;
      }

      return false;
   }

   private void updatePixelSize () {
      // We'll update the pixel size using the new camera size
      int pixelSize = CameraManager.self != null ? CameraManager.getCurrentPPUScale() : DEFAULT_PIXEL_SIZE;

      // Make sure we have a valid pixelSize value
      pixelSize = pixelSize > 0 ? pixelSize : DEFAULT_PIXEL_SIZE;

      _quadRenderer.material.SetFloat(_pixelSizePropertyID, pixelSize);
   }

   private Camera getCurrentCamera () {
      if (CameraManager.self != null) {
         return CameraManager.getCurrentCamera();
      }

      return Camera.main;
   }

   private void removeCommandBuffer () {
      foreach (KeyValuePair<Camera, CommandBuffer> cam in _cameras) {
         if (cam.Key) {
            cam.Key.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, cam.Value);
         }
      }

      _cameras.Clear();
   }

   public void updateRenderBuffer (SpriteRenderer[] renderers) {
      Camera cam = getCurrentCamera();

      if (cam != null) {
         resizeRenderTexture();
         resizeQuad(cam);
         updatePixelSize();
         positionQuad(cam);
         createCommandBuffer(renderers, cam);
      }
   }

   private void createCommandBuffer (SpriteRenderer[] renderers, Camera cam) {
      if (_cameras.ContainsKey(cam)) {
         cam.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, _cameras[cam]);
      }

      CommandBuffer outlineCommandBuffer = new CommandBuffer();
      outlineCommandBuffer.name = "Draw Outlines Buffer";
      _cameras[cam] = outlineCommandBuffer;

      // Tell our command buffer to use the texture we created as the render target
      outlineCommandBuffer.SetRenderTarget(_renderTexture);

      // Make sure the texture is completely clear before we render to it
      outlineCommandBuffer.ClearRenderTarget(true, true, Color.clear);

      // Draw all the sprites of our SpriteOutline to the texture using the "silhouette only" shader
      foreach (SpriteRenderer rend in renderers) {
         if (SpriteOutline.isRendererVisible(rend)) {
            outlineCommandBuffer.DrawRenderer(rend, rend.material);
         }
      }

      // Add the command buffer to the pipeline after transparents are rendered
      cam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, outlineCommandBuffer);
   }

   private void positionQuad (Camera cam) {
      _quadRenderer.transform.SetParent(cam.transform, false);

      Vector3 pos = _quadRenderer.transform.localPosition;
      pos.x = 0;
      pos.y = 0;
      _quadRenderer.transform.localPosition = pos;
   }

   private void resizeQuad (Camera cam) {
      // Make sure our quad always fills the entire screen
      cam = getCurrentCamera();
      float orthographicSize = cam.orthographicSize;
      Vector3 scale = transform.localScale;
      scale.y = orthographicSize * 2 * cam.rect.height;
      scale.x = orthographicSize * 2 * cam.rect.width * Screen.width / Screen.height;
      transform.localScale = scale;
   }

   public void updateColor (Color newColor) {
      _quadRenderer.material.SetColor(_outlineColorPropertyID, newColor);
   }

   public void updateDepth (float depth) {
      Vector3 quadPos = _quadRenderer.transform.position;
      quadPos.z = depth - OUTLINE_Z_OFFSET;
      _quadRenderer.transform.position = quadPos;
   }

   public void reset () {
      removeCommandBuffer();

      if (_renderTexture != null) {
         _renderTexture.Release();
      }

      _renderTexture = null;
   }

   #region Private Variables

   // The quad we use for displaying the outline
   [SerializeField]
   private MeshRenderer _quadRenderer = default;

   // The texture to which we draw
   private RenderTexture _renderTexture = null;

   // All the cameras using the buffer
   private Dictionary<Camera, CommandBuffer> _cameras = new Dictionary<Camera, CommandBuffer>();

   // The property ID of the "_PixelSize" property
   private int _pixelSizePropertyID;

   // The property ID of the "_OutlineColor" property
   private int _outlineColorPropertyID;

   // The default pixel size if an invalid PPU scale is provided
   public const int DEFAULT_PIXEL_SIZE = 4;

   // The distance between the object and the outline in the Z axis
   public const float OUTLINE_Z_OFFSET = -0.0001f;

   #endregion
}
