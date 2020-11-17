using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Rendering;
using System;

public class PostSpriteOutline : MonoBehaviour
{
   #region Public Variables

   // Self
   public static PostSpriteOutline self;

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      CameraManager.self.resolutionChanged += updateQuadSize;
      updateQuadSize();
      cleanup();
   }

   private void OnDestroy () {
      if (CameraManager.self != null) {
         CameraManager.self.resolutionChanged += updateQuadSize;
      }
   }

   private void updateQuadSize () {
      _renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
      _renderTexture.filterMode = FilterMode.Point;

      _quadRenderer.material.SetTexture("_MainTex", _renderTexture);
   }

   public void OnDisable () {      
   }

   public void OnEnable () {
      self = this;      
   }

   private void Update () {
      Camera cam = CameraManager.getCurrentCamera();

      // Make sure our quad always fills the entire screen
      float orthographicSize = cam.orthographicSize;

      Vector3 scale = transform.localScale;

      scale.y = orthographicSize * 2;
      scale.x = orthographicSize * 2 * Screen.width / Screen.height;

      transform.localScale = scale;
   }

   private void LateUpdate () {
      // Our quad should always be at the same Z position as the outlined object so it doesn't render in front of everything
      if (_currentOutline != null) {
         Vector3 quadPos = _quadRenderer.transform.position;
         quadPos.z = _currentOutline.transform.position.z;
         _quadRenderer.transform.position = quadPos;
      }
   }

   private void cleanup () {
      foreach (KeyValuePair<Camera, CommandBuffer> cam in _cameras) {
         if (cam.Key) {
            cam.Key.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, cam.Value);
         }
      }

      _cameras.Clear();

      updateRenderBuffer();
   }

   public void onWillRenderObject (ShaderSpriteOutline outline) {
      // Currently outlined gameobject will call this method
      if (_currentOutline != null && outline == _currentOutline) {
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
      Camera cam = CameraManager.getCurrentCamera();

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
            _outlinesBuffer.DrawRenderer(rend, rend.material);
         }
      }

      // Add the command buffer to the pipeline after transparents are rendered
      cam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _outlinesBuffer);
   }

   public void setOutlinedSprite (ShaderSpriteOutline sprite) {
      if (_currentOutline != sprite) {
         // Let the current outline know it's no longer visible
         if (_currentOutline != null) {
            _currentOutline.setVisibility(false);
         }

         _currentOutline = sprite;
         cleanup();
      }
   }

   public void removeOutlinedSprite (ShaderSpriteOutline sprite) {
      // Only remove the sprite if it's the one we're currently outlining
      if (sprite == _currentOutline) {
         StartCoroutine(CO_removeOutlinedSpriteDelayed());
      }      
   }

   private IEnumerator CO_removeOutlinedSpriteDelayed () {
      yield return null;

      _currentOutline = null;
      cleanup();
   }

   #region Private Variables

   // The sprite we're currently outlining
   [SerializeField]
   private ShaderSpriteOutline _currentOutline;

   // The shader that draws the outline of objects
   [SerializeField]
   private Shader _outlinesShader;
         
   // The quad we use for displaying the outline
   [SerializeField]
   private MeshRenderer _quadRenderer;

   // The texture to which we draw
   private RenderTexture _renderTexture;

   // The command buffer
   private CommandBuffer _outlinesBuffer;

   // All the cameras using the buffer
   private Dictionary<Camera, CommandBuffer> _cameras = new Dictionary<Camera, CommandBuffer>();

   #endregion
}
