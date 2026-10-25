using UnityEngine;

public class LetterItem : MonoBehaviour
{
    public char letter;
    public int letterIndex; // Index within the target word
    public WordCollectManager manager;

    private bool collected = false;
    private Vector3 initialLocalPos;

    private void Start()
    {
        initialLocalPos = transform.localPosition;
    }

    private void Update()
    {
        // Gentle rotation and bobbing animation
        transform.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);
        float bob = Mathf.Sin(Time.time * 3f + letterIndex) * 0.12f;
        transform.localPosition = initialLocalPos + new Vector3(0f, bob, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        // Check if player touched the letter
        if (other.CompareTag("Player") || other.GetComponent<ThirdPersonController>() != null || other.GetComponentInParent<ThirdPersonController>() != null)
        {
            Collect();
        }
    }

    public void Collect()
    {
        if (collected) return;
        collected = true;

        if (manager != null)
        {
            manager.CollectLetter(letter, letterIndex);
        }

        // Spawn a subtle pop/shrink effect or destroy
        Destroy(gameObject);
    }
}
