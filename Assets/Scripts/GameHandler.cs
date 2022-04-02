using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;

public class GameHandler : MonoBehaviour {
    public static GameHandler Instance { get; private set; }

    [SerializeField] public bool godMode;
    [SerializeField] private bool showTerrainDebug;
    [SerializeField] private bool showCombatDebug;
    [SerializeField] private bool showAvailablePositionsDebug;
    [SerializeField] private TerrainVisual terrainVisual;
    [SerializeField] private CombatSystem gridCombatSystem;
    [SerializeField] private AvailablePositionsTilemapVisual AvailablePositionsTilemapVisual;
    private TerrainMap terrainTilemap;
    private AvailablePositionsTilemap availablePositionsTilemap;
    private Grid<CombatGridObject> combatGrid;
    private SaveAndLoadSystem saveAndLoadSystem;

    private int mapWidth = 20;
    private int mapHeight = 13;
    private float cellSize = 3f;
    private Vector3 origin = new Vector3(-30, -20, 0);

    private TerrainNode.TerrainType terrainType;


    private void Awake() {
        Instance = this;

        terrainTilemap = new TerrainMap(mapWidth, mapHeight, cellSize, origin, showTerrainDebug);
        availablePositionsTilemap = new AvailablePositionsTilemap(
                mapWidth, mapHeight, cellSize, origin, showAvailablePositionsDebug
            );
        combatGrid = new Grid<CombatGridObject>(
                mapWidth, mapHeight, cellSize, origin,
                (Grid<CombatGridObject> g, int x, int y) => new CombatGridObject(g, x, y), showCombatDebug
            );
        saveAndLoadSystem = new SaveAndLoadSystem();
    }


    void Start() {

        terrainTilemap.SetTerrainVisual(terrainVisual);
        availablePositionsTilemap.SetAvailablePositionsTilemapVisual(AvailablePositionsTilemapVisual);
    }

    void Update() {

        if (Input.GetKeyDown(KeyCode.N)) {
            terrainType = TerrainNode.TerrainType.Normal;
            Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
            terrainTilemap.SetTerrainType(mouseWorldPosition, terrainType);
        }

        if (Input.GetKeyDown(KeyCode.D)) {
            terrainType = TerrainNode.TerrainType.Difficult;
            Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
            terrainTilemap.SetTerrainType(mouseWorldPosition, terrainType);
        }

        if (Input.GetKeyDown(KeyCode.U)) {
            terrainType = TerrainNode.TerrainType.Unwalkable;
            Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
            terrainTilemap.SetTerrainType(mouseWorldPosition, terrainType);
        }

        if (Input.GetKeyDown(KeyCode.H)) {
            terrainType = TerrainNode.TerrainType.Sand;
            Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
            terrainTilemap.SetTerrainType(mouseWorldPosition, terrainType);
        }



        if (Input.GetKeyDown(KeyCode.S)) {
            saveAndLoadSystem.Save();
        }


        if (Input.GetKeyDown(KeyCode.L)) {
            saveAndLoadSystem.Load();
        }

    }

    public Grid<CombatGridObject> GetCombatGrid() {
        return combatGrid;
    }

    public float getCellSize() {
        return cellSize;
    }

    public int GetMapWidth() {
        return mapWidth;
    }

    public int GetMapHeight() {
        return mapHeight;
    }

    public CombatSystem GetCombatSystem() {
        return gridCombatSystem;
    }

    public TerrainMap GetTerrainTilemap() {
        return terrainTilemap;
    }

    public AvailablePositionsTilemap GetAvailablePositionsTilemap() {
        return availablePositionsTilemap;
    }

    public void HandleClickOnBoard() {
        gridCombatSystem.HandleClickOnGrid(UtilsClass.GetMouseWorldPosition());
    }

}
