using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.EventSystems;

public class SpriteSelector : MonoBehaviour
{
   #region Public Variables

   // Is sprite selector initialized. Initialization is shared among all sprite selectors, only needs to be performed once for all.
   public static bool initialized { get; private set; }

   [Header("Path that is currently selected")]
   public string value;

   [Header("Set if selection must start at specified folder")]
   public string startInPath;

   [Header("Can you select in subfolders")]
   public bool allowSubfolders = true;

   [Space(10)]
   // Sprite that is shown when the target image is not found
   public Sprite defaultSprite;

   // Sprite for the folder, which leads to the parent folder
   public Sprite backFolderSprite;

   // Sprite for a folder
   public Sprite folderSprite;

   [Space(10)]
   // Image that is displaying the currently selected sprite
   public Image previewImage;

   // Objects that are shown when sprites are being selected
   public GameObject selectionTemplate;

   // Text that is shown on top of selection panel
   public Text selectionLabel;

   // Preview sprite that is shown during sprite selection
   public Image selectionPreviewImage;

   // Prefab of an item (folder/texture) placed during sprite selection
   public SpriteSelectorItem selectionItemTemplate;

   #endregion

   private void Start () {
      if (!initialized) {
         initialize();
      }

      selectionTemplate.SetActive(false);
      setValueWithoutNotify(value);

      _startFolder = getFolder(startInPath, false);
   }

   public void setValueWithoutNotify (string value) {
      if (!initialized) {
         initialize();
      }

      this.value = value;

      Sprite sprite = getSpriteOrDefault(value);
      previewImage.sprite = sprite;
      selectionPreviewImage.sprite = sprite;
   }

   public void openSelection () {
      if (selectionTemplate.activeSelf) return;

      selectionTemplate.SetActive(true);

      setSelectionFolder(applyConstraints(getFolder(value, true)));

      // At the beginning, set current value as selected item
      if (_currentFolder.texturePaths.Contains(value)) {
         foreach (SpriteSelectorItem item in selectionItemTemplate.transform.parent.GetComponentsInChildren<SpriteSelectorItem>()) {
            if (item.type == SpriteSelectorItem.Type.Texture && item.texturePath.Equals(value)) {
               item.setSelected(true);
            }
         }
      }
   }

   public void closeSelection () {
      selectionTemplate.SetActive(false);
   }

   private void setSelectionFolder (TextureFolder folder) {
      _currentFolder = folder;

      // Destroy previously placed items (NOTE: this will not destroy the template, because the template is inactive and will not be found)
      foreach (SpriteSelectorItem item in selectionItemTemplate.transform.parent.GetComponentsInChildren<SpriteSelectorItem>()) {
         Destroy(item.gameObject);
      }

      // Add the 'back' folder
      if (folder != _startFolder) {
         SpriteSelectorItem backFolder = Instantiate(selectionItemTemplate, selectionItemTemplate.transform.parent);
         backFolder.gameObject.SetActive(true);

         backFolder.icon.sprite = backFolderSprite;
         backFolder.label.text = "";
         backFolder.type = SpriteSelectorItem.Type.Back;
         backFolder.folder = folder.parent;
         backFolder.spriteSelector = this;
         backFolder.setSelected(false);
      }

      // Add folders
      // Populate with textures from the current folder
      if (allowSubfolders) {
         foreach (TextureFolder childFolder in folder.children) {
            SpriteSelectorItem item = Instantiate(selectionItemTemplate, selectionItemTemplate.transform.parent);
            item.gameObject.SetActive(true);

            item.icon.sprite = folderSprite;
            item.label.text = childFolder.name;
            item.type = SpriteSelectorItem.Type.Folder;
            item.folder = childFolder;
            item.spriteSelector = this;
            item.setSelected(false);
         }
      }

      // Populate with textures from the current folder
      foreach (string texturePath in folder.texturePaths) {
         SpriteSelectorItem item = Instantiate(selectionItemTemplate, selectionItemTemplate.transform.parent);
         item.gameObject.SetActive(true);

         item.icon.sprite = getSpriteOrDefault(texturePath);
         item.label.text = getTextureName(texturePath);
         item.type = SpriteSelectorItem.Type.Texture;
         item.texturePath = texturePath;
         item.spriteSelector = this;
         item.setSelected(false);
      }

      selectionLabel.text = folder.getPath();
   }

   public void selectionItemClick (SpriteSelectorItem clickedItem, PointerEventData data) {
      if (data.clickCount == 1) {
         // Set the item as selected
         foreach (SpriteSelectorItem item in selectionItemTemplate.transform.parent.GetComponentsInChildren<SpriteSelectorItem>()) {
            item.setSelected(item == clickedItem);
         }
         // If item is back folder, go back
         if (clickedItem.type == SpriteSelectorItem.Type.Back && clickedItem.folder != null) {
            setSelectionFolder(clickedItem.folder);
         }

         // If item is texture, set it's value
         if (clickedItem.type == SpriteSelectorItem.Type.Texture) {
            setValueWithoutNotify(clickedItem.texturePath);
         }
      } else {
         if (clickedItem.type == SpriteSelectorItem.Type.Texture) {
            setValueWithoutNotify(clickedItem.texturePath);
            closeSelection();
         } else {
            if (clickedItem.folder != null) {
               setSelectionFolder(clickedItem.folder);
            }
         }
      }
   }

