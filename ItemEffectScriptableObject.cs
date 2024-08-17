using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//[CreateAssetMenu(fileName = "ItemEffectScriptableObject", menuName = "ScriptableObjects/ItemEffect")]
public class ItemEffectScriptableObject : ScriptableObject
{
    public float effectValue;

    public virtual void ApplyEffect(Player p) { 
        
    }

    public virtual string GetEffect() { return ""; }
}
