using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosive : MonoBehaviour
{
    [SerializeField]
    private GameObject explosionPrefab;

    [Range(1, 50)]
    public float scaler;

    // Start is called before the first frame update
    void Start()
    {
        if (this.TryGetComponent<HealthController>(out HealthController hc)) {
            hc.die += TimedExplosion;
        }
    }

    public void Explode() {
        GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        explosion.transform.localScale = Vector3.one * scaler;

        Destroy(this.gameObject);
    }

    public void TimedExplosion() {
        StartCoroutine("CountdownExplosion");
    }

    private IEnumerator CountdownExplosion() {

        if (this.TryGetComponent<HealthController>(out HealthController hc)) {
            hc.ColorMaterials(Color.red);
        }

        yield return new WaitForSeconds(2.0f);
        Explode();
    }
}