   private TextureFolder applyConstraints (TextureFolder folder) {
      // Prevent folder from going out of bounds, according to given selection parameters

      bool hasParent = false;
      for (TextureFolder parent = folder; parent != null; parent = parent.parent) {
         if (parent == _startFolder) {
            hasParent = true;
            break;
         }
      }

      if (!hasParent) {
         return _startFolder;
      }

      if (!allowSubfolders && folder != _startFolder) {
         return _startFolder;
      }

      return folder;
   }

   private Sprite getSpriteOrDefault (string path) {
      Sprite sprite = ImageManager.getSprite(path);
      if (sprite == null || sprite == ImageManager.self.blankSprite) {
         sprite = defaultSprite;
      }
      return sprite;
   }

   private string getTextureName (string path) {
      string[] parts = path.Split('/');
      if (parts.Length == 0) {
         return "";
      }

      return parts[parts.Length - 1];
   }

   private TextureFolder getFolder (string path, bool isTexture) {
      string[] pathParts = path.Trim().Split('/');

      // We expect the texture to have at least a root folder
      if (pathParts.Length < (isTexture ? 2 : 1) || !_rootFolder.name.Equals(pathParts[0])) return _rootFolder;

      // Try to find the folder, based on all paths
      TextureFolder currentFolder = _rootFolder;
      for (int i = 1; i < pathParts.Length - (isTexture ? 1 : 0); i++) {
         bool found = false;
         foreach (TextureFolder nextFolder in currentFolder.children) {
            // Try to find the next folder in the tree
            if (nextFolder.name.Equals(pathParts[i])) {
               currentFolder = nextFolder;
               found = true;
               break;
            }
         }

         // If folder was not found, there is no such folder, return default
         if (!found) {
            return _rootFolder;
         }
      }

      return currentFolder;
   }

   #region Initialization

   private void initialize () {
      // NOTE: this rather expensive, consider optimizing if it becomes a problem
      initialized = false;

      // First of all, lets get all valid paths to all textures, without duplications
      HashSet<string> paths = new HashSet<string>();

      // Load all filepaths
      TextAsset[] filepaths = Resources.LoadAll<TextAsset>("Filepaths");

      // Turn filepaths into valid texture paths
      foreach (TextAsset filepath in filepaths) {
         // Check that texture exists
         string path = filepath.text.Replace("Assets/Resources/", "");

         Texture texture = Resources.Load<Texture>(path);
         if (texture != null) {
            if (!paths.Contains(path)) {
               paths.Add(path);
            }

            // Unload texture - only need the path, we don't need the texture right now
            Resources.UnloadAsset(texture);
         }

         // Unload the filepath file
         Resources.UnloadAsset(filepath);
      }

      // Now with a list of all valid paths, we want to turn them into a traversal tree
      _rootFolder = new TextureFolder { name = "Sprites", children = new List<TextureFolder>(), parent = null, texturePaths = new List<string>() };
      foreach (string path in paths) {
         addTexturePathToFolderTree(path);
      }

      initialized = true;
   }

   private void addTexturePathToFolderTree (string path) {
      string[] pathParts = path.Split('/');

      // We expect the texture to have at least a root folder and a name
      if (pathParts.Length < 2) return;

      // Get the folder part of the path
      string targetFolderPath = string.Join("/", pathParts.Take(pathParts.Length - 1));

      // Make sure the beginning is the same as root folder
      if (!pathParts[0].Equals(_rootFolder.name)) return;

      // Find where to place the texture, creating folders as needed along the way
      int currentPathPart = 0;
      TextureFolder currentFolder = _rootFolder;
      while (currentPathPart < pathParts.Length - 2) {
         string nextFolderName = pathParts[currentPathPart + 1];

         // Check if folder exists, otherwise create it
         TextureFolder nextFolder = currentFolder.children.FirstOrDefault(f => f.name.Equals(nextFolderName));
         if (nextFolder == null) {
            nextFolder = new TextureFolder { name = nextFolderName, parent = currentFolder, children = new List<TextureFolder>(), texturePaths = new List<string>() };
            currentFolder.children.Add(nextFolder);
         }

         currentFolder = nextFolder;
         currentPathPart++;
      }

      // Once we have the target folder, we can just add the texture path to it
      currentFolder.texturePaths.Add(path);
   }

   #endregion

   #region Private Variables

   // The root texture folder - beginning of traversal tree
   private static TextureFolder _rootFolder;

   // Currently openned folder
   private TextureFolder _currentFolder;

   // If specified, allow selection only from this folder downwards
   private TextureFolder _startFolder;

   /// <summary>
   /// Used to make a traversal tree, which we use to find all textures in our project,
   /// and search for them by going in and out of folders
   /// </summary>
   public class TextureFolder
   {
      // The name of this folder
      public string name;

      // Parent folder
      public TextureFolder parent;

      // Children folders
      public List<TextureFolder> children;

      // Paths of textures that are inside this folder
      public List<string> texturePaths;

      public string getPath () {
         if (parent == null) {
            return name;
         }

         return parent.getPath() + "/" + name;
      }
   }

   #endregion
}