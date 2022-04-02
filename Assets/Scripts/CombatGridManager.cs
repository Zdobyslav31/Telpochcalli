using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombatGridManager {
    private int LAYER_Z_ALT = -3;

    public GameObject SpawnWarrior(GameObject warriorPrefab, Vector3 position) {
        CombatGridObject field = GetCombatGrid().GetGridObject(position);
        if(field.GetWarrior() || !GetTerrainMap().GetNode(field.x, field.y).isWalkable) {
            return null;
        }
        Quaternion rot = Quaternion.Euler(0, 0, 0);
        GameObject warrior = UnityEngine.Object.Instantiate(warriorPrefab, GetWorldPositionOnWarriorsLayer(field.x, field.y), rot);
        field.SetWarrior(warrior);
        return warrior;
    }

    public bool MoveWarriorOnGrid(GameObject activeWarrior, Vector3 targetPosition, Action onMoveComplete, out int distance, out List<TerrainNode> pathNodes) {
        if (IsMoveAvailable(targetPosition, activeWarrior, out pathNodes, out distance)) {
            Vector3[] pathCoords = new Vector3[pathNodes.Count];
            for (int i = 0; i < pathNodes.Count; i++) {
                pathCoords[i] = GetWorldPositionOnWarriorsLayer(pathNodes[i].x, pathNodes[i].y);
            }

            GetCombatGrid().GetGridObject(activeWarrior.transform.position).ClearWarrior();
            GetCombatGrid().GetGridObject(targetPosition).SetWarrior(activeWarrior);
            activeWarrior.GetComponent<WarriorUISystem>().StartMoveThroughPath(pathCoords, onMoveComplete);
            return true;
        } else
            return false;
    }

    public int UpdateAvailableMovePositions(GameObject activeWarrior) {
        int numberOfWalkableFields = 0;
        GetCombatGrid().GetGridPosition(activeWarrior.transform.position, out int unitX, out int unitY);

        GetAvailPosTilemap().ClearTilemap();
        int movePoints = activeWarrior.GetComponent<BaseWarrior>().GetMovePoints();

        for (int x = unitX - movePoints / 10; x <= unitX + movePoints / 10; x++) {
            for (int y = unitY - movePoints; y <= unitY + movePoints; y++) {
                if (IsMoveAvailable(x, y, activeWarrior, out List<TerrainNode> path, out int _)) {
                    numberOfWalkableFields++;
                    AvailablePositionsTilemapObject.TilemapSprite sprite = path.Last().isEndangered ? AvailablePositionsTilemapObject.TilemapSprite.MoveEndangered : AvailablePositionsTilemapObject.TilemapSprite.Move;
                    GetAvailPosTilemap().SetTilemapSprite(
                        x, y, sprite
                    );
                }
            }
        }
        return numberOfWalkableFields;
    }

    public void UpdateAvailableRotationPositions(GameObject activeWarrior) {

        GetCombatGrid().GetGridPosition(activeWarrior.transform.position, out int unitX, out int unitY);

        GetAvailPosTilemap().ClearTilemap();

        GetAvailPosTilemap().SetTilemapSprite(
            unitX, unitY + 1, AvailablePositionsTilemapObject.TilemapSprite.RotateN
        );
        GetAvailPosTilemap().SetTilemapSprite(
            unitX, unitY - 1, AvailablePositionsTilemapObject.TilemapSprite.RotateS
        );
        GetAvailPosTilemap().SetTilemapSprite(
            unitX + 1, unitY, AvailablePositionsTilemapObject.TilemapSprite.RotateE
        );
        GetAvailPosTilemap().SetTilemapSprite(
            unitX - 1, unitY, AvailablePositionsTilemapObject.TilemapSprite.RotateW
        );

    }


    public List<CombatGridObject> GetAvailableMeleeAttackTargetPositions(GameObject activeWarrior) {
        List<CombatGridObject> availableTargets = new List<CombatGridObject>();
        GetWarriorCoordinates(activeWarrior, out int activeWarriorX, out int activeWarriorY);
        List<CombatGridObject> adjacentFields = GetFieldsInFront(
            activeWarriorX, activeWarriorY, activeWarrior.GetComponent<WarriorUISystem>().direction);
        foreach (CombatGridObject field in adjacentFields) {
            if (
                    field.GetWarrior() != null
                    && field.GetWarrior().GetComponent<WarriorUISystem>().team != activeWarrior.GetComponent<WarriorUISystem>().team
                ) {
                availableTargets.Add(field);
            }
        }
        return availableTargets;
    }

    public List<CombatGridObject> GetAvailableRangedAttackTargetPositions(GameObject activeWarrior) {
        List<CombatGridObject> availableTargets = new List<CombatGridObject>();
        GetWarriorCoordinates(activeWarrior, out int warriorX, out int warriorY);
        GetTerrainMap().UpdateVisibleFields(warriorX, warriorY);
        foreach (CombatGridObject field in GetFieldsOccupiedByTeam(OppositeTeam(activeWarrior.GetComponent<WarriorUISystem>().team))) {
            if (GetTerrainMap().IsFieldVisible(field.x, field.y)) {
                availableTargets.Add(field);
            }
        }
        return availableTargets;
    }

    public bool IsMoveAvailable(Vector3 position, GameObject activeWarrior, out List<TerrainNode> movePath, out int distance) {
        GetCombatGrid().GetGridPosition(position, out int x, out int y);
        bool result = IsMoveAvailable(x, y, activeWarrior, out movePath, out distance);
        return result;
    }

    public void GetWarriorCoordinates(GameObject warrior, out int x, out int y) {
        GetCombatGrid().GetGridPosition(warrior.transform.position, out x, out y);
    }

    public List<CombatGridObject> GetFieldsInFront(CombatGridObject field, WarriorUISystem.Direction direction) {
        return GetFieldsInFront(field.x, field.y, direction);
    }

    public List<CombatGridObject> GetFieldsInFlank(CombatGridObject field, WarriorUISystem.Direction direction) {
        return GetFieldsInFlank(field.x, field.y, direction);
    }

    public List<CombatGridObject> GetFieldsInRear(CombatGridObject field, WarriorUISystem.Direction direction) {
        return GetFieldsInRear(field.x, field.y, direction);
    }

    public List<CombatGridObject> GetFieldsInControlZoneOfTeam(WarriorUISystem.Team team) {
        List<CombatGridObject> warriorsLocations = GetFieldsOccupiedByTeam(team);
        List<CombatGridObject> controlledFields = new List<CombatGridObject>();
        foreach (CombatGridObject warriorLocation in warriorsLocations) {
            controlledFields.AddRange(GetFieldsInControlZone(warriorLocation, warriorLocation.GetWarrior().GetComponent<WarriorUISystem>().direction));
        }
        return controlledFields;
    }

    private bool IsMoveAvailable(int x, int y, GameObject activeWarrior, out List<TerrainNode> movePath, out int distance) {
        movePath = null;
        distance = 0;
        if (GetCombatGrid().GetGridObject(x, y) == null) {
            // Destination outside board
            return false;
        }
        if (GetCombatGrid().GetGridObject(x, y).IsOccupied()) {
            // Destination already occupied by another warrior
            return false;
        }
        if (!GetTerrainMap().IsWalkable(x, y)) {
            // Destination is not walkable
            return false;
        }
        GetWarriorCoordinates(activeWarrior, out int activeWarriorX, out int activeWarriorY);
        List<TerrainNode> pathNodes = GetTerrainMap().FindPath(
                activeWarriorX, activeWarriorY, x, y, out distance,
                ToTerrainNodes(GetFieldsOccupiedByTeam(OppositeTeam(activeWarrior.GetComponent<WarriorUISystem>().team))),
                ToTerrainNodes(GetFieldsInControlZoneOfTeam(OppositeTeam(activeWarrior.GetComponent<WarriorUISystem>().team)))
            );
        if (pathNodes == null) {
            // No valid path
            return false;
        }
        if (distance > activeWarrior.GetComponent<BaseWarrior>().GetMovePoints()) {
            // Outside walking distance
            return false;
        }
        movePath = pathNodes;
        return true;
    }

    private WarriorUISystem.Team OppositeTeam(WarriorUISystem.Team team) {
        if (team == WarriorUISystem.Team.Heroes) {
            return WarriorUISystem.Team.Enemies;
        } else {
            return WarriorUISystem.Team.Heroes;
        }
    }

    private List<CombatGridObject> GetFieldsOccupiedByTeam(WarriorUISystem.Team team) {
        List<CombatGridObject> occupiedFields = new List<CombatGridObject>();
        foreach (GameObject warrior in GetCombatSystem().TeamMembers(team)) {
            occupiedFields.Add(GetCombatGrid().GetGridObject(warrior.transform.position));
        }
        return occupiedFields;
    }

    private List<CombatGridObject> GetFieldsInFront(int x, int y, WarriorUISystem.Direction direction) {
        List<CombatGridObject> fieldsList = new List<CombatGridObject>();
        switch (direction) {
            case WarriorUISystem.Direction.N:
                fieldsList.Add(GetCombatGrid().GetGridObject(x, y + 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y + 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y + 1));
                break;
            case WarriorUISystem.Direction.S:
                fieldsList.Add(GetCombatGrid().GetGridObject(x, y - 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y - 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y - 1));
                break;
            case WarriorUISystem.Direction.W:
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y));
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y + 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y - 1));
                break;
            case WarriorUISystem.Direction.E:
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y + 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y - 1));
                break;
        }
        fieldsList.RemoveAll(item => item is null);
        return fieldsList;
    }

    private List<CombatGridObject> GetFieldsInFlank(int x, int y, WarriorUISystem.Direction direction) {
        List<CombatGridObject> fieldsList = new List<CombatGridObject>();
        switch (direction) {
            case WarriorUISystem.Direction.N:
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y));
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y - 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y - 1));
                break;
            case WarriorUISystem.Direction.S:
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y));
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y + 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y + 1));
                break;
            case WarriorUISystem.Direction.W:
                fieldsList.Add(GetCombatGrid().GetGridObject(x, y - 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y - 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x, y + 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y + 1));
                break;
            case WarriorUISystem.Direction.E:
                fieldsList.Add(GetCombatGrid().GetGridObject(x, y - 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y - 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x, y + 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y + 1));
                break;
        }
        fieldsList.RemoveAll(item => item is null);
        return fieldsList;
    }

    private List<CombatGridObject> GetFieldsInRear(int x, int y, WarriorUISystem.Direction direction) {

        List<CombatGridObject> fieldsList = new List<CombatGridObject>();
        switch (direction) {
            case WarriorUISystem.Direction.N:
                fieldsList.Add(GetCombatGrid().GetGridObject(x, y - 1));
                break;
            case WarriorUISystem.Direction.S:
                fieldsList.Add(GetCombatGrid().GetGridObject(x, y + 1));
                break;
            case WarriorUISystem.Direction.W:
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y));
                break;
            case WarriorUISystem.Direction.E:
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y));
                break;
        }
        fieldsList.RemoveAll(item => item is null);
        return fieldsList;
    }

    private List<CombatGridObject> GetFieldsInControlZone(int x, int y, WarriorUISystem.Direction direction) {
        List<CombatGridObject> fieldsList = new List<CombatGridObject>();
        switch (direction) {
            case WarriorUISystem.Direction.N:
                fieldsList.Add(GetCombatGrid().GetGridObject(x, y + 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y + 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y + 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y));
                break;
            case WarriorUISystem.Direction.S:
                fieldsList.Add(GetCombatGrid().GetGridObject(x, y - 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y - 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y - 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y));
                break;
            case WarriorUISystem.Direction.W:
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y));
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y + 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y - 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x, y - 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x, y + 1));
                break;
            case WarriorUISystem.Direction.E:
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y + 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y - 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x, y - 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x, y + 1));
                break;
        }
        fieldsList.RemoveAll(item => item is null);
        return fieldsList;
    }

    private List<CombatGridObject> GetFieldsInControlZone(CombatGridObject field, WarriorUISystem.Direction direction) {
        return GetFieldsInControlZone(field.x, field.y, direction);
    }

    private Vector3 GetWorldPositionOnWarriorsLayer(int x, int y) {
        Vector3 cellOriginCoords = GetCombatGrid().GetWorldPosition(x, y);
        return cellOriginCoords + new Vector3(GameHandler.Instance.getCellSize() * .5f, GameHandler.Instance.getCellSize() * .5f, LAYER_Z_ALT);
    }

    private List<TerrainNode> ToTerrainNodes(List<CombatGridObject> gridObjects) {
        List<TerrainNode> terrainNodes = new List<TerrainNode>();
        foreach (CombatGridObject gridObject in gridObjects) {
            terrainNodes.Add(GetTerrainMap().GetGrid().GetGridObject(gridObject.x, gridObject.y));
        }
        return terrainNodes;
    }

    private Grid<CombatGridObject> GetCombatGrid() {
        return GameHandler.Instance.GetCombatGrid();
    }

    private TerrainMap GetTerrainMap() {
        return GameHandler.Instance.GetTerrainTilemap();
    }

    private AvailablePositionsTilemap GetAvailPosTilemap() {
        return GameHandler.Instance.GetAvailablePositionsTilemap();
    }

    private CombatSystem GetCombatSystem() {
        return GameHandler.Instance.GetCombatSystem();
    }
}

public class CombatGridObject {

    private Grid<CombatGridObject> grid;
    public int x;
    public int y;
    GameObject ocuppyingWarrior;

    public CombatGridObject(Grid<CombatGridObject> grid, int x, int y) {
        this.grid = grid;
        this.x = x;
        this.y = y;
    }

    public override string ToString() {
        if (ocuppyingWarrior != null) {
            return ocuppyingWarrior.name;
        } else {
            return x + "," + y;
        }
    }
    public bool IsOccupied() {
        return (ocuppyingWarrior != null);
    }

    public void SetWarrior(GameObject activeWarrior) {
        ocuppyingWarrior = activeWarrior;
        grid.TriggerGridObjectChanged(x, y);
    }

    public void ClearWarrior() {
        ocuppyingWarrior = null;
        grid.TriggerGridObjectChanged(x, y);
    }

    public GameObject GetWarrior() {
        return ocuppyingWarrior;
    }
}