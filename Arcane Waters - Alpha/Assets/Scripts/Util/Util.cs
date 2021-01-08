using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using TMPro;
using System.Text;
using System.Xml;
using System.Globalization;
using UnityEngine.Events;
using MapCustomization;

public class Util : MonoBehaviour {
   public static Sprite getRawSpriteIcon(Item.Category category, int itemType) {
      if (category != Item.Category.None && itemType != 0) {
         string castItem = new Item { category = category, itemTypeId = itemType }.getCastItem().getIconPath();
         Sprite spriteCache = ImageManager.getSprite(castItem);
         return spriteCache;
      }

      return null;
   }

   public class BoolEvent : UnityEvent<bool> { }

   public static Sprite switchSpriteBiome (Sprite sprite, Biome.Type from, Biome.Type to) {
      try {
         string textureName = sprite.name.Substring(0, sprite.name.Length - 2).TrimEnd('_');
         string targetTextureName = textureName.Replace(from.ToString().ToLower(), to.ToString().ToLower());
         int spriteIndex = int.Parse(sprite.name.Substring(sprite.name.Length - 2).TrimStart('_'));

         return ImageManager.getSprites(@"Biomable/" + targetTextureName)[spriteIndex];
      } catch {
         return sprite;
      }
   }

   public static byte[] getTextureBytesForTransport (Texture2D texture) {      
      byte[] screenshotBytes = texture.EncodeToPNG();
      int maxPacketSize = Transport.activeTransport.GetMaxPacketSize();

      if (screenshotBytes.Length >= maxPacketSize) {
         // Skip every other row and column (no quality loss except minimap and fonts, because assets are using 200% scale)
         Texture2D skippedRowsTex = removeEvenRowsAndColumns(texture);
         screenshotBytes = skippedRowsTex.EncodeToPNG();
         if (screenshotBytes.Length >= maxPacketSize) {
            // Try to use texture with skipped rows/columns with lower resolution and quality (JPG)
            int quality = 100;
            while (quality > 0 && screenshotBytes.Length >= maxPacketSize) {
               skippedRowsTex = removeEvenRowsAndColumns(texture);
               screenshotBytes = skippedRowsTex.EncodeToJPG(quality);

               quality -= 5;
            }
         }
      }

      return screenshotBytes;
   }

   public static Texture2D removeEvenRowsAndColumns (Texture2D tex) {
      List<Color[]> listColors = new List<Color[]>();
      List<Color> finalColors = new List<Color>();

      for (int y = 0; y < tex.height; y += 2) {
         listColors.Add(tex.GetPixels(0, y, tex.width, 1));
      }

      for (int i = 0; i < listColors.Count; i++) {
         for (int x = 0; x < listColors[i].Length; x += 2) {
            finalColors.Add(listColors[i][x]);
         }
      }
      tex = new Texture2D(tex.width / 2, tex.height / 2);
      tex.SetPixels(finalColors.ToArray());

      return tex;
   }

   public static Texture2D getScreenshot () {
      #if UNITY_EDITOR
         if (!Application.isPlaying) {
            D.error("Screenshots can only be taken in playmode.");
         }
      #endif

      // Prepare data
      int width = Screen.width;
      int height = Screen.height;
      Camera camera = Camera.main;
      Canvas canvasGUI = CameraManager.self.guiCanvas;

      // Use battle camera if player is currently in battle
      if (BattleCamera.self.GetComponent<Camera>() && BattleCamera.self.GetComponent<Camera>().enabled) {
         camera = BattleCamera.self.GetComponent<Camera>();
      }

      RenderTexture savedCameraRT = camera.targetTexture;
      RenderTexture savedActiveRT = RenderTexture.active;
      RenderMode cameraRenderMode = canvasGUI.renderMode;
      Camera canvasWorldCamera = canvasGUI.worldCamera;
      float planeDistance = canvasGUI.planeDistance;

      // Temporary change render mode of Canvas to "Screen Space - Camera" to enable UI capture in screenshot
      if (cameraRenderMode != RenderMode.ScreenSpaceCamera) {
         canvasGUI.renderMode = RenderMode.ScreenSpaceCamera;
         canvasGUI.worldCamera = camera;
         canvasGUI.planeDistance = camera == Camera.main ? 1.0f : -2.0f;
      }

      // Create render texture, assign and render to it
      RenderTexture rt = new RenderTexture(width, height, 24);
      camera.targetTexture = rt;
      Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
      camera.Render();

      // Revert changes in Canvas
      canvasGUI.renderMode = cameraRenderMode;
      canvasGUI.worldCamera = canvasWorldCamera;
      canvasGUI.planeDistance = planeDistance;

      // Read pixels to Texture2D
      RenderTexture.active = rt;
      screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);

