using UnityEngine;
using System.Collections;


public class TriggerForce : MonoBehaviour
{
    [SerializeField] private float force = 1f;
    [SerializeField] private float waitTime = 1f;
    private bool isPlayerInTrigger = false;
    private Rigidbody playerRigidbody;

    [SerializeField] private ParticleSystem particlePulse;
    [SerializeField] private ParticleSystem particlePreburst;

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.tag == "Player")
        {
            particlePreburst.Play();
            isPlayerInTrigger = true;
            playerRigidbody = other.transform.GetComponent<Rigidbody>();
            StartCoroutine(CheckPlayerInTrigger());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.tag == "Player")
        {
            isPlayerInTrigger = false;
        }
    }

    private IEnumerator CheckPlayerInTrigger()
    {
        yield return new WaitForSeconds(waitTime);
        particlePreburst.Stop();
        particlePulse.Emit(100);
        if (isPlayerInTrigger && playerRigidbody != null)
        {
            // Apply force to the Rigidbody
            Vector3 forceVector = transform.up * force; // Example force, modify as needed
            playerRigidbody.AddForce(forceVector, ForceMode.Impulse);
        }
    }
}
