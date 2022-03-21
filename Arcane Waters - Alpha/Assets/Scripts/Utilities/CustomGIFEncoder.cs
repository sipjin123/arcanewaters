using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

internal class CustomGIFEncoder : ProGifEncoder
{
   private CustomQuant nq;

   public void setQuantizer (CustomQuant q, byte[] palette) {
      nq = q;
      m_ColorTab = palette;
   }

   public CustomGIFEncoder (int repeat, int quality, int encoderIndex, Action<int> OnEncodingComplete)
      : base(repeat, quality, encoderIndex, OnEncodingComplete) { }

   /// <summary>
   /// Adds next GIF frame. The frame is not written immediately, but is actually deferred
   /// until the next frame is received so that timing data can be inserted. Invoking
   /// <code>Finish()</code> flushes all frames.
   /// </summary>
   /// <param name="frame">GifFrame containing frame to write.</param>
   public override void AddFrame (Frame frame) {
      if ((frame == null))
         throw new ArgumentNullException("Can't add a null frame to the gif.");

      if (!m_HasStarted)
         throw new InvalidOperationException("Call Start() before adding frames to the gif.");

      CustomFrame f = frame as CustomFrame;

      // Use first frame's size
      if (!m_IsSizeSet) SetSize(f.Width, f.Height);

      //		#if UNITY_EDITOR
      //		SetTransparencyColor(new Color32(49, 77, 121, 255)); //Hard code test
      //		m_AutoTransparent = true;
      //		#endif

      // Instead of GetImagePixels() we can assign it directly, because we made sure to do things in this format
      m_Pixels = f.Data;

      // We call a modified version of AnalyzePixels(), with a palette that's been made with all the frames together
      AnalyzePixels();

      if (m_IsFirstFrame) {
         WriteLSD();
         WritePalette();

         if (m_Repeat >= 0)
            WriteNetscapeExt();
      }

      WriteGraphicCtrlExt();
      WriteImageDesc();

      if (!m_IsFirstFrame)
         WritePalette();

      WritePixels();
      m_IsFirstFrame = false;
   }

   public override void AnalyzePixels () {
      // Analyzes image colors and creates color map.
      int len = m_Pixels.Length;
      int nPix = len / 3;
      m_IndexedPixels = new byte[nPix];

      // These two steps are replaced by a shared palette creation process
      //ProGifNeuQuant nq = new ProGifNeuQuant(m_Pixels, len, m_SampleInterval, m_HasTransparentPixel);
      //m_ColorTab = nq.Process(); // Create reduced palette

      // Map image pixels to new palette
      int k = 0, k0, k1, k2;
      bool chkTransparent = m_TransparentFlag == 1;
      byte tR = m_TransparentColor.r, tG = m_TransparentColor.g, tB = m_TransparentColor.b;
      byte b, g, r;
      for (int i = 0; i < nPix; i++) {
         k0 = k++; k1 = k++; k2 = k++;
         b = m_Pixels[k0]; g = m_Pixels[k1]; r = m_Pixels[k2];
         int index = nq.Map(b & 0xff, g & 0xff, r & 0xff);

         // Disable this, will not need
         //// Find m_TransparentColor in the Picture(m_Pixels), set the same Index as TransparencyColorIndex
         //if (chkTransparent) {
         //   if ((m_HasTransparentPixel && m_PixelsAlpha[i] == 0) ||
         //       (_transparentByColor && Mathf.Abs(tR - r) <= _transparentColorRange && Mathf.Abs(tG - g) <= _transparentColorRange && Mathf.Abs(tB - b) <= _transparentColorRange)) {
         //      index = m_TransparentColorIndex;
         //   }
         //}

         //m_UsedEntry[index] = true;
         m_IndexedPixels[i] = (byte) index;
      }

      m_Pixels = null;
      m_PixelsAlpha = null;
      m_ColorDepth = 8;
      m_PaletteSize = 7;
   }
}

internal class CustomFrame : Frame
{
   public new int Width;
   public new int Height;

   // All the pixels' r,g,b components
   public new byte[] Data;
}
