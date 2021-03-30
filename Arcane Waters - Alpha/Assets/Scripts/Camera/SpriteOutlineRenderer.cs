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

   private void OnEnable () {
      if (CameraManager.self != null) {
         CameraManager.self.resolutionChanged += updateQuadSize;
      }

      updateQuadSize();
      updateRenderBuffer();
   }

   private void OnDisable () {
      if (CameraManager.self != null) {
         CameraManager.self.resolutionChanged -= updateQuadSize;
      }

      removeCommandBuffer();
   }

   private void updateQuadSize () {
      _renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
      _renderTexture.filterMode = FilterMode.Point;

      _quadRenderer.material.SetTexture("_MainTex", _renderTexture);
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

   private void Update () {
      Camera cam = getCurrentCamera();

      // Make sure our quad always fills the entire screen
      float orthographicSize = cam.orthographicSize;

      Vector3 scale = transform.localScale;

      scale.y = orthographicSize * 2 * cam.rect.height;
      scale.x = orthographicSize * 2 * cam.rect.width * Screen.width / Screen.height;

      transform.localScale = scale;

      updatePixelSize();

      if (_currentOutline != null && _currentOutline.didRendererStateChange()) {
         removeCommandBuffer();
         updateRenderBuffer();
      }
   }

   private void LateUpdate () {
      // Our quad should always be at the same Z position as the outlined object so it doesn't render in front of everything
      if (_currentOutline != null) {
         Vector3 quadPos = _quadRenderer.transform.position;
         quadPos.z = _currentOutline.transform.position.z - OUTLINE_Z_OFFSET;
         _quadRenderer.transform.position = quadPos;
      }
   }

   private void removeCommandBuffer () {
      foreach (KeyValuePair<Camera, CommandBuffer> cam in _cameras) {
         if (cam.Key) {
            cam.Key.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, cam.Value);
         }
      }

      _cameras.Clear();      
   }

   public void onWillRenderObject (SpriteOutline outline) {
      // Currently outlined gameobject will call this method
      if (_currentOutline != null && outline == _currentOutline) {
         _quadRenderer.material.SetColor(_outlineColorPropertyID, _currentOutline.getColor());
         updateRenderBuffer();
      }
   }

   private void OnWillRenderObject () {
      // Only use the quad's OnWillRenderObject if no other object is currently being outlined.
      // The reason is we may end up drawing the outline of a sprite before the sprite itself is rendered (or updated, in case of animations), causing the outline not to match the real sprite.
      // We still need to update the buffer if no sprite is selected so we clear the RenderTexture.
      if (_currentOutline == null) {
         updateRenderBuffer();
      }
   }

   public void updateRenderBuffer () {
      Camera cam = getCurrentCamera();

      // Make sure we only add the buffer to the camera once!
      if (cam == null || _cameras.ContainsKey(cam)) {
         return;
      }

      _quadRenderer.transform.SetParent(cam.transform, false);

      Vector3 pos = _quadRenderer.transform.localPosition;
      pos.x = 0;
      pos.y = 0;
      _quadRenderer.transform.localPosition = pos;

      _outlinesBuffer = new CommandBuffer();
      _outlinesBuffer.name = "Draw Outlines Buffer";
      _cameras[cam] = _outlinesBuffer;

      // Tell our command buffer to use the texture we created as the render target
      _outlinesBuffer.SetRenderTarget(_renderTexture);

      // Make sure the texture is completely clear before we render to it
      _outlinesBuffer.ClearRenderTarget(true, true, Color.clear);

      if (_currentOutline != null) {         
         // Draw all the sprites of our SpriteOutline to the texture using the "silhouette only" shader
         SpriteRenderer[] renderers = _currentOutline.getRenderers();

         foreach (SpriteRenderer rend in renderers) {
            if (SpriteOutline.isRendererVisible(rend)) {
               _outlinesBuffer.DrawRenderer(rend, rend.material);
            }
         }
      }

      // Add the command buffer to the pipeline after transparents are rendered
      cam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _outlinesBuffer);
   }

   public void setOutlinedSprite (SpriteOutline sprite) {
      if (_currentOutline != sprite) {
         // Let the current outline know it's no longer visible
         if (_currentOutline != null) {
            _currentOutline.setVisibility(false);
         }

         _currentOutline = sprite;
         removeCommandBuffer();
      }
   }

   #region Private Variables

   // The sprite we're currently outlining
   [SerializeField]
   private SpriteOutline _currentOutline;

   // The quad we use for displaying the outline
   [SerializeField]
   private MeshRenderer _quadRenderer;

   // The texture to which we draw
   private RenderTexture _renderTexture;

   // The command buffer
   private CommandBuffer _outlinesBuffer;

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
