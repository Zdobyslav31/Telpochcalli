using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarriorUISystem : MonoBehaviour {
    public enum Direction {
        N,
        S,
        W,
        E
    }
    public Direction direction;
    private enum WarriorState {
        Moving,
        UpdatingBars,
        Idle
    }
    private enum Stat {
        Health,
        Orderliness
    }
    private float MOVEMENT_SPEED = 5f;
    private float BAR_UPDATE_SPEED = .5f;
    private WarriorState warriorState;
    private Vector3[] movePath;
    private int movePathIndex;
    private float targetHealthBarValue;
    private float targetOrderlinessBarValue;
    private Action onAnimationComplete;
    private BaseWarrior warriorModel;
    private Dictionary<Direction, Quaternion> directionRotationVectors;
    [SerializeField] private GameObject healthBarIndicator;
    [SerializeField] private GameObject orderlinessBarIndicator;
    [SerializeField] private GameObject activeIndicator;
    [SerializeField] private GameObject directionArrow;

    private void Start() {
        directionRotationVectors = new Dictionary<Direction, Quaternion> {
            { Direction.N, Quaternion.Euler(0, 0,   0f) },
            { Direction.S, Quaternion.Euler(0, 0, 180f) },
            { Direction.W, Quaternion.Euler(0, 0,  90f) },
            { Direction.E, Quaternion.Euler(0, 0, 270f) },
    };
        warriorState = WarriorState.Idle;
        warriorModel = this.GetComponent<BaseWarrior>();
        UpdateDirectionArrow();
    }

    private void Update() {
        switch (warriorState) {
            case (WarriorState.Idle):
                break;

            case (WarriorState.Moving):
                ContinueMoveThroughPath();
                break;

            case (WarriorState.UpdatingBars):
                ContinueUpdateBars();
                break;
        }

    }

    public void StartMoveThroughPath(Vector3[] path, Action onMoveComplete) {
        movePath = path;
        movePathIndex = 1;
        warriorState = WarriorState.Moving;
        onAnimationComplete = onMoveComplete;
    }

    public void StartUpdateBars(Action onUpdateComplete) {
        warriorState = WarriorState.UpdatingBars;
        targetHealthBarValue = warriorModel.GetHealthNormailzed();
        targetOrderlinessBarValue = warriorModel.GetOrderlinessNormailzed();
        onAnimationComplete = onUpdateComplete;
    }

    private void ContinueUpdateBars() {
        if (ContinueUpdateBar(healthBarIndicator, targetHealthBarValue)
            && ContinueUpdateBar(orderlinessBarIndicator, targetOrderlinessBarValue)) {
            warriorState = WarriorState.Idle;
            onAnimationComplete();
        }
    }

    private bool ContinueUpdateBar(GameObject barIndicator, float targetValue) {
        if (Vector3.Distance(
                barIndicator.transform.localScale,
                new Vector3(targetValue, 1, 1)
                ) > 0.00001f) { // didn't finish updating
            barIndicator.transform.localScale = Vector3.MoveTowards(
                    barIndicator.transform.localScale,
                    new Vector3(targetValue, 1, 1),
                    BAR_UPDATE_SPEED * Time.deltaTime
                );
            return false;
        } else {
            return true;
        }
    }

    public void SetActiveIndicatorEnabled(bool value) {
        activeIndicator.SetActive(value);
    }

    public void ChangeDirection(Direction targetDirection) {
        direction = targetDirection;
        UpdateDirectionArrow();
    }

    public void UpdateDirectionArrow() {
        directionArrow.transform.rotation = directionRotationVectors[direction];
    }

    private void ContinueMoveThroughPath() {
        if (Vector3.Distance(transform.position, movePath[movePathIndex]) > 0.001f) { // didn't reach destination
            transform.position = Vector3.MoveTowards(
                    transform.position,
                    movePath[movePathIndex],
                    MOVEMENT_SPEED * Time.deltaTime
                );
        } else { // reached destination
            if (movePathIndex != movePath.Length - 1) { // walkthrought destination
                movePathIndex++;
            } else { // final destination
                warriorState = WarriorState.Idle;
                onAnimationComplete();
            }

        }
    }

    public Vector3 GetPosition() {
        return transform.position;
    }
}
