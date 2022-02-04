using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Strike
{
    public enum AttackType {
        Melee,
        Ranged,
        Charge,
        Splash
    }
    public AttackType attackType;
    public int damage;
    public int disruption;
    public int preDamageDisruption;

    public Strike(AttackType attackType, int damage, int disruption, int preDamageDisruption = 0) {
        this.attackType = attackType;
        this.damage = damage;
        this.disruption = disruption;
        this.preDamageDisruption = preDamageDisruption;
    }

    public override string ToString() {
        return "damage: " + damage + "; disruption: " + disruption + "; preDemageDisruption: " + preDamageDisruption;
    }
}
