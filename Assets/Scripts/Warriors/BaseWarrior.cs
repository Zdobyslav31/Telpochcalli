using System;
using System.Collections;
using System.Collections.Generic;
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
    public int regroupAbility;
    [SerializeField] public List<Action> avaliableActions;

    private void Awake() {
        currentHealth = maxHealth;
        currentOrderliness = maxOrderliness;
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

    public void UseMovePoints(int movePoints) {
        currentMovePoints = Math.Clamp(currentMovePoints - movePoints, 0, maxMovePoints);
    }

    public void ResetMovePoints() {
        currentMovePoints = maxMovePoints;
    }

    public void TakeStrike(Strike strike) {
        Debug.Log(this.gameObject.name + " current stats: health: " + currentHealth + "; orderliness: " + currentOrderliness);
        Debug.Log("Taking " + strike.attackType + " strike: " + strike);
        ChangeOrderliness(-strike.preDamageDisruption);
        if (strike.attackType == Strike.AttackType.Melee) {
            ChangeHealth(-strike.damage * Math.Max(MIN_DAMAGE_PERCENTAGE, (100 - currentOrderliness)) / 100);
        } else if (strike.attackType == Strike.AttackType.Ranged) {
            ChangeHealth(-strike.damage);
        }
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
                    GetDamageRandomizedAndModified(rangedAttackDisruptingPower)
                    );
            case Strike.AttackType.Charge:
                throw new NotImplementedException();
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
}

