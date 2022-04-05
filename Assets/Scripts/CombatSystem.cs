using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombatSystem : MonoBehaviour {
    private int FLANK_ATTACK_PRE_DAMAGE_DISRUPTION = 10;
    private int REAR_ATTACK_PRE_DAMAGE_DISRUPTION = 20;
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
    private List<TerrainNode> ActiveWarriorTravelledPath;

    public List<GameObject> warriorsToDeploy;
    [SerializeField] private int activeWarriorIndex;
    [SerializeField] private List<GameObject> warriors;
    [SerializeField] private GameObject changePhaseButton;
    [SerializeField] private GameObject selectActionPanel;

    private CombatGridManager combatGridManager;

    private struct ActionDetails {
        public bool requiresTarget;
        public Func<GameObject, List<CombatGridObject>> GetAvailableTargetPositions;
        public Action<CombatGridObject> PerformAction;
        public ActionDetails(Func<GameObject, List<CombatGridObject>> getAvailableTargetPositions, Action<CombatGridObject> performAction) {
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

    private void Awake() {
        combatGridManager = new CombatGridManager();
    }

    private void Start() {
        battleState = BattleState.Idle;
        phase = Phase.SettingUp;
        foreach (GameObject warrior in warriors) {
            Vector3 warriorPosition = warrior.transform.position;
            GetCombatGrid().GetGridObject(warriorPosition).SetWarrior(warrior);
        }
        actionDetailsDict = new Dictionary<BaseWarrior.Action, ActionDetails>() {
            { BaseWarrior.Action.Regroup, new ActionDetails(PerformRegroup)},
            { BaseWarrior.Action.Attack, new ActionDetails(
                    combatGridManager.GetAvailableMeleeAttackTargetPositions, PerformMeleeAttack
                )
            },
            { BaseWarrior.Action.Shoot, new ActionDetails(
                    combatGridManager.GetAvailableRangedAttackTargetPositions, PerformRangedAttack
                )
            },
            { BaseWarrior.Action.Charge, new ActionDetails(
                    combatGridManager.GetAvailableMeleeAttackTargetPositions, PerformCharge
                )
            },
        };
        activeWarriorIndex = 0;
        isActionSelected = false;
        ActiveWarriorTravelledPath = new List<TerrainNode>();
        UpdateAvailablePositions();
        GetActiveWarriorUISystem().SetActiveIndicatorEnabled(true);
    }

    private void Update() {

    }

    public void HandleClickOnGrid(Vector3 targetPosition) {
        switch (phase) {
            case Phase.SettingUp:
                SpawnFirstWarriorFromList(targetPosition);
                break;
            case Phase.Movement:
                MoveActiveWarrior(targetPosition);
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
                if (ActiveWarriorTravelledPath.Any()) {
                    GetActiveWarriorClass().AdjustChargeSpeed(ActiveWarriorTravelledPath);
                }
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
        UpdateFightBoundStatuses();
    }

    public void ReportMoveAnimationFinished() {
        battleState = BattleState.Idle;
        if (combatGridManager.UpdateAvailableMovePositions(GetActiveWarrior()) == 0) {
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
        ActiveWarriorTravelledPath = new List<TerrainNode>();
    }

    private void ActivateSelectActionPanel() {
        selectActionPanel.SetActive(true);
        int i = 0;
        foreach (BaseWarrior.Action action in Enum.GetValues(typeof(BaseWarrior.Action))) {
            GameObject actionButton = selectActionPanel.transform.GetChild(i).gameObject;
            if (GetActiveWarriorClass().IsActionPossible(action)) {
                actionButton.SetActive(true);
            } else {
                actionButton.SetActive(false);
            }
            i++;
        }
    }

    private void UpdateAvailablePositions() {

        switch (phase) {
            case Phase.SettingUp:
                
                break;
            case Phase.Movement:
                combatGridManager.UpdateAvailableMovePositions(GetActiveWarrior());
                break;
            case Phase.Rotation:
                combatGridManager.UpdateAvailableRotationPositions(GetActiveWarrior());
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

    private void UpdateAvaliableTargetPositions() {
        List<CombatGridObject> availableTargets = actionDetailsDict[selectedAction].GetAvailableTargetPositions(GetActiveWarrior());
        GetAvailPosTilemap().ClearTilemap();
        foreach (CombatGridObject target in availableTargets) {
            GetAvailPosTilemap().SetTilemapSprite(
                target.x, target.y, AvailablePositionsTilemapObject.TilemapSprite.Attack
            );
        }
    }

    private void UpdateFightBoundStatuses() {
        foreach(GameObject warrior in warriors) {
            WarriorUISystem.Team team = warrior.GetComponent<WarriorUISystem>().team;
            combatGridManager.GetWarriorCoordinates(warrior, out int x, out int y);
            bool isFightBound = combatGridManager.GetFieldsInControlZoneOfTeam(OppositeTeam(team)).Contains(GetCombatGrid().GetGridObject(x, y));
            warrior.GetComponent<BaseWarrior>().SetFightBound(isFightBound);
        }
    }

    private void SpawnFirstWarriorFromList(Vector3 position) {
        GameObject warriorPrefab = warriorsToDeploy[0];
        if (SpawnWarrior(position, warriorPrefab)) warriorsToDeploy.RemoveAt(0);
        if (warriorsToDeploy.Count < 1) ChangePhase();
    }

    private bool SpawnWarrior(Vector3 position, GameObject warriorPrefab) {
        GameObject warrior = combatGridManager.SpawnWarrior(warriorPrefab, position);
        if (warrior is null) return false;
        warriors.Add(warrior);
        return true;
    }

    public bool SpawnWarrior(int x, int y, GameObject warriorPrefab) {
        GameObject warrior = combatGridManager.SpawnWarrior(warriorPrefab, x, y);
        if (warrior is null) return false;
        warriors.Add(warrior);
        return true;
    }

    private void MoveActiveWarrior(Vector3 targetPosition) {
        if (battleState == BattleState.Busy) {
            Debug.Log("Move command not accepted: already busy with another animation");
            return;
        }
        if (combatGridManager.MoveWarriorOnGrid(GetActiveWarrior(), targetPosition, ReportMoveAnimationFinished, out int distance, out List<TerrainNode> pathNodes)) {
            battleState = BattleState.Busy;
            GetActiveWarriorClass().UseMovePoints(distance);
            ActiveWarriorTravelledPath.AddRange(pathNodes.Skip(1).ToList<TerrainNode>());
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
            if (actionDetailsDict[selectedAction].GetAvailableTargetPositions(GetActiveWarrior()).Contains(target)) {
                actionDetailsDict[selectedAction].PerformAction(target);
            } else Debug.Log("Invalid action target");
        } else actionDetailsDict[selectedAction].PerformAction(null);
    }

    private void PerformMeleeAttack(CombatGridObject target) {
        Strike strike = GetActiveWarriorClass().GenerateStrike(Strike.AttackType.Melee);
        strike = ApplyDirectionModifiers(strike, target);
        TakeStrike(target, strike);
    }

    private void PerformCharge(CombatGridObject target) {
        Strike strike = GetActiveWarriorClass().GenerateStrike(Strike.AttackType.Charge);
        strike = ApplyDirectionModifiers(strike, target);
        strike.preDamageDisruption *= GetActiveWarriorClass().GetChargeMomentum();
        TakeStrike(target, strike);
    }

    private void PerformRegroup(CombatGridObject target = null) {
        GetActiveWarriorClass().ChangeOrderliness(GetActiveWarriorClass().regroupAbility);
        StartUpdateBarsAnimation(GetActiveWarriorUISystem());
    }

    private Strike ApplyDirectionModifiers(Strike strike, CombatGridObject target) {
        GetActiveWarriorCoordinates(out int AttackerX, out int AttackerY);
        CombatGridObject attackerField = GetCombatGrid().GetGridObject(AttackerX, AttackerY);
        WarriorUISystem.Direction targetDirection = target.GetWarrior().GetComponent<WarriorUISystem>().direction;
        if (combatGridManager.GetFieldsInFlank(target, targetDirection).Contains(attackerField)) { // flank attack
            strike.preDamageDisruption += FLANK_ATTACK_PRE_DAMAGE_DISRUPTION;
        } else if (combatGridManager.GetFieldsInRear(target, targetDirection).Contains(attackerField)) { // rear attack
            strike.preDamageDisruption += REAR_ATTACK_PRE_DAMAGE_DISRUPTION;
        }
        return strike;
    }

    private void TakeStrike(CombatGridObject target, Strike strike) {
        target.GetWarrior().GetComponent<BaseWarrior>().TakeStrike(strike);
        StartUpdateBarsAnimation(target.GetWarrior().GetComponent<WarriorUISystem>());

    }

    private void PerformRangedAttack(CombatGridObject target) {
        Strike strike = GetActiveWarriorClass().GenerateStrike(Strike.AttackType.Ranged);
        TakeStrike(target, strike);
    }

    private void StartUpdateBarsAnimation(WarriorUISystem warriorUISystem) {
        battleState = BattleState.Busy;
        GetAvailPosTilemap().ClearTilemap();
        warriorUISystem.StartUpdateBars(ReportActionAnimationFinished);
    }

    private WarriorUISystem GetActiveWarriorUISystem() {
        return GetActiveWarrior().GetComponent<WarriorUISystem>();
    }

    private BaseWarrior GetActiveWarriorClass() {
        return GetActiveWarrior().GetComponent<BaseWarrior>();
    }

    private GameObject GetActiveWarrior() {
        return warriors[activeWarriorIndex];
    }


    private void GetActiveWarriorCoordinates(out int x, out int y) {
        combatGridManager.GetWarriorCoordinates(GetActiveWarrior(), out x, out y);
    }
    public List<GameObject> TeamMembers(WarriorUISystem.Team team) {
        List<GameObject> teamMembers = new List<GameObject>();
        foreach (GameObject warrior in warriors) {
            if (warrior.GetComponent<WarriorUISystem>().team == team) {
                teamMembers.Add(warrior);
            }
        }
        return teamMembers;
    }


    private WarriorUISystem.Team OppositeTeam(WarriorUISystem.Team team) {
        if (team == WarriorUISystem.Team.Heroes) {
            return WarriorUISystem.Team.Enemies;
        } else {
            return WarriorUISystem.Team.Heroes;
        }
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
