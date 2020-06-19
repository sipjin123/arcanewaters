﻿using UnityEngine;
using System.IO;
using System.Text;
using System;
using System.IO.Compression;

public static class GZipUtility
{
   public delegate void ProgressDelegate (string sMessage);

   public static void decompressToDirectory (string sCompressedFile, string sDir, ProgressDelegate progress) {
      using (FileStream inFile = new FileStream(sCompressedFile, FileMode.Open, FileAccess.Read, FileShare.None)) {
         using (GZipStream zipStream = new GZipStream(inFile, CompressionMode.Decompress, true)) {
            while (decompressFile(sDir, zipStream, progress)) ;
         }
      }
   }

   private static bool decompressFile (string sDir, GZipStream zipStream, ProgressDelegate progress) {
      //Decompress file name
      byte[] bytes = new byte[sizeof(int)];
      int Readed = zipStream.Read(bytes, 0, sizeof(int));
      if (Readed < sizeof(int)) {
         return false;
      }

      int iNameLen = BitConverter.ToInt32(bytes, 0);
      bytes = new byte[sizeof(char)];
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < iNameLen; i++) {
         zipStream.Read(bytes, 0, sizeof(char));
         char c = BitConverter.ToChar(bytes, 0);
         sb.Append(c);
      }
      string sFileName = sb.ToString();
      if (progress != null) {
         progress(sFileName);
      }

      //Decompress file content
      bytes = new byte[sizeof(int)];
      zipStream.Read(bytes, 0, sizeof(int));
      int iFileLen = BitConverter.ToInt32(bytes, 0);

      bytes = new byte[iFileLen];
      zipStream.Read(bytes, 0, bytes.Length);

      string sFilePath = Path.Combine(sDir, sFileName);
      string sFinalDir = Path.GetDirectoryName(sFilePath);
      if (!Directory.Exists(sFinalDir)) {
         Directory.CreateDirectory(sFinalDir);
      }

      using (FileStream outFile = new FileStream(sFilePath, FileMode.Create, FileAccess.Write, FileShare.None)) {
         outFile.Write(bytes, 0, iFileLen);
      }

      return true;
   }

   public static void compressDirectory (string sInDir, string sOutFile, ProgressDelegate progress) {
      string[] sFiles = Directory.GetFiles(sInDir, "*.*", SearchOption.AllDirectories);
      int iDirLen = sInDir[sInDir.Length - 1] == Path.DirectorySeparatorChar ? sInDir.Length : sInDir.Length + 0;
      using (FileStream outFile = new FileStream(sOutFile, FileMode.Create, FileAccess.Write, FileShare.None)) {
         using (GZipStream str = new GZipStream(outFile, CompressionMode.Compress)) {
            foreach (string sFilePath in sFiles) {
               string sRelativePath = sFilePath.Substring(iDirLen);
               if (progress != null) {
                  progress(sRelativePath);
               }
               compressFile(sInDir, sRelativePath, str);
            }
         }
      }
   }

   private static void compressFile (string sDir, string sRelativePath, GZipStream zipStream) {
      //Compress file name
      char[] chars = sRelativePath.ToCharArray();
      zipStream.Write(BitConverter.GetBytes(chars.Length), 0, sizeof(int));
      foreach (char c in chars) {
         zipStream.Write(BitConverter.GetBytes(c), 0, sizeof(char));
      }

      //Compress file content
      byte[] bytes = File.ReadAllBytes(Path.Combine(sDir, sRelativePath));
      zipStream.Write(BitConverter.GetBytes(bytes.Length), 0, sizeof(int));
      zipStream.Write(bytes, 0, bytes.Length);
   }
}