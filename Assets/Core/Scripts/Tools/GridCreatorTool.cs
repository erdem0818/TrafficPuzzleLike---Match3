using System.Collections.Generic;
using Core.Scripts.Gameplay.GridFolder;
using Core.Scripts.Gameplay.QuestObjects;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core.Scripts.Gameplay.StoneFolder;
using Direction = Core.Scripts.Gameplay.StoneFolder.Direction;

namespace Core.Scripts.Tools
{
    [CreateAssetMenu(menuName = "Tools/Grid Creator")]
    public class GridCreatorTool : SerializedScriptableObject
    {
        private const string AllStonesPath = "Assets/Core/Prefabs/Stone Prefabs";
        
        [Header("Grid Properties")]
        [SerializeField] private GridProperties gridProperties;
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private List<GameObject> gridPrefabs;
        
        [Header("All Usable Stones - Quest Objects")]
        public List<GameObject> allStones;
        
        [TableMatrix(HorizontalTitle = "Up --- Down --- Right --- Left", SquareCells = true, RespectIndentLevel = false)] 
        public GameObject[,] PlaceableStones;
        
        [TableMatrix(SquareCells = true,
            RespectIndentLevel = false, 
            ResizableColumns = false)] 
        public GameObject[,] Stones;
        
        [TableMatrix(DrawElementMethod = "DrawCell")]
        public bool[,] CustomCellDrawing;
        
        [OnInspectorInit]
        private void InitGrid()
        {
            Stones ??= new GameObject[gridProperties.width, gridProperties.height];
            CustomCellDrawing ??= new bool[gridProperties.width, gridProperties.height];
            CreatePool();
        }

        private void OnValidate()
        {
            CreatePool();
        }

        private void CreatePool()
        {
            var prefabsGuids = AssetDatabase.FindAssets("t:Prefab", new [] { AllStonesPath });
            allStones ??= new List<GameObject>();
            allStones.Clear();
            
            foreach (var guid in prefabsGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                allStones.Add(go);
            }

            int placeableHeight = allStones.Count / 4;
            PlaceableStones = new GameObject[4, placeableHeight + 1];
            int index = 0;
            for (int i = 0; i < PlaceableStones.GetLength(1); i++) // 2
            {
                for (int j = 0; j < PlaceableStones.GetLength(0); j++) // 4
                {
                    if (index >= allStones.Count) return;
                    var item = allStones[index];
                    if (item == null) continue;
                    PlaceableStones[j, i] = item;
                    index++;
                }
            }
        }
        
        [Button("Create Grid")]
        private void CreateGridBySize()
        {
            Stones = new GameObject[gridProperties.width, gridProperties.height];
            CustomCellDrawing = new bool[gridProperties.width, gridProperties.height];
        }
        