      // Cleanup
      camera.targetTexture = savedCameraRT;
      RenderTexture.active = savedActiveRT;
      Destroy(rt);

      return screenShot;
   }

   public static bool hasValidEntryName (string entryName) {
      if (entryName.ToLower() == "none" || entryName.ToLower() == "undefined") {
         return false;
      }
      return true;
   }

   public static string getItemName (Item.Category category, int typeID) {
      string itemTypeName = "";
      switch (category) {
         case Item.Category.CraftingIngredients:
            itemTypeName = ((CraftingIngredients.Type) typeID).ToString();
            break;
         case Item.Category.Armor:
            itemTypeName = EquipmentXMLManager.self.getArmorDataBySqlId(typeID).equipmentName;
            break;
         case Item.Category.Blueprint:
            // TODO: Ensure this is never called
            UnityEngine.Debug.LogWarning("Deprecated Call");
            break;
         case Item.Category.Weapon:
            itemTypeName = EquipmentXMLManager.self.getWeaponData(typeID).equipmentName;
            break;
         case Item.Category.Usable:
            itemTypeName = ((UsableItem.Type) typeID).ToString();
            break;
         case Item.Category.Hats:
            HatStatData hatData = EquipmentXMLManager.self.getHatData(typeID);
            if (hatData != null) {
               itemTypeName = hatData.equipmentName;
            }
            break;
         case Item.Category.Quest_Item:
            itemTypeName = ((QuestItem.Type) typeID).ToString();
            break;
         default:
            itemTypeName = "None";
            break;
      }
      return itemTypeName;
   }

   public static Type getItemType (Item.Category category) {
      Type newType = null;
      switch (category) {
         case Item.Category.Armor:
            // TODO: Clearly wipe out potential scripts that may call this
            UnityEngine.Debug.LogWarning("Deprecated Call: " + category);
            break;
         case Item.Category.Hats:
            // TODO: Clearly wipe out potential scripts that may call this
            UnityEngine.Debug.LogWarning("Deprecated Call: " + category);
            break;
         case Item.Category.CraftingIngredients:
            newType = typeof(CraftingIngredients.Type);
            break;
         case Item.Category.Weapon:
            // TODO: Clearly wipe out potential scripts that may call this
            UnityEngine.Debug.LogWarning("Deprecated Call: " + category);
            break;
         case Item.Category.Usable:
            newType = typeof(UsableItem.Type);
            break;
         case Item.Category.Quest_Item:
            newType = typeof(QuestItem.Type);
            break;
         default:
            newType = null;
            break;
      }
      return newType;
   }

   public static bool isEmpty (String str) {
      return (str == null || str.Equals(""));
   }

   public static Vector3 getMousePos () {
      // Cache a reference to the main camera if it doesn't exist
      if (_mainCamera == null) {
         _mainCamera = Camera.main;
      }

      Vector3 worldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
      worldPos.z = 0f;
      return worldPos;
   }

   public static void setXY (Transform transform, Vector3 newPosition) {
      Vector3 vector = transform.position;
      vector.x = newPosition.x;
      vector.y = newPosition.y;
      transform.position = vector;
   }

   public static void setZ (Transform transform, float newZ) {
      Vector3 vector = transform.position;
      vector.z = newZ;
      transform.position = vector;
   }

   public static void setLocalX (Transform transform, float newX) {
      transform.localPosition = new Vector3(
          newX,
          transform.localPosition.y,
          transform.localPosition.z
      );
   }

   public static void setLocalY (Transform transform, float newY) {
      transform.localPosition = new Vector3(
          transform.localPosition.x,
          newY,
          transform.localPosition.z
      );
   }

   public static void setLocalXY (Transform transform, Vector3 newPosition) {
      Vector3 vector = transform.localPosition;
      vector.x = newPosition.x;
      vector.y = newPosition.y;
      transform.localPosition = vector;
   }

   public static float angle (Vector2 vector) {
      float angle = Vector2.Angle(Vector2.up, vector);

      if (vector.x < 0) {
         angle = 360f - angle;
      }

      return angle;
   }

   public static float angle (Vector2 vec1, Vector2 vec2) {
      float angle = Vector2.Angle(vec2, vec1);

      if (vec1.x < 0) {
         angle = 360f - angle;
      }

      return angle;
   }

   public static float lerpDouble (double start, double end, double value) {
      // This is exactly like Mathf.Lerp, but takes doubles as parameters
      return (float)(start + (end - start) * value);
   }

   public static float inverseLerpDouble (double start, double end, double value) {
      // This is exactly like Mathf.InverseLerp, but takes doubles as parameters
      return (float)((value - start) / (end - start));
   }

   public static Direction getFacing (float angle) {
      if (angle <= 30 || angle >= 330) {
         return Direction.North;
      } else if (angle >= 30 && angle <= 150) {
         return Direction.East;
      } else if (angle >= 150 && angle <= 210) {
         return Direction.South;
      } else if (angle >= 210 && angle <= 330) {
         return Direction.West;
      }

      // Default
      return Direction.East;
   }

   public static float getAngle (Direction facing) {
      switch (facing) {
         case Direction.North:
            return 0;
         case Direction.NorthEast:
            return 45f;
         case Direction.East:
            return 90f;
         case Direction.SouthEast:
            return 135f;
         case Direction.South:
            return 180f;
         case Direction.SouthWest:
            return 225f;
         case Direction.West:
            return 270f;
         case Direction.NorthWest:
            return 315f;
      }

      return 0f;
   }

   public static Direction getFacingWithDiagonals (float angle) {
      if (angle <= 22.5 || angle >= 337.5) {
         return Direction.North;
      } else if (angle >= 22.5 && angle <= 67.5) {
         return Direction.NorthEast;
      } else if (angle >= 67.5 && angle <= 112.5) {
         return Direction.East;
      } else if (angle >= 112.5 && angle <= 157.5) {
         return Direction.SouthEast;
      } else if (angle >= 157.5 && angle <= 202.5) {
         return Direction.South;
      } else if (angle >= 202.5 && angle <= 257.5) {
         return Direction.SouthWest;
      } else if (angle >= 257.5 && angle <= 292.5) {
         return Direction.West;
      } else if (angle >= 292.5 && angle <= 337.5) {
         return Direction.NorthWest;
      }

      // Default
      return Direction.East;
   }

   public static Vector2 getDirectionFromFacing(Direction facing) {
      switch (facing) {
         case Direction.North:
            return Vector2.up;
         case Direction.NorthEast:
            return Vector2.up + Vector2.right;
         case Direction.East:
            return Vector2.right;
         case Direction.SouthEast:
            return Vector2.right + Vector2.down;
         case Direction.South:
            return Vector2.down;
         case Direction.SouthWest:
            return Vector2.down + Vector2.left;
         case Direction.West:
            return Vector2.left;
         case Direction.NorthWest:
            return Vector2.left + Vector2.up;
      }
      return Vector2.right;
   }

   public static double maxDouble (double a, double b) {
      return a > b ? a : b;
   }

   public static T clamp<T> (T value, T min, T max)
          where T : System.IComparable<T> {
      T result = value;
      if (value.CompareTo(max) > 0)
         result = max;
      if (value.CompareTo(min) < 0)
         result = min;
      return result;
   }

   public static void setScale (Transform transform, float newScale) {
      transform.localScale = new Vector3(newScale, newScale, transform.localScale.z);
   }

   public static void setAlphaInShader (GameObject gameObject, float alpha, string propertyName = "_Color") {
      SpriteRenderer[] renderers = gameObject.GetComponentsInChildren<SpriteRenderer>(true);
      
      foreach (SpriteRenderer rend in renderers) {
         Color color = rend.material.GetColor(propertyName);
         color.a = alpha;
         rend.material.SetColor(propertyName, color);
      }
   }

   public static void setAlpha (SpriteRenderer spriteRenderer, float alpha) {
      Color color = spriteRenderer.color;
      alpha = Mathf.Clamp(alpha, 0f, 1f);
      spriteRenderer.color = new Color(color.r, color.g, color.b, alpha);
   }

   public static void setAlpha (Text text, float alpha) {
      if (text == null) {
         return;
      }

      Color color = text.color;
      text.color = new Color(color.r, color.g, color.b, alpha);
   }

   public static void setAlpha (TextMeshProUGUI text, float alpha) {
      if (text == null) {
         return;
      }

      Color color = text.color;
      text.color = new Color(color.r, color.g, color.b, alpha);
   }

   public static void setAlpha (Image image, float alpha) {
      Color color = image.color;
      image.color = new Color(color.r, color.g, color.b, alpha);
   }

   public static void setAlpha (Material material, float alpha) {
      Color color = material.GetColor("_Color");
      material.SetColor("_Color", new Color(color.r, color.g, color.b, alpha));
   }

   public static void setMaterialBlockAlpha (SpriteRenderer renderer, float newAlpha) {
      MaterialPropertyBlock block = new MaterialPropertyBlock();

      // Assign our new alpha value
      Color color = Color.white;
      color.a = newAlpha;
      block.SetColor("_Color", color);

      if (renderer.sprite == null || renderer.sprite.texture == null) {
         return;
      }

      // Ensures that the texture is not lost during sprite change while animating
      block.SetTexture("_MainTex", renderer.sprite.texture);

      // Apply the edited values to the renderer
      renderer.SetPropertyBlock(block);
   }

   public static void setMaterialBlockTexture (SpriteRenderer renderer, Texture2D newTexture) {
      MaterialPropertyBlock block = new MaterialPropertyBlock();
      block.SetTexture("_MainTex", newTexture);
      renderer.SetPropertyBlock(block);
   }

   public static float getSinOfAngle (float angleInDegrees) {
      float sinOfAngle = Mathf.Sin((angleInDegrees * Mathf.PI) / 180);

      return sinOfAngle;
   }

   public static bool hasLandTile (Vector3 pos) {
      if (Global.player == null) {
         return false;
      }

      Area area = AreaManager.self.getArea(Global.player.areaKey);
      if (area == null) {
         return false;
      }

      Vector3Int cellPos = area.worldToCell(pos);

      foreach (TilemapLayer layer in area.getTilemapLayers()) {
         if (layer.name.ToLower().StartsWith("mountain")) {
            TileBase tile = layer.tilemap.GetTile(cellPos);

            if (tile != null) {
               return true;
            }
         }
      }

      return false;
   }

   public static Color getColor (int r, int g, int b) {
      return new Color((float) r / 255f, (float) g / 255f, (float) b / 255f);
   }

   public static Color getColor (int r, int g, int b, int a) {
      return new Color((float) r / 255f, (float) g / 255f, (float) b / 255f, (float) a / 255f);
   }

   public static bool isSelected (InputField inputField) {
      return (EventSystem.current.currentSelectedGameObject == inputField.gameObject);
   }

   public static void select (InputField inputField) {
      EventSystem.current.SetSelectedGameObject(inputField.gameObject);
      inputField.ActivateInputField();
   }

   public static void clickButton (Button button) {
      ExecuteEvents.Execute(button.gameObject, null, ExecuteEvents.submitHandler);
   }

   public static bool isServerBuild () {
      bool isServerBuild = false;

      #if IS_SERVER_BUILD
      isServerBuild = true;
      #endif

      return isServerBuild;
   }

   public static bool isForceServerLocalWithAutoDbconfig () {
      // return true; // Debug usage only
      return false;
   }

   public static bool isBatch () {
      // return true; // Debug usage only to simulate batch mode logic
      return Application.isBatchMode;
   }

   public static bool isBatchServer () {
      // In production, we always run the server in batch mode (no visuals) and automatically start up the server
      return Application.isBatchMode && CommandCodes.get(CommandCodes.Type.AUTO_SERVER);
   }

   public static void SetVisibility (GameObject go, bool vis) {
      foreach (var r in go.GetComponents<Renderer>()) {
         r.enabled = vis;
      }

      for (int i = 0; i < go.transform.childCount; i++) {
         var t = go.transform.GetChild(i);
         SetVisibility(t.gameObject, vis);
      }
   }

   public static string stripHTML (string source) {
      return Regex.Replace(source, @"<[^>]+>|&nbsp;", "");
   }

   public static bool isServerNonHost () {
      return (NetworkServer.active && !MyNetworkManager.isHost);
   }

   public static bool isPlayer (int userId) {
      if (Global.player != null && Global.player.userId > 0 && Global.player.userId == userId) {
         return true;
      }

      return false;
   }

   public static string getAppName () {
      Process p = Process.GetCurrentProcess();

      if (p != null && !p.HasExited) {
         return Path.GetFileName(p.ProcessName);
      }

      return "Unknown";
   }

   public static float getBellCurveFloat (float mean, float stdDev, float min, float max) {
      // Add 3 randomly generated floats together, each one between -1 and 1
      float sum = r.NextFloat(-1f, 1f) +
          r.NextFloat(-1f, 1f) +
          r.NextFloat(-1f, 1f);

      // Create our uniform random number using the specified mean and standard deviation
      float bellCurveRandom = (sum * stdDev) + mean;

      // Limit it to the specified min and max
      bellCurveRandom = Mathf.Clamp(bellCurveRandom, min, max);

      return bellCurveRandom;
   }

   public static int getBellCurveInt (float mean, float stdDev, float min, float max) {
      return (int) getBellCurveFloat(mean, stdDev, min, max);
   }

   public static Vector2 randFromCenter (float centerX, float centerY, float randomRadius) {
      return new Vector2(
         centerX + UnityEngine.Random.Range(-randomRadius, randomRadius),
         centerY + UnityEngine.Random.Range(-randomRadius, randomRadius)
      );
   }

   public static Vector3 pixelSnap (Vector3 pos) {
      float ppu = 100f * 2f;

      // Round the pixel value
      float nextX = Mathf.Round(ppu * pos.x);
      float nextY = Mathf.Round(ppu * pos.y);
      return new Vector3(
          nextX / ppu,
          nextY / ppu,
          pos.z
      );
   }

   public static Vector3 RandomPointInBounds (Bounds bounds) {
      return new Vector3(
          UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
          UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
          UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
      );
   }

   public static void activateVirtualCamera (CinemachineVirtualCamera virtualCamera) {
      // Make sure all other cameras are deactivated
      foreach (CinemachineVirtualCamera virtualCam in FindObjectsOfType<CinemachineVirtualCamera>()) {
         virtualCam.VirtualCameraGameObject.SetActive(false);
      }

      // And now we can activate
      virtualCamera.VirtualCameraGameObject.SetActive(true);
   }

   public static void tryToRunInServerBackground (Action action) {
      #if IS_SERVER_BUILD

      // If Unity is shutting down, we can't create new background threads
      if (ClientManager.isApplicationQuitting) {
         action();
         return;
      }

      // Otherwise, go ahead and run it in the background
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         action();
      });

      #endif
   }

   public static string removeNumbers (string input) {
      return Regex.Replace(input, @"[\d-]", string.Empty);
   }

   public static float TruncateTo100ths (float f) {
      return (float) Math.Truncate(f * 100) / 100;
   }

   public static float Truncate (float value, int digits=2) {
      string formatString = "0.";

      for (int i = 0; i < digits; i++) {
         formatString += "#";
      }

      return float.Parse(value.ToString(formatString));
   }

   public static void enableCanvasGroup (CanvasGroup canvasGroup) {
      canvasGroup.alpha = 1f;
      canvasGroup.interactable = true;
      canvasGroup.blocksRaycasts = true;
   }

   public static void disableCanvasGroup (CanvasGroup canvasGroup) {
      canvasGroup.alpha = 0f;
      canvasGroup.interactable = false;
      canvasGroup.blocksRaycasts = false;
   }

   public static string createSalt (string UserName) {
      #if IS_SERVER_BUILD
         Rfc2898DeriveBytes hasher = new Rfc2898DeriveBytes(UserName.ToLower(),
            System.Text.Encoding.Default.GetBytes("saltmZ8HxZEL7PTsalt"), 1000);
         return System.Convert.ToBase64String(hasher.GetBytes(25));
      #else
         return "";
      #endif
   }

   public static string hashPassword (string Salt, string Password) {
      #if IS_SERVER_BUILD
         Rfc2898DeriveBytes Hasher = new Rfc2898DeriveBytes(Password,
                  System.Text.Encoding.Default.GetBytes(Salt), 1000);
         return System.Convert.ToBase64String(Hasher.GetBytes(25));
      #else
         return "";
      #endif
   }

   public static string UppercaseFirst (string s) {
      // Check for empty string
      if (string.IsNullOrEmpty(s)) {
         return string.Empty;
      }
      // Return char and concat substring
      return char.ToUpper(s[0]) + s.Substring(1);
   }

   public static string[] serialize<T> (List<T> list) {
      List<string> stringList = new List<string>();

      foreach (T t in list) {
         stringList.Add(JsonUtility.ToJson(t));
      }

      return stringList.ToArray();
   }

   public static List<T> unserialize<T> (string[] stringArray) {
      List<T> list = new List<T>();

      foreach (string str in stringArray) {
         list.Add(JsonUtility.FromJson<T>(str));
      }

      return list;
   }

   public static Texture2D textureFromSprite (Sprite sprite) {
      if (sprite.rect.width != sprite.texture.width) {
         Texture2D newText = new Texture2D((int) sprite.rect.width, (int) sprite.rect.height);
         Color[] newColors = sprite.texture.GetPixels((int) sprite.textureRect.x,
                                                      (int) sprite.textureRect.y,
                                                      (int) sprite.textureRect.width,
                                                      (int) sprite.textureRect.height);
         newText.SetPixels(newColors);
         newText.Apply();
         return newText;
      } else
         return sprite.texture;
   }

   public static T randomEnum<T> () {
      T[] values = (T[]) Enum.GetValues(typeof(T));
      return values[r.Next(0, values.Length)];
   }

   public static T randomEnumStartAt<T> (int startingIndex) {
      T[] values = (T[]) Enum.GetValues(typeof(T));
      return values[r.Next(startingIndex, values.Length)];
   }

   public static List<T> getAllEnumValues<T> () {
      return new List<T>(Enum.GetValues(typeof(T)).Cast<T>());
   }

   public static bool isButtonClick () {
      // The chat scroll bar is kind of special since it's a click-and-hold
      if (ChatPanel.self.isScrolling()) {
         return true;
      }

      return GUIUtility.hotControl != 0 || EventSystem.current.IsPointerOverGameObject();
   }

   public static int roundToPrettyNumber (int num) {
      // We handle it differently based on how big it is
      if (num <= 10) {
         return num;
      } else if (num <= 100) {
         return num - (num % 2);
      } else if (num <= 1000) {
         return num - (num % 10);
      } else {
         int numDigits = Mathf.Abs(num).ToString().Length;

         return num - (num % (int) Mathf.Pow(10, numDigits - 3));
      }
   }

   public static int getCommandLineInt (string key) {
      foreach (string arg in Environment.GetCommandLineArgs()) {
         if (arg.Contains(key)) {
            string[] split = arg.Split('=');

            return int.Parse(split[1]);
         }
      }

      return 0;
   }

   public static bool isAutoStarting () {
      if (CommandCodes.get(CommandCodes.Type.AUTO_HOST) || CommandCodes.get(CommandCodes.Type.AUTO_TEST) || Global.startAutoHost) {
         return true;
      }

      return false;
   }

   public static bool isAutoTesting () {
      return CommandCodes.get(CommandCodes.Type.AUTO_TEST);
   }

   public static void readFastLoginFile () {
      // If the file doesn't exist or there is an error, fast login is disabled
      Global.isFastLogin = false;

      string path = Path.Combine(Application.dataPath, "fast_login.txt");

      // Read the fast_login file and populate the associated global variables
      StreamReader reader = null;
      try {
         FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
         reader = new StreamReader(fs, System.Text.Encoding.GetEncoding("UTF-8"));
         string line;

         // Host or client mode
         line = reader.ReadLine();
         if (line != null) {
            if (string.Equals("host", line)) {
               Global.isFastLoginHostMode = true;
            } else {
               Global.isFastLoginHostMode = false;
            }
         }

         // Account name
         line = reader.ReadLine();
         if (line != null) {
            Global.fastLoginAccountName = line;

            // Account password
            line = reader.ReadLine();
            if (line != null) {
               Global.fastLoginAccountPassword = line;

               // Character id - not mandatory
               line = reader.ReadLine();
               if (line != null) {
                  Global.fastLoginCharacterSpotIndex = Int32.Parse(line);

                  // Database server - not mandatory
                  line = reader.ReadLine();
                  if (line != null) {
                     #if IS_SERVER_BUILD
                     DB_Main.setServer(line);
                     #endif
                  }
               }

               Global.isFastLogin = true;
            }
         }
      } catch (Exception) {
      } finally {
         if (reader != null) {
            reader.Close();
         }
      }
   }

   public static int compare (string a, string b) {
      if (String.IsNullOrEmpty(a) && String.IsNullOrEmpty(b)) {
         return 0;
      }
      if (String.IsNullOrEmpty(a)) {
         return b.Length;
      }
      if (String.IsNullOrEmpty(b)) {
         return a.Length;
      }
      int lengthA = a.Length;
      int lengthB = b.Length;
      var distances = new int[lengthA + 1, lengthB + 1];
      for (int i = 0; i <= lengthA; distances[i, 0] = i++) ;
      for (int j = 0; j <= lengthB; distances[0, j] = j++) ;

      for (int i = 1; i <= lengthA; i++)
         for (int j = 1; j <= lengthB; j++) {
            int cost = b[j - 1] == a[i - 1] ? 0 : 1;
            distances[i, j] = Math.Min
                (
                Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                distances[i - 1, j - 1] + cost
                );
         }
      return distances[lengthA, lengthB];
   }

   public static string getFrameNumber (Sprite sprite) {
      int length = sprite.name.Length;

      // We'll get the frame number as a string to keep this function efficient enough to call in LateUpdate()
      string frameNumber = sprite.name.Substring(length - 2);
      string cleanedFrameNumber = frameNumber.Replace("_", "");
      return cleanedFrameNumber;
   }

   // Truncates the specified float value to the requested number of digits
   public static float TruncateRounded (float value, int digits=2) {
      double mult = Math.Pow(10.0, digits);
      double result = Math.Truncate(mult * value) / mult;
      return (float) result;
   }

   // Returns -1 when to the left, 1 to the right, and 0 for forward/backward
   public static float AngleDirection (Vector3 fwd, Vector3 targetDir, Vector3 up) {
      Vector3 perp = Vector3.Cross(fwd, targetDir);
      float dir = Vector3.Dot(perp, up);

      if (dir > 0.0f) {
         return 1.0f;
      } else if (dir < 0.0f) {
         return -1.0f;
      } else {
         return 0.0f;
      }
   }

   // Returns the angle between the two vectors, in the range 0 to 360
   public static float AngleBetween (Vector3 fwd, Vector3 targetDir) {
      float angle = Vector3.Angle(fwd, targetDir);

      if (AngleDirection(fwd, targetDir, Vector3.forward) == -1) {
         return 360 - angle;
      } else {
         return angle;
      }
   }

   public static bool hasInputField (GameObject obj) {
      return (obj.HasComponent<InputField>() || obj.HasComponent<TMP_InputField>());
   }

   public static bool isGeneralInputAllowed () {
      return !(PanelManager.self.hasPanelInLinkedList() ||
         ChatPanel.self.inputField.isFocused ||
         ((MailPanel) PanelManager.self.get(Panel.Type.Mail)).isWritingMail() ||
         Global.player == null ||
         !AreaManager.self.hasArea(Global.player.areaKey) ||
         MapCustomizationManager.isCustomizing);
   }
   
   // Loads an XML text asset and deserializes it into an object
   public static T xmlLoad<T> (TextAsset textAsset) {
      StringReader reader = null;
      try {
         // Streams the xml string
         reader = new StringReader(textAsset.text);

         // Create an instance of the XMLSerializer
         XmlSerializer serializer = new XmlSerializer(typeof(T));

         // Deserialize the object
         T obj = (T) serializer.Deserialize(reader);

         // Return the result
         return obj;

      } catch (Exception e) {
         D.error("Error when loading the file " + textAsset.name + "\n" + e.ToString());
         return default(T);
      } finally {
         // Close the reader
         if (reader != null) {
            reader.Close();
         }
      }
   }
   public static T xmlLoad<T> (string textAsset) {
      StringReader reader = null;
      try {
         // Streams the xml string
         reader = new StringReader(textAsset);

         // Create an instance of the XMLSerializer
         XmlSerializer serializer = new XmlSerializer(typeof(T));

         // Deserialize the object
         T obj = (T) serializer.Deserialize(reader);

         // Return the result
         return obj;

      } catch (Exception e) {
         D.error("Error when loading the file " + textAsset + "\n" + e.ToString());
         return default(T);
      } finally {
         // Close the reader
         if (reader != null) {
            reader.Close();
         }
      }
   }

   public static string[] getFileNamesInFolder(string directoryPath, string searchPattern="*.*") {
      // Get the list of files in the directory
      DirectoryInfo dir = new DirectoryInfo(directoryPath);
      FileInfo[] info = dir.GetFiles(searchPattern);

      // Get the name of each file
      string[] fileNamesArray = new string[info.Length];
      for (int i = 0; i < info.Length; i++) {
         fileNamesArray[i] = info[i].Name;
      }
      return fileNamesArray;
   }

   public static int getGameVersion () {
      int gameVersion;

      // Load the cloud build manifest json file
      TextAsset manifestJSON = (TextAsset) Resources.Load("UnityCloudBuildManifest.json");
      
      if (manifestJSON != null) {
         // Deserialize the manifest
         CloudBuildManifest manifest = JsonUtility.FromJson<CloudBuildManifest>(manifestJSON.text);

         // Get the game version number from the manifest
         try {
            gameVersion = int.Parse(manifest.buildNumber);
         } catch (Exception e) {
            D.error("Could not parse the build number. Exception: " + e);
            gameVersion = 0;
         }
      } else {
         // If the manifest does not exist, set the game version to a high value to allow the connection to the server
         gameVersion = int.MaxValue;

         // If this is a cloud build, then the manifest cannot be missing
#if CLOUD_BUILD
         D.error("Could not find the cloud build manifest.");
         gameVersion = 0;
#endif
      }

      return gameVersion;
   }

   public static bool isSameIpAddress (string addressA, string addressB) {
      if ((addressA == "localhost" || addressA == "127.0.0.1") &&
          (addressB == "localhost" || addressB == "127.0.0.1")) {
         return true;
      } else {
         return addressA == addressB;
      }
   }
   
   public static string toTitleCase(string s) {
      // Capitalizes the first letter of each word
      return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLower());
   }

   public static void stopHostAndReturnToTitleScreen () {
      // Stop any client or server that may have been running
      MyNetworkManager.self.StopHost();

      // Activate the Title Screen camera
      Util.activateVirtualCamera(TitleScreen.self.virtualCamera);

      // Clear out our saved data
      Global.lastUsedAccountName = "";
      Global.lastUserAccountPassword = "";
      Global.currentlySelectedUserId = 0;

      // Clear the current area - if we reconnect to another server, the area position could be different
      MapManager.self.destroyLastMap();

      // Notice the tutorial
      TutorialManager3.self.onUserLogOut();
   }

   public static bool isWithinCone (Vector2 coneStart, Vector2 target, float coneMiddleAngle, float coneHalfAngle, float coneRadius = -1.0f) {
      // Checks if a target is within the range of a cone, defined by a middle angle and a half angle (Both in degrees), and an optional radius check

      // If we should check the cone's radius
      if (coneRadius > 0.0f) {
         if (Vector2.Distance(coneStart, target) > coneRadius) {
            return false;
         }
      }

      float angleToTarget = Util.angle(target - coneStart);
      return (Mathf.Cos((angleToTarget - coneMiddleAngle) * Mathf.Deg2Rad) > (Mathf.Cos(coneHalfAngle * Mathf.Deg2Rad)));
   }

   // A Random instance we can use for generating random numbers
   private static System.Random r = new System.Random();

   // A cached reference to the main camera
   private static Camera _mainCamera;
}