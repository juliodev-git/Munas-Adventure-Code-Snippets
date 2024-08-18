using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/*
 * HealthController tracks an entity's health and allows for health manipulation via damage from bullets, grenades, slashes or the healing effects of consumables.
 * A delegate is used to trigger any events that come from an entitie's death or changes to an entities health.
 * For the player, health updates trigger the UI to update the health bar. The OnDie delegate is used to trigger a game over.
 * For any other entities, only the OnDie delegate is used: either to spawn loot from the enemy, or instantiate an explosion followed by object destruction.
 */

public class HealthController : MonoBehaviour
{
    [SerializeField]
    public int health;

    public int maxHealth;

    private float defense;

    private bool isDamaged;

    private bool dead;

    [SerializeField]
    private SkinnedMeshRenderer m_skr;

    [SerializeField]
    private MeshRenderer m_mr;

    private Material[] originalColor;

    public delegate void OnDie();
    public OnDie die;

    public delegate void OnDamage();
    public OnDamage onDamage;

    public delegate void OnExplode();
    public OnExplode explode;

    //Human subscibres to this, but even a game controller can subscribe to keep an eye out for when an entity dies in order to complete the level
    public delegate void OnHealthUpdate();
    public OnHealthUpdate onHealthUpdate;

    public UnityEvent OnDeath;

    private void Start()
    {
        StoreMaterialColors();

        if (health <= 0) {

            if (maxHealth <= 0)
                health = 100;
            else
                health = maxHealth;
        }

        if (maxHealth <= 0)
        {
            maxHealth = 100;
        }

        defense = 1.0f;

        dead = false;
    }

    public void AddHealth(int h) {
        health += h;
        health = Mathf.Clamp(health, 0, maxHealth);

        onHealthUpdate?.Invoke();
    }

    //a delegate that Human listens to? 
    public void Damage(int d) {

        if (dead)
            return;

        health -= (int)(d * (1/defense));
        health = Mathf.Clamp(health, 0, maxHealth);

        onHealthUpdate?.Invoke();
        //onDamage?.Invoke();

        if (health <= 0)
        {
            //die

            //disable dieing twice...
            die?.Invoke();
            OnDeath.Invoke();
            this.enabled = false;
            dead = true;
            //this.GetComponent<Collider>().enabled = false;
            //if (this.TryGetComponent<Rigidbody>(out Rigidbody rb)) {
            //    rb.useGravity = false;
            //    rb.isKinematic = true;
            //}
        }
        else {

            StopCoroutine("Hurt");
            StartCoroutine("Hurt");
        }
    }


    public void Push() {
        onDamage?.Invoke();

        
    }

    private void EndDamageStun() {

        //isDamaged = false;
    
    }

    IEnumerator Hurt() {

        ColorMaterials(Color.white);
        yield return new WaitForSeconds(Time.fixedDeltaTime * 6);
        RestoreMaterialColors();
    }

    public void InitiateExplosion()
    {
        //StartCoroutine("Explode");
    }

    IEnumerator Explode()
    {
        for (float i = 0; i < Time.fixedDeltaTime * 3; i += Time.fixedDeltaTime) {

            if (m_skr)
                ColorMaterials(Color.yellow * (i/2.0f));

            yield return null;
        }

        //explosion... instantiate an explosion and also delete the bot...
        explode?.Invoke();
        
    }

    public void ColorMaterials(Color c) {

        if (m_skr) {
            for (int i = 0; i < m_skr.materials.Length; i++)
            {

                if (m_skr.materials[i].HasProperty("_Color"))
                    m_skr.materials[i].color = c;

            }
            return;
        }

        if (m_mr) {
            for (int i = 0; i < m_mr.materials.Length; i++)
            {

                if (m_mr.materials[i].HasProperty("_Color"))
                    m_mr.materials[i].color = c;

            }
            return;
        }
    }

    private void RestoreMaterialColors() {

        if (m_skr) {
            for (int i = 0; i < m_skr.materials.Length; i++)
            {
                if (m_skr.materials[i].HasProperty("_Color"))
                    m_skr.materials[i].color = originalColor[i].color;

            }
            return;
        }

        if (m_mr) {
            for (int i = 0; i < m_mr.materials.Length; i++)
            {
                if (m_mr.materials[i].HasProperty("_Color"))
                    m_mr.materials[i].color = originalColor[i].color;

            }
            return;

        }
       
    }

    private void StoreMaterialColors() {

        if (m_skr) {
            originalColor = new Material[m_skr.materials.Length];

            for (int i = 0; i < m_skr.materials.Length; i++)
            {

                if (m_skr.materials[i].HasProperty("_Color"))
                {
                    Material copy = m_skr.materials[i];
                    originalColor[i] = new Material(copy);
                    originalColor[i].color = copy.color;

                }

            }
            return;
        }

        if (m_mr) {

            originalColor = new Material[m_mr.materials.Length];

            for (int i = 0; i < m_mr.materials.Length; i++)
            {

                if (m_mr.materials[i].HasProperty("_Color"))
                {
                    Material copy = m_mr.materials[i];
                    originalColor[i] = new Material(copy);
                    originalColor[i].color = copy.color;

                }

            }

        }
    }

    public void SetHealth(int h) {
        health = h;
    }

    public void SetMaxHealth(int mh) {
        maxHealth = mh;
    }

    public void SetDefense(float d) {
        defense = d;
        Invoke("ResetDefense", 60.0f);
    }

    private void ResetDefense() {

        defense = 1.0f;
    }
}
