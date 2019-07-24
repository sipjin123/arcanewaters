using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEditor;

using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProceduralMap
{
    public class MapGenerator : EditorWindow
    {

        #region Public Variables

        #endregion

        #region Private Variables
#pragma warning disable 0649
        [SerializeField] MapGeneratorPreset[] _presets;
#pragma warning restore 0649
        #endregion

        #region Properties

        #endregion

        #region Unity Methods

        /// <summary>
        /// OnGUI is called for rendering and handling GUI events.
        /// This function can be called multiple times per frame (one call per event).
        /// </summary>
        void OnGUI()
        {
            //map presets
            ScriptableObject target = this;
            SerializedObject so = new SerializedObject(target);
            SerializedProperty tileLayerProperty = so.FindProperty("_presets");
            EditorGUILayout.PropertyField(tileLayerProperty, true); // True means show children
            so.ApplyModifiedProperties(); // Remember to apply modified properties

            bool enableButton = false;
            if (_presets.Length > 0)
            {
                if (_presets[0])
                {
                    enableButton = true;
                }
            }
            else
            {
                enableButton = false;
            }

            EditorGUI.BeginDisabledGroup(!enableButton);
            if (GUILayout.Button("Generate Map"))
            {
                GenerateMap();
            }
            EditorGUI.EndDisabledGroup();

        }

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        void OnEnable()
        {
            // Here we retrieve the data if it exists or we save the default field initialisers we set above
            var data = EditorPrefs.GetString("MapGenerator", JsonUtility.ToJson(this, false));
            // Then we apply them to this window
            JsonUtility.FromJsonOverwrite(data, this);
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled or inactive.
        /// </summary>
        void OnDisable()
        {
            // We get the Json data
            var data = JsonUtility.ToJson(this, false);
            // And we save it
            EditorPrefs.SetString("MapGenerator", data);
        }
        #endregion

        #region Public Methods

        [MenuItem("Window/Map Generator")]
        public static void ShowWindow()
        {
            GetWindow<MapGenerator>("Map Generator");
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Generate the map object
        /// </summary>
        void GenerateMap()
        {
            foreach (var preset in _presets)
            {
                GameObject prefab = new GameObject(preset.MapPrefixName + preset.MapName + preset.MapSuffixName);
                GameObject gridLayers = new GameObject("Grid Layers");
                gridLayers.transform.parent = prefab.transform;

                gridLayers.AddComponent(typeof(Grid));

                Rigidbody2D rigidbody2D = gridLayers.AddComponent(typeof(Rigidbody2D))as Rigidbody2D;
                rigidbody2D.bodyType = RigidbodyType2D.Static;

                CompositeCollider2D compositeCollider = gridLayers.AddComponent(typeof(CompositeCollider2D))as CompositeCollider2D;

                float[, ] noiseMap = GenerateNoiseMap(preset.mapSize, preset.seed, preset.noiseScale, preset.octaves, preset.persistance, preset.lacunarity, preset.offset);

                for (int i = 0; i < preset.layers.Length; i++)
                {
                    GameObject layerObject = new GameObject(preset.layers[i].name);
                    layerObject.transform.position = new Vector3(layerObject.transform.position.x, layerObject.transform.position.y, (float)(preset.layers.Length - i) / 100);
                    layerObject.transform.parent = gridLayers.transform;
                    Tilemap tilemap = layerObject.AddComponent(typeof(Tilemap))as Tilemap;
                    TilemapRenderer tilemapRenderer = layerObject.AddComponent(typeof(TilemapRenderer))as TilemapRenderer;
                    tilemapRenderer.sortOrder = TilemapRenderer.SortOrder.TopLeft;

                    if (preset.layers[i].useCollider)
                    {
                        TilemapCollider2D tilemapCollider = layerObject.AddComponent(typeof(TilemapCollider2D))as TilemapCollider2D;
                        tilemapCollider.usedByComposite = true;
                    }
                    
                    SetBaseLayers(preset, noiseMap, i, tilemap);
                    if (preset.layers[i].useBorderOnDiferentLayer)
                    {
                        GameObject borderLayerObject = new GameObject(preset.layers[i].name + preset.layers[i].BorderLayerName);
                        borderLayerObject.transform.position = new Vector3(borderLayerObject.transform.position.x, borderLayerObject.transform.position.y, (float)(preset.layers.Length - i) / 100);
                        borderLayerObject.transform.parent = gridLayers.transform;
                        Tilemap borderTilemap = borderLayerObject.AddComponent(typeof(Tilemap))as Tilemap;
                        TilemapRenderer borderTilemapRenderer = borderLayerObject.AddComponent(typeof(TilemapRenderer))as TilemapRenderer;
                        borderTilemapRenderer.sortOrder = TilemapRenderer.SortOrder.TopLeft;

                        //SetBorder(preset, i, tilemap, tilemap, true);
                        SetBorder(preset, i, tilemap, borderTilemap, false);
                    }
                    else
                    {
                        SetBorder(preset, i, tilemap, tilemap, false);
                    }

                }

                GeneratePrefab(preset.mapPath, prefab);
            }
        }
        /// <summary>
        /// set base tiles on the paramref name="tilemap"  using the paramref name="noiseMap"
        /// </summary>
        /// <param name="preset">preset</param>
        /// <param name="noiseMap">noise</param>
        /// <param name="i">index</param>
        /// <param name="tilemap">tilemap used</param>
        void SetBaseLayers(MapGeneratorPreset preset, float[, ] noiseMap, int i, Tilemap tilemap)
        {
            float[] layersHeight = null;

            if (!preset.usePresetLayerHeight)
            {
                layersHeight = CalculateLayerHeight(preset.layers.Length);
            }

            for (int y = 0; y < preset.mapSize.y; y++)
            {
                for (int x = 0; x < preset.mapSize.x; x++)
                {
                    float currentHeight = noiseMap[x, y];

                    if (preset.usePresetLayerHeight)
                    {
                        if (currentHeight <= preset.layers[i].height)
                        {
                            tilemap.SetTile(new Vector3Int(x, y, 0), preset.layers[i].tile);
                            //tilemap.SetTile(new Vector3Int(-x + preset.mapSize.x / 2, -y + preset.mapSize.y / 2, 0), preset.layers[i].tile);
                        }
                    }
                    else
                    {
                        if (currentHeight <= layersHeight[i])
                        {
                            tilemap.SetTile(new Vector3Int(x, y, 0), preset.layers[i].tile);
                            //tilemap.SetTile(new Vector3Int(-x + preset.mapSize.x / 2, -y + preset.mapSize.y / 2, 0), preset.layers[i].tile);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// use the paramref name="checkedTilemap" to find the borders and set on paramref name="setTilemap" 
        /// </summary>
        /// <param name="preset">preset</param>
        /// <param name="i">index</param>
        /// <param name="checkedTilemap">tilemap to check</param>
        /// <param name="setTilemap">tilemap to set</param>
        /// <param name="diferentLayerBorder">create new border for the layer</param>
        void SetBorder(MapGeneratorPreset preset, int i, Tilemap checkedTilemap, Tilemap setTilemap, bool diferentLayerBorder)
        {
            for (int x = 0; x < preset.mapSize.x; x++)
            {
                for (int y = 0; y < preset.mapSize.y; y++)
                {
                    Vector3Int cellPos = new Vector3Int(x, y, 0);

                    var tileSprite = checkedTilemap.GetSprite(cellPos);

                    if (tileSprite)
                    {
                        foreach (var border in preset.layers[i].borders)
                        {
                            switch (border.borderType)
                            {
                                //Four directions
                                case BorderType.allDirections:
                                    //                                                     right   left   up   down
                                    if (CheckExistingBorderDirections(checkedTilemap, x, y, false, false, false, false))
                                    {
                                        if (diferentLayerBorder)
                                        {
                                            setTilemap.SetTile(cellPos, null);
                                        }
                                        else
                                        {
                                            setTilemap.SetTile(cellPos, border.BorderTile);
                                        }
                                    }
                                    break;

                                    //Three directions
                                case BorderType.topLateral:
                                    if (CheckExistingBorderDirections(checkedTilemap, x, y, false, false, false, true))
                                    {
                                        if (diferentLayerBorder)
                                        {
                                            setTilemap.SetTile(cellPos, null);
                                        }
                                        else
                                        {
                                            setTilemap.SetTile(cellPos, border.BorderTile);
                                        }
                                    }
                                    break;

                                case BorderType.downLateral:
                                    if (CheckExistingBorderDirections(checkedTilemap, x, y, false, false, true, false))
                                    {
                                        if (diferentLayerBorder)
                                        {
                                            setTilemap.SetTile(cellPos, null);
                                        }
                                        else
                                        {
                                            setTilemap.SetTile(cellPos, border.BorderTile);
                                        }
                                    }
                                    break;

                                case BorderType.leftTopDown:
                                    if (CheckExistingBorderDirections(checkedTilemap, x, y, true, false, false, false))
                                    {
                                        if (diferentLayerBorder)
                                        {
                                            setTilemap.SetTile(cellPos, null);
                                        }
                                        else
                                        {
                                            setTilemap.SetTile(cellPos, border.BorderTile);
                                        }
                                    }
                                    break;

                                case BorderType.rightTopDown:
                                    if (CheckExistingBorderDirections(checkedTilemap, x, y, false, true, false, false))
                                    {
                                        if (diferentLayerBorder)
                                        {
                                            setTilemap.SetTile(cellPos, null);
                                        }
                                        else
                                        {
                                            setTilemap.SetTile(cellPos, border.BorderTile);
                                        }
                                    }
                                    break;

                                    //Two directions
                                case BorderType.topDown:
                                    if (CheckExistingBorderDirections(checkedTilemap, x, y, true, true, false, false))
                                    {
                                        if (diferentLayerBorder)
                                        {
                                            setTilemap.SetTile(cellPos, null);
                                        }
                                        else
                                        {
                                            setTilemap.SetTile(cellPos, border.BorderTile);
                                        }
                                    }
                                    break;

                                case BorderType.Lateral:
                                    if (CheckExistingBorderDirections(checkedTilemap, x, y, false, false, true, true))
                                    {
                                        if (diferentLayerBorder)
                                        {
                                            setTilemap.SetTile(cellPos, null);
                                        }
                                        else
                                        {
                                            setTilemap.SetTile(cellPos, border.BorderTile);
                                        }
                                    }
                                    break;

                                case BorderType.topLeft:
                                    if (CheckExistingBorderDirections(checkedTilemap, x, y, true, false, false, true))
                                    {
                                        if (diferentLayerBorder)
                                        {
                                            setTilemap.SetTile(cellPos, null);
                                        }
                                        else
                                        {
                                            setTilemap.SetTile(cellPos, border.BorderTile);
                                        }
                                    }
                                    break;

                                case BorderType.downLeft:
                                    if (CheckExistingBorderDirections(checkedTilemap, x, y, true, false, true, false))
                                    {
                                        if (diferentLayerBorder)
                                        {
                                            setTilemap.SetTile(cellPos, null);
                                        }
                                        else
                                        {
                                            setTilemap.SetTile(cellPos, border.BorderTile);
                                        }
                                    }
                                    break;

                                case BorderType.topRight:
                                    if (CheckExistingBorderDirections(checkedTilemap, x, y, false, true, false, true))
                                    {
                                        if (diferentLayerBorder)
                                        {
                                            setTilemap.SetTile(cellPos, null);
                                        }
                                        else
                                        {
                                            setTilemap.SetTile(cellPos, border.BorderTile);
                                        }
                                    }
                                    break;

                                case BorderType.downRight:
                                    if (CheckExistingBorderDirections(checkedTilemap, x, y, false, true, true, false))
                                    {
                                        if (diferentLayerBorder)
                                        {
                                            setTilemap.SetTile(cellPos, null);
                                        }
                                        else
                                        {
                                            setTilemap.SetTile(cellPos, border.BorderTile);
                                        }
                                    }
                                    break;

                                    //One direction
                                case BorderType.top:
                                    if (CheckExistingBorderDirections(checkedTilemap, x, y, true, true, false, true))
                                    {
                                        if (diferentLayerBorder)
                                        {
                                            setTilemap.SetTile(cellPos, null);
                                        }
                                        else
                                        {
                                            setTilemap.SetTile(cellPos, border.BorderTile);
                                        }
                                    }
                                    break;

                                case BorderType.down:
                                    if (CheckExistingBorderDirections(checkedTilemap, x, y, true, true, true, false))
                                    {
                                        if (diferentLayerBorder)
                                        {
                                            setTilemap.SetTile(cellPos, null);
                                        }
                                        else
                                        {
                                            setTilemap.SetTile(cellPos, border.BorderTile);
                                        }
                                    }
                                    break;

                                case BorderType.right:
                                    if (CheckExistingBorderDirections(checkedTilemap, x, y, false, true, true, true))
                                    {
                                        if (diferentLayerBorder)
                                        {
                                            setTilemap.SetTile(cellPos, null);
                                        }
                                        else
                                        {
                                            setTilemap.SetTile(cellPos, border.BorderTile);
                                        }
                                    }
                                    break;

                                case BorderType.left:
                                    if (CheckExistingBorderDirections(checkedTilemap, x, y, true, false, true, true))
                                    {
                                        if (diferentLayerBorder)
                                        {
                                            setTilemap.SetTile(cellPos, null);
                                        }
                                        else
                                        {
                                            setTilemap.SetTile(cellPos, border.BorderTile);
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// check if exist tiles around the tilemap
        /// </summary>
        /// <param name="checkedTilemap">tilemap to check</param>
        /// <param name="x">x position</param>
        /// <param name="y">y position</param>
        /// <param name="right">tile on the right</param>
        /// <param name="left">tile on the left</param>
        /// <param name="up">tile above</param>
        /// <param name="down">tile below</param>
        /// <returns></returns>
        bool CheckExistingBorderDirections(Tilemap checkedTilemap, int x, int y, bool right, bool left, bool up, bool down)
        {
            if (CheckExistingTile(checkedTilemap, x + 1, y) == right && CheckExistingTile(checkedTilemap, x - 1, y) == left && CheckExistingTile(checkedTilemap, x, y + 1) == up && CheckExistingTile(checkedTilemap, x, y - 1) == down)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// check if paramref name="tilemap" exist
        /// </summary>
        /// <param name="tilemap">tilemap to check</param>
        /// <param name="x">x position</param>
        /// <param name="y">y position</param>
        /// <returns></returns>
        bool CheckExistingTile(Tilemap tilemap, int x, int y)
        {
            if (tilemap.GetSprite(new Vector3Int(x, y, 0)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// automatically calculate the layer height
        /// </summary>
        /// <param name="layerLenght">number of layers</param>
        /// <returns></returns>
        float[] CalculateLayerHeight(int layerLenght)
        {
            List<float> layersHeight = null;

            layersHeight = new List<float>();

            float maxHeight = float.MinValue;
            float minHeight = float.MaxValue;

            for (int i = 0; i < layerLenght; i++)
            {
                layersHeight.Add(i + 1f);
                if (layersHeight[i] > maxHeight)
                {
                    maxHeight = layersHeight[i];
                }
                if (layersHeight[i] < minHeight)
                {
                    minHeight = layersHeight[i];
                }
            }

            for (int i = 0; i < layerLenght; i++)
            {
                layersHeight[i] = Mathf.InverseLerp(minHeight, maxHeight, layersHeight[i]);
            }

            layersHeight.Reverse();
            return layersHeight.ToArray();

        }
        
        /// <summary>
        /// generate the noise map
        /// </summary>
        /// <param name="mapSize">size</param>
        /// <param name="seed"></param>
        /// <param name="scale"></param>
        /// <param name="octaves"></param>
        /// <param name="persistance"></param>
        /// <param name="lacunarity"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public float[, ] GenerateNoiseMap(Vector2Int mapSize, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset)
        {
            float[, ] noiseMap = new float[mapSize.x, mapSize.y];

            System.Random pseudoRandom = new System.Random(seed);
            Vector2[] octaveOffsets = new Vector2[octaves];

            for (int i = 0; i < octaves; i++)
            {
                float offsetX = pseudoRandom.Next(-100000, 100000) + offset.x;
                float offsetY = pseudoRandom.Next(-100000, 100000) + offset.y;

                octaveOffsets[i] = new Vector2(offsetX, offsetY);
            }

            if (scale <= 0)
            {
                scale = .0001f;
            }

            float maxNoiseHeight = float.MinValue;
            float minNoiseHeight = float.MaxValue;

            float halfWidth = mapSize.x / 2;
            float halfHeight = mapSize.y / 2;

            for (int y = 0; y < mapSize.y; y++)
            {
                for (int x = 0; x < mapSize.x; x++)
                {
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;

                    for (int i = 0; i < octaves; i++)
                    {
                        float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x;
                        float sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].y;

                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= persistance;
                        frequency *= lacunarity;
                    }

                    if (noiseHeight > maxNoiseHeight)
                    {
                        maxNoiseHeight = noiseHeight;
                    }
                    else if (noiseHeight < minNoiseHeight)
                    {
                        minNoiseHeight = noiseHeight;
                    }

                    noiseMap[x, y] = noiseHeight;
                }
            }

            //normalize
            for (int y = 0; y < mapSize.y; y++)
            {
                for (int x = 0; x < mapSize.x; x++)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
                }
            }

            return noiseMap;
        }
        /// <summary>
        /// create the paramref name="gameObject" on the project
        /// </summary>
        /// <param name="path">path</param>
        /// <param name="gameObject">object</param>
        void GeneratePrefab(string path, GameObject gameObject)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                Directory.CreateDirectory(path);
            }
            //Set the path as within the Assets folder, and name it as the GameObject's name with the .prefab format
            string localPath = path + gameObject.name + ".prefab";

            PrefabUtility.SaveAsPrefabAsset(gameObject, localPath);
        }

        #endregion     
    }
}