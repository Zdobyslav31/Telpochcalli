using CodeMonkey.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCombatSystem : MonoBehaviour {
    private int LAYER_Z_ALT = -3;
    private int FLANK_ATTACK_PRE_DAMAGE_DISRUPTION = 10;
    private int REAR_ATTACK_PRE_DAMAGE_DISRUPTION = 10;
    private enum BattleState {
        Busy,
        Idle
    }
    public enum Phase {
        SettingUp,
        Movement,
        Rotation,
        Action
    }
    private BattleState battleState;
    public Phase phase;
    private BaseWarrior.Action selectedAction;
    private bool isActionSelected;

    [SerializeField] private int activeWarriorIndex;
    [SerializeField] private List<GameObject> warriors;
    [SerializeField] private GameObject changePhaseButton;
    [SerializeField] private GameObject selectActionPanel;

    private struct ActionDetails {
        public bool requiresTarget;
        public Func<List<CombatGridObject>> GetAvailableTargetPositions;
        public Action<CombatGridObject> PerformAction;
        public ActionDetails(Func<List<CombatGridObject>> getAvailableTargetPositions, Action<CombatGridObject> performAction) {
            this.requiresTarget = true;
            this.GetAvailableTargetPositions = getAvailableTargetPositions;
            this.PerformAction = performAction;
        }
        public ActionDetails(Action<CombatGridObject> performAction) {
            this.requiresTarget = false;
            this.GetAvailableTargetPositions = null;
            this.PerformAction = performAction;
        }
    }
    private Dictionary<BaseWarrior.Action, ActionDetails> actionDetailsDict;

    private void Start() {
        battleState = BattleState.Idle;
        phase = Phase.Movement; // TODO: change to SettingUp when implemented
        foreach (GameObject warrior in warriors) {
            Vector3 warriorPosition = warrior.transform.position;
            GetCombatGrid().GetGridObject(warriorPosition).SetWarrior(warrior);
        }
        actionDetailsDict = new Dictionary<BaseWarrior.Action, ActionDetails>() {
            { BaseWarrior.Action.Regroup, new ActionDetails(PerformRegroup)},
            { BaseWarrior.Action.Attack, new ActionDetails(
                    GetAvailableMeleeAttackTargetPositions, PerformMeleeAttack
                )
            },
            { BaseWarrior.Action.Shoot, new ActionDetails(
                    GetAvailableRangedAttackTargetPositions, PerformRangedAttack
                )
            },
            { BaseWarrior.Action.Charge, new ActionDetails(
                    GetAvailableMeleeAttackTargetPositions, PerformMeleeAttack
                )
            },
        };
        activeWarriorIndex = 0;
        isActionSelected = false;
        UpdateAvailablePositions();
        GetActiveWarriorUISystem().SetActiveIndicatorEnabled(true);
    }

    private void Update() {

    }

    public void HandleClickOnGrid(Vector3 targetPosition) {
        switch (phase) {
            case Phase.SettingUp:
                break;
            case Phase.Movement:
                MoveActiveWarriorOnGrid(targetPosition);
                break;
            case Phase.Rotation:
                RotateActiveWarrior(targetPosition);
                break;
            case Phase.Action:
                if (isActionSelected) {
                    PerformAction(targetPosition);
                }
                break;
        }
    }

    public void ChangePhase() {
        switch (phase) {
            case Phase.SettingUp:
                phase = Phase.Movement;
                Utils.ChangeButtonText(changePhaseButton, "Zakończ poruszanie");
                break;
            case Phase.Movement:
                phase = Phase.Rotation;
                Utils.ChangeButtonText(changePhaseButton, "Zakończ obrót");
                break;
            case Phase.Rotation:
                phase = Phase.Action;
                isActionSelected = false;
                ActivateSelectActionPanel();
                Utils.ChangeButtonText(changePhaseButton, "Zakończ turę");
                break;
            case Phase.Action:
                ChangeActiveWarrior();
                phase = Phase.Movement;
                selectActionPanel.SetActive(false);
                Utils.ChangeButtonText(changePhaseButton, "Zakończ poruszanie");
                break;
        }
        UpdateAvailablePositions();
    }

    public void ReportMoveAnimationFinished() {
        battleState = BattleState.Idle;
        if (UpdateAvailableMovePositions() == 0) {
            ChangePhase();
        }
    }

    public void ReportActionAnimationFinished() {
        battleState = BattleState.Idle;
        ChangePhase();
    }

    public void SelectAction(int actionIndex) {
        selectedAction = (BaseWarrior.Action) actionIndex;
        isActionSelected = true;
        selectActionPanel.SetActive(false);

        if (actionDetailsDict[selectedAction].requiresTarget) {
            UpdateAvailablePositions();
        } else {
            PerformAction();
        }
    }

    private void ChangeActiveWarrior() {
        GetActiveWarriorUISystem().SetActiveIndicatorEnabled(false);
        if (activeWarriorIndex < warriors.Count - 1) {
            activeWarriorIndex++;
        } else {
            activeWarriorIndex = 0;
        }
        GetActiveWarriorClass().ResetMovePoints();
        GetActiveWarriorUISystem().SetActiveIndicatorEnabled(true);
    }

    private void ActivateSelectActionPanel() {
        selectActionPanel.SetActive(true);
        List<BaseWarrior.Action> avaliableActions = GetActiveWarriorClass().avaliableActions;
        int i = 0;
        foreach (BaseWarrior.Action action in Enum.GetValues(typeof(BaseWarrior.Action))) {
            GameObject actionButton = selectActionPanel.transform.GetChild(i).gameObject;
            if (avaliableActions.Contains(action)) {
                actionButton.SetActive(true);
            } else {
                actionButton.SetActive(false);
            }
            i++;
        }
    }

    /*
     * Logic related to AvailablePositionsTilemap
     */

    private void UpdateAvailablePositions() {

        switch (phase) {
            case Phase.SettingUp:

                break;
            case Phase.Movement:
                UpdateAvailableMovePositions();
                break;
            case Phase.Rotation:
                UpdateAvailableRotationPositions();
                break;
            case Phase.Action:
                if (!isActionSelected) {
                    GetAvailPosTilemap().ClearTilemap();
                } else {
                    UpdateAvaliableTargetPositions();
                }
                break;
        }
    }

    private int UpdateAvailableMovePositions() {
        int numberOfWalkableFields = 0;
        GetCombatGrid().GetGridPosition(warriors[activeWarriorIndex].transform.position, out int unitX, out int unitY);

        GetAvailPosTilemap().ClearTilemap();
        if (phase != Phase.Movement) {
            return 0;
        }
        int movePoints = GetActiveWarriorClass().GetMovePoints();

        for (int x = unitX - movePoints / 10; x <= unitX + movePoints / 10; x++) {
            for (int y = unitY - movePoints; y <= unitY + movePoints; y++) {
                if (IsMoveAvailable(x, y, out List<TerrainNode> _, out int _)) {
                    numberOfWalkableFields++;
                    GetAvailPosTilemap().SetTilemapSprite(
                        x, y, AvailablePositionsTilemapObject.TilemapSprite.Move
                    );
                }
            }
        }
        return numberOfWalkableFields;
    }

    private void UpdateAvailableRotationPositions() {

        GetCombatGrid().GetGridPosition(warriors[activeWarriorIndex].transform.position, out int unitX, out int unitY);

        GetAvailPosTilemap().ClearTilemap();
        if (phase != Phase.Rotation) {
            return;
        }
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

    private void UpdateAvaliableTargetPositions() {
        List<CombatGridObject> availableTargets = actionDetailsDict[selectedAction].GetAvailableTargetPositions();
        GetAvailPosTilemap().ClearTilemap();
        foreach (CombatGridObject target in availableTargets) {
            GetAvailPosTilemap().SetTilemapSprite(
                target.x, target.y, AvailablePositionsTilemapObject.TilemapSprite.Attack
            );
        }
    }

    private List<CombatGridObject> GetAvailableMeleeAttackTargetPositions() {
        List<CombatGridObject> availableTargets = new List<CombatGridObject>();
        GetActiveWarriorCoordinates(out int activeWarriorX, out int activeWarriorY);
        List<CombatGridObject> adjacentFields = GetFieldsInFront(
            activeWarriorX, activeWarriorY, GetActiveWarriorUISystem().direction);
        foreach (CombatGridObject field in adjacentFields) {
            if (
                    field.GetWarrior() != null
                    && field.GetWarrior().GetComponent<BaseWarrior>().team != GetActiveTeam()
                ) {
                availableTargets.Add(field);
            }
        }
        return availableTargets;
    }

    private List<CombatGridObject> GetAvailableRangedAttackTargetPositions() {
        List<CombatGridObject> availableTargets = new List<CombatGridObject>();
        GetActiveWarriorCoordinates(out int warriorX, out int warriorY);
        GetTerrainMap().UpdateVisibleFields(warriorX, warriorY);
        foreach (CombatGridObject field in GetFieldsOccupiedByTeam(OppositeTeam(GetActiveTeam()))) {
            if (GetTerrainMap().IsFieldVisible(field.x, field.y)) {
                availableTargets.Add(field);
            }                
        }
        return availableTargets;
    }

    private bool IsMoveAvailable(int x, int y, out List<TerrainNode> movePath, out int distance) {
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
        GetActiveWarriorCoordinates(out int activeWarriorX, out int activeWarriorY);

        List<TerrainNode> pathNodes = GetTerrainMap().FindPath(
                activeWarriorX, activeWarriorY, x, y, out distance,
                GetTerrainNodesOcuppiedByInactiveTeam()
            );
        if (pathNodes == null) {
            // No valid path
            return false;
        }
        if (distance > GetActiveWarriorClass().GetMovePoints()) {
            // Outside walking distance
            return false;
        }
        movePath = pathNodes;
        return true;
    }

    private bool IsMoveAvailable(Vector3 position, out List<TerrainNode> movePath, out int distance) {
        GetCombatGrid().GetGridPosition(position, out int x, out int y);
        bool result = IsMoveAvailable(x, y, out movePath, out distance);
        return result;
    }

    private void MoveActiveWarriorOnGrid(Vector3 targetPosition) {
        if (battleState == BattleState.Busy) {
            Debug.Log("Move command not accepted: already busy with another animation");
            return;
        }
        if (IsMoveAvailable(targetPosition, out List<TerrainNode> pathNodes, out int distance)) {
            Vector3[] pathCoords = new Vector3[pathNodes.Count];
            for (int i = 0; i < pathNodes.Count; i++) {
                pathCoords[i] = GetWorldPositionOnWarriorsLayer(pathNodes[i].x, pathNodes[i].y);
            }

            GetCombatGrid().GetGridObject(warriors[activeWarriorIndex].transform.position)
                .ClearWarrior();
            GetCombatGrid().GetGridObject(targetPosition).SetWarrior(warriors[activeWarriorIndex]);
            GetActiveWarriorUISystem().StartMoveThroughPath(pathCoords, ReportMoveAnimationFinished);
            battleState = BattleState.Busy;
            GetActiveWarriorClass().UseMovePoints(distance);
        }
        
    }

    private void RotateActiveWarrior(Vector3 targetPosition) {
        GetCombatGrid().GetGridPosition(
                targetPosition, out int targetX, out int targetY
            );
        GetActiveWarriorCoordinates(out int activeWarriorX, out int activeWarriorY);

        if (targetX == activeWarriorX) {
            if (targetY == activeWarriorY + 1) {
                GetActiveWarriorUISystem().ChangeDirection(WarriorUISystem.Direction.N);
                ChangePhase();
            } else if (targetY == activeWarriorY - 1) {
                GetActiveWarriorUISystem().ChangeDirection(WarriorUISystem.Direction.S);
                ChangePhase();
            }
        } else if (targetY == activeWarriorY) {
            if (targetX == activeWarriorX + 1) {
                GetActiveWarriorUISystem().ChangeDirection(WarriorUISystem.Direction.E);
                ChangePhase();
            } else if (targetX == activeWarriorX - 1) {
                GetActiveWarriorUISystem().ChangeDirection(WarriorUISystem.Direction.W);
                ChangePhase();
            }
        }
    }

    private void PerformAction(Vector3 targetPosition = default(Vector3)) {
        if (actionDetailsDict[selectedAction].requiresTarget) {
            CombatGridObject target = GetCombatGrid().GetGridObject(targetPosition);
            if (actionDetailsDict[selectedAction].GetAvailableTargetPositions().Contains(target)) {
                actionDetailsDict[selectedAction].PerformAction(target);
            } else {
                Debug.Log("Invalid action target");
            }
        } else {
            actionDetailsDict[selectedAction].PerformAction(null);
        }
    }

    private void PerformMeleeAttack(CombatGridObject target) {
        GetActiveWarriorCoordinates(out int AttackerX, out int AttackerY);
        CombatGridObject attackerField = GetCombatGrid().GetGridObject(AttackerX, AttackerY);
        WarriorUISystem.Direction targetDirection = target.GetWarrior().GetComponent<WarriorUISystem>().direction;


        Strike strike = GetActiveWarriorClass().GenerateStrike(Strike.AttackType.Melee);
        if (GetFieldsInFlank(target, targetDirection).Contains(attackerField)) { // flank attack
            strike.preDamageDisruption = FLANK_ATTACK_PRE_DAMAGE_DISRUPTION;
        } else if (GetFieldsInRear(target, targetDirection).Contains(attackerField)) { // rear attack
            strike.preDamageDisruption = REAR_ATTACK_PRE_DAMAGE_DISRUPTION;
        }
        target.GetWarrior().GetComponent<BaseWarrior>().TakeStrike(strike);
        StartUpdateBarsAnimation(target.GetWarrior().GetComponent<WarriorUISystem>());
    }

    private void PerformRangedAttack(CombatGridObject target) {
        Strike strike = GetActiveWarriorClass().GenerateStrike(Strike.AttackType.Ranged);
        target.GetWarrior().GetComponent<BaseWarrior>().TakeStrike(strike);
        StartUpdateBarsAnimation(target.GetWarrior().GetComponent<WarriorUISystem>());
    }

    private void PerformRegroup(CombatGridObject target = null) {
        GetActiveWarriorClass().ChangeOrderliness(GetActiveWarriorClass().regroupAbility);
        StartUpdateBarsAnimation(GetActiveWarriorUISystem());
    }

    private void StartUpdateBarsAnimation(WarriorUISystem warriorUISystem) {
        battleState = BattleState.Busy;
        GetAvailPosTilemap().ClearTilemap();
        warriorUISystem.StartUpdateBars(ReportActionAnimationFinished);
    }

    private WarriorUISystem GetActiveWarriorUISystem() {
        return warriors[activeWarriorIndex].GetComponent<WarriorUISystem>();
    }

    private BaseWarrior GetActiveWarriorClass() {
        return warriors[activeWarriorIndex].GetComponent<BaseWarrior>();
    }

    private BaseWarrior.Team GetActiveTeam() {
        return warriors[activeWarriorIndex].GetComponent<BaseWarrior>().team;
    }

    private void GetWarriorCoordinates(GameObject warrior, out int x, out int y) {
        GetCombatGrid().GetGridPosition( warrior.transform.position, out x, out y);
    }

    private void GetActiveWarriorCoordinates(out int x, out int y) {
        GetWarriorCoordinates(warriors[activeWarriorIndex], out x, out y);
    }

    private List<CombatGridObject> GetFieldsInFront(int x, int y, WarriorUISystem.Direction direction) {
        List<CombatGridObject> fieldsList = new List<CombatGridObject>();
        switch (direction) {
            case WarriorUISystem.Direction.N:
                fieldsList.Add(GetCombatGrid().GetGridObject(x    , y + 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y + 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y + 1));
                break;
            case WarriorUISystem.Direction.S:
                fieldsList.Add(GetCombatGrid().GetGridObject(x    , y - 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y - 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y - 1));
                break;
            case WarriorUISystem.Direction.W:
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y    ));
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y + 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y - 1));
                break;
            case WarriorUISystem.Direction.E:
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y    ));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y + 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y - 1));
                break;
        }
        return fieldsList;
    }

    private List<CombatGridObject> GetFieldsInFront(CombatGridObject field, WarriorUISystem.Direction direction) {
        return GetFieldsInFront(field.x, field.y, direction);    
    }

    private List<CombatGridObject> GetFieldsInFlank(int x, int y, WarriorUISystem.Direction direction) {
        List<CombatGridObject> fieldsList = new List<CombatGridObject>();
        switch (direction) {
            case WarriorUISystem.Direction.N:
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y    ));
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y - 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y    ));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y - 1));
                break;
            case WarriorUISystem.Direction.S:
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y    ));
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y + 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y    ));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y + 1));
                break;
            case WarriorUISystem.Direction.W:
                fieldsList.Add(GetCombatGrid().GetGridObject(x    , y - 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y - 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x    , y + 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x + 1, y + 1));
                break;
            case WarriorUISystem.Direction.E:
                fieldsList.Add(GetCombatGrid().GetGridObject(x    , y - 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y - 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x    , y + 1));
                fieldsList.Add(GetCombatGrid().GetGridObject(x - 1, y + 1));
                break;
        }
        return fieldsList;
    }

    private List<CombatGridObject> GetFieldsInFlank(CombatGridObject field, WarriorUISystem.Direction direction) {
        return GetFieldsInFlank(field.x, field.y, direction);
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
        return fieldsList;
    }

    private List<CombatGridObject> GetFieldsInRear(CombatGridObject field, WarriorUISystem.Direction direction) {
        return GetFieldsInRear(field.x, field.y, direction);
    }

    private List<TerrainNode> GetTerrainNodesOcuppiedByInactiveTeam() {
        BaseWarrior.Team team = OppositeTeam(GetActiveTeam());
        List<CombatGridObject> gridObjects = GetFieldsOccupiedByTeam(team);
        List<TerrainNode> terrainNodes = new List<TerrainNode>();
        foreach (CombatGridObject gridObject in gridObjects) {
            terrainNodes.Add(GetTerrainMap().GetGrid().GetGridObject(gridObject.x, gridObject.y));
        }
        return terrainNodes;
    }

    private BaseWarrior.Team OppositeTeam(BaseWarrior.Team team) {
        if (team == BaseWarrior.Team.Heroes) {
            return BaseWarrior.Team.Enemies;
        } else {
            return BaseWarrior.Team.Heroes;
        }
    }
    private List<GameObject> TeamMembers(BaseWarrior.Team team) {
        List<GameObject> teamMembers = new List<GameObject>();
        foreach (GameObject warrior in warriors) {
            if (warrior.GetComponent<BaseWarrior>().team == team) {
                teamMembers.Add(warrior);
            }
        }
        return teamMembers;
    }

    private List<CombatGridObject> GetFieldsOccupiedByTeam(BaseWarrior.Team team) {
        List<CombatGridObject> occupiedFields = new List<CombatGridObject>();
        foreach (GameObject warrior in TeamMembers(team)) {
            occupiedFields.Add(GetCombatGrid().GetGridObject(warrior.transform.position));
        }
        return occupiedFields;
    }

    private Vector3 GetWorldPositionOnWarriorsLayer(int x, int y) {
        Vector3 cellOriginCoords = GetCombatGrid().GetWorldPosition(x, y);
        return cellOriginCoords + new Vector3(
                    GameHandler.Instance.getCellSize() * .5f, GameHandler.Instance.getCellSize() * .5f, LAYER_Z_ALT
                );
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
