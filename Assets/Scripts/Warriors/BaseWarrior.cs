using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BaseWarrior : MonoBehaviour {
    public enum Team {
        Heroes,
        Enemies
    }
    public enum Action {
        Attack,
        Regroup,
        Shoot,
        Charge
    }
    private int MIN_DAMAGE_PERCENTAGE = 3;
    private int MIN_DISTANCE_FOR_CHARGE = 2;

    [SerializeField] public Team team;
    [SerializeField] public string warriorName;

    [SerializeField] private int maxMovePoints;
    private int currentMovePoints;
    [SerializeField] private int maxHealth;
    public int currentHealth;
    [SerializeField] private int maxOrderliness;
    private int currentOrderliness;
    [SerializeField] private int meleeAttackDamagingPower;
    [SerializeField] private int meleeAttackDisruptingPower;
    [SerializeField] private int rangedAttackDamagingPower;
    [SerializeField] private int rangedAttackDisruptingPower;
    [SerializeField] private int chargeMomentum;
    public int regroupAbility;
    [SerializeField] private List<Action> possibleActions;
    private int chargeSpeed;
    private bool fightBound;

    private void Awake() {
        currentHealth = maxHealth;
        currentOrderliness = maxOrderliness;
        chargeSpeed = 0;
        fightBound = false;
        ResetMovePoints();
    }

    public void ChangeHealth(int value) {
        currentHealth = Math.Clamp(currentHealth + value, 0, maxHealth);
    }

    public void ChangeOrderliness(int value) {
        currentOrderliness = Math.Clamp(currentOrderliness + value, 0, maxOrderliness);
    }

    public float GetHealthNormailzed() {
        return (float) currentHealth / (float) maxHealth;
    }

    public float GetOrderlinessNormailzed() {
        return (float) currentOrderliness / (float) maxOrderliness;
    }

    public int GetMovePoints() {
        return currentMovePoints;
    }

    public int GetChargeMomentum() {
        return chargeMomentum;
    }

    public void SetFightBound(bool value) {
        fightBound = value;
    }

    public void UseMovePoints(int movePoints) {
        currentMovePoints = Math.Clamp(currentMovePoints - movePoints, 0, maxMovePoints);
    }

    public void ResetMovePoints() {
        currentMovePoints = maxMovePoints;
        chargeSpeed = 0;
    }

    public void AdjustChargeSpeed(List<TerrainNode> pathTraveled) {
        TerrainNode last = pathTraveled.Last();
        foreach (TerrainNode terrainNode in pathTraveled) {
            bool isLastNode = terrainNode.Equals(last);
            if (terrainNode.AllowsCharge(isLastNode)) {
                chargeSpeed++;
            } else {
                chargeSpeed = 0;
            }
        }
    }

    public void TakeStrike(Strike strike) {
        Debug.Log(this.gameObject.name + " current stats: health: " + currentHealth + "; orderliness: " + currentOrderliness);
        Debug.Log("Taking " + strike.attackType + " strike: " + strike);
        ChangeOrderliness(-strike.preDamageDisruption);
        float defenceModifier = 1f;
        if (!strike.ignoresOrderliness) {
            defenceModifier = Math.Max(MIN_DAMAGE_PERCENTAGE, (100 - currentOrderliness)) / 100f;
        }
        ChangeHealth((int)(-strike.damage * defenceModifier));
        ChangeOrderliness(-strike.disruption);
        Debug.Log(this.gameObject.name + " current stats: health: " + currentHealth + "; orderliness: " + currentOrderliness);
    }

    public Strike GenerateStrike(Strike.AttackType attackType) {
        switch (attackType) {
            case Strike.AttackType.Melee:
                return new Strike(
                    attackType,
                    GetDamageRandomizedAndModified(meleeAttackDamagingPower),
                    GetDamageRandomizedAndModified(meleeAttackDisruptingPower)
                    );
            case Strike.AttackType.Ranged:
                return new Strike(
                    attackType,
                    GetDamageRandomizedAndModified(rangedAttackDamagingPower),
                    GetDamageRandomizedAndModified(rangedAttackDisruptingPower),
                    ignoresOrderliness:true
                    );
            case Strike.AttackType.Charge:
                return new Strike(
                    attackType,
                    GetDamageRandomizedAndModified(meleeAttackDamagingPower + chargeSpeed * chargeMomentum),
                    GetDamageRandomizedAndModified(meleeAttackDisruptingPower),
                    chargeSpeed
                    );
            case Strike.AttackType.Splash:
                throw new NotImplementedException();
        }
        return null;
    }

    public int GetDamageRandomizedAndModified(int baseDamage) {
        System.Random rnd = new System.Random();
        int randomizedDamage = baseDamage * (100 + rnd.Next(-20, 21)) / 100;
        return (int) (randomizedDamage * Math.Max(GetHealthNormailzed(), .3f));
    }

    public bool IsActionPossible(Action action) {
        if (!possibleActions.Contains(action)) {
            return false;
        }
        if (action == Action.Charge) {
            return (chargeSpeed >= MIN_DISTANCE_FOR_CHARGE);
        }
        if (fightBound && (action == Action.Shoot || action == Action.Regroup)) {
            return false;
        }
        return true;
    }
}