        [Button("Instantiate Grid In Scene")]
        private void InstantiateGridInScene()
        {
            int x = gridProperties.width;
            int y = gridProperties.height;
            Vector3Int startPos = Vector3Int.zero;

            GameObject grid = new GameObject("Grid Board")
            {
                transform =
                {
                    position = Vector3.zero
                }
            };
            
            EditorUtility.SetDirty(grid);
            
            Board puzzleGrid = grid.AddComponent<Board>();
            puzzleGrid.InitGrid(x, y);
            Vector3Int currentPos = startPos;

            for (int i = 0; i < y; i++)
            {
                int ct = i;
                for (int j = 0; j < x; j++)
                {
                    GameObject clone =
                        PrefabUtility.InstantiatePrefab(ct % 2 == 0 ? 
                                gridPrefabs[0] : 
                                gridPrefabs[1], 
                                grid.transform)
                            as GameObject;
                    
                    if(clone != null)
                        clone.name = $"Grid Part {j} {i}";
                    
                    GridPart part = clone.GetComponent<GridPart>();
                    part.SetPosition(currentPos.x, currentPos.z);
                    part.transform.localScale = new Vector3(1f, 0.2f, 1f) *
                                                gridProperties.scale;
                    part.IsEmpty = true;
                    puzzleGrid.SetCell(j, i, part);
                    currentPos.x += gridProperties.tileSize;
                    ct++;
                }
                
                currentPos.x = startPos.x;
                currentPos.z += gridProperties.tileSize;
            }
            grid.transform.position = new Vector3(-2.5f, 0f, -3.25f);
            CreateArrows(ref puzzleGrid);
            PlaceStonesAndQuestObjects(ref puzzleGrid);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        
        private void PlaceStonesAndQuestObjects(ref Board puzzleGrid)
        {
            GameObject goEntitiesParent = new GameObject()
            {
                transform = { position = Vector3.zero }
            };
            int x = gridProperties.width;
            int y = gridProperties.height;
            
            if(Stones == null)
            {
                Debug.Log("array null");
                return;
            }
            
            for (int i = y - 1; i >= 0; i--)
            {
                for (int j = 0; j < x; j++)
                {
                    if (Stones[j, i] == null)
                        continue;
                   
                    Stone stone = Stones[j, i].GetComponent<Stone>();
                    if (stone != null)
                    {
                        int correctX = j;
                        int correctY = gridProperties.height - i - 1;
                        
                        Stone stoneClone = PrefabUtility.InstantiatePrefab(stone) as Stone;
                        stoneClone.GridCoordinate = new Vector2Int(correctX, correctY);
                        GridPart part = puzzleGrid.GetCell(correctX, correctY);
                        part.stone = stoneClone;
                        part.IsEmpty = false;

                        stoneClone.transform.position = part.transform.position + (Vector3.up * 0.3f);
                        stoneClone.transform.SetParent(goEntitiesParent.transform);
                    }
                    
                    QuestObject questObject = Stones[j, i].GetComponent<QuestObject>();
                    if (questObject != null)
                    {
                        int correctX = j;
                        int correctY = gridProperties.height - i - 1;
                        QuestObject questClone = PrefabUtility.InstantiatePrefab(questObject) as QuestObject;
                        questClone.gridCoordinate = new Coord2D(correctX, correctY);
                        GridPart part = puzzleGrid.GetCell(correctX, correctY);
                        part.questObject = questClone;
                        part.IsEmpty = false;

                        questClone.transform.position = part.transform.position + (Vector3.up * 0.3f);
                        questClone.transform.SetParent(goEntitiesParent.transform);
                    }
                    
                }
            }
        }
        
        private void CreateArrows(ref Board puzzleGrid)
        {
            GameObject arrows = new GameObject("Arrows")
            {
                transform =
                {
                    position = Vector3.zero
                }
            };

            int w = puzzleGrid.Size.x;
            int h = puzzleGrid.Size.y;
            //bottom line
            for (int i = 0; i < w; i++)
            {
                var tempCellPos = puzzleGrid.GetCell(i, 0).GetGridPos;
                Vector3 tempArrowPos = new Vector3(tempCellPos.x, 0f, tempCellPos.y);
                tempArrowPos.z -= 1f;
                GameObject arrowClone = PrefabUtility.InstantiatePrefab(arrowPrefab, arrows.transform) as GameObject;
                //todo add comp and set direction here 
                var tempArrow = arrowClone.AddComponent<Arrow>();
                tempArrow.Direction = Direction.Up;
                tempArrow.RotateArrow();
                tempArrow.GridPosition = new Vector2Int(i, 0);
                arrowClone!.transform.position = tempArrowPos;
                tempArrow.CanPlaceable = CustomCellDrawing[i, gridProperties.height - 1];//reverse
                arrowClone.transform.GetChild(0).gameObject.SetActive(tempArrow.CanPlaceable);
            }
            
            //upper line
            for (int i = 0; i < w; i++)
            {
                var tempCellPos = puzzleGrid.GetCell(i, h - 1).GetGridPos;
                Vector3 tempArrowPos = new Vector3(tempCellPos.x, 0f, tempCellPos.y);
                tempArrowPos.z += 1f;
                GameObject arrowClone = PrefabUtility.InstantiatePrefab(arrowPrefab, arrows.transform) as GameObject;
                var tempArrow = arrowClone.AddComponent<Arrow>();
                tempArrow.Direction = Direction.Down;
                tempArrow.RotateArrow();
                tempArrow.GridPosition = new Vector2Int(i, h - 1);
                arrowClone!.transform.position = tempArrowPos;
                tempArrow.CanPlaceable = CustomCellDrawing[i, 0];
                arrowClone.transform.GetChild(0).gameObject.SetActive(tempArrow.CanPlaceable);
            }
            
            //left line
            for (int i = 0; i < h; i++)
            {
                var tempCellPos = puzzleGrid.GetCell(0, i).GetGridPos;
                Vector3 tempArrowPos = new Vector3(tempCellPos.x, 0f, tempCellPos.y);
                tempArrowPos.x -= 1f;
                GameObject arrowClone = PrefabUtility.InstantiatePrefab(arrowPrefab, arrows.transform) as GameObject;
                var tempArrow = arrowClone.AddComponent<Arrow>();
                tempArrow.Direction = Direction.Right;
                tempArrow.RotateArrow();
                tempArrow.GridPosition = new Vector2Int(0,i);
                arrowClone!.transform.position = tempArrowPos;
                tempArrow.CanPlaceable = CustomCellDrawing[0, gridProperties.height - i - 1];
                arrowClone.transform.GetChild(0).gameObject.SetActive(tempArrow.CanPlaceable);
            }
            //right line
            for (int i = 0; i < h; i++)
            {
                var tempCellPos = puzzleGrid.GetCell(w - 1, i).GetGridPos;
                Vector3 tempArrowPos = new Vector3(tempCellPos.x, 0f, tempCellPos.y);
                tempArrowPos.x += 1f;
                GameObject arrowClone = PrefabUtility.InstantiatePrefab(arrowPrefab, arrows.transform) as GameObject;
                var tempArrow = arrowClone.AddComponent<Arrow>();
                tempArrow.Direction = Direction.Left; 
                tempArrow.RotateArrow();
                tempArrow.GridPosition = new Vector2Int(w - 1, i);
                arrowClone!.transform.position = tempArrowPos;
                tempArrow.CanPlaceable = CustomCellDrawing[w - 1, gridProperties.height - i - 1];
                arrowClone.transform.GetChild(0).gameObject.SetActive(tempArrow.CanPlaceable);
            }

            arrows.transform.position = new Vector3(-2.5f, 0f, -3.25f);
        }
        
        public static bool DrawCell(Rect rect, bool value)
        {
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                value = !value;
                GUI.changed = true;
                Event.current.Use();
            }
            
            EditorGUI.DrawRect(rect.Padding(1), value ? Color.green : Color.blue);
            return value;
        }
    }
}
