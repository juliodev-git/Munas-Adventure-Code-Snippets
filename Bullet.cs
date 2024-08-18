using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * This script is attached to a particle system. Using OnParticleCollision, we can detect any colliders that were hit by our bullet particle and interact with them.
 * In our case, we simply deduct health from any HealthControllers attached to GameObjects. The HealthController handles it from there.
 */

public class Bullet : MonoBehaviour
{
    [SerializeField]
    private GameObject bulletHitParticle;

    public List<ParticleCollisionEvent> collisionEvents;

    private ParticleSystem part;

    private void OnEnable()
    {
        part = GetComponent<ParticleSystem>();
        collisionEvents = new List<ParticleCollisionEvent>();
    }

    private void OnParticleCollision(GameObject other)
    {
       // Debug.Log(other.name);
        //do damage to the other entity
        if (other.TryGetComponent<HealthController>(out HealthController hc)) {

            
            //TODO: Refactor, all bullets check for player
            //Might be better to like, publisize the damage value or something
            if(other.CompareTag("Player"))
                hc.Damage(5);
            else
                hc.Damage(1);

            part.GetCollisionEvents(other, collisionEvents);

            Instantiate(bulletHitParticle, collisionEvents[0].intersection, Quaternion.LookRotation(collisionEvents[0].normal));
        }

        Destroy(this.gameObject);
    }
}
