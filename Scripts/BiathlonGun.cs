using UnityEngine;
using UnityEngine.InputSystem;

public class BiathlonGun : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform muzzlePoint;       // punta della canna
    [SerializeField] private GameObject projectilePrefab; // prefab proiettile con Rigidbody + Collider
    [SerializeField] private Rigidbody gunRigidbody;     // Rigidbody del fucile (opzionale)

    [Header("Ballistics")]
    [SerializeField] private float muzzleVelocity = 330f; // velocità m/s
    [SerializeField] private float spawnOffset = 0.15f;   // distanza dalla canna per evitare collisioni

    [Header("Optional")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private AudioSource fireSound;
    [SerializeField] private float recoilKick = 0.05f;
    [SerializeField] private float recoilRecoverSpeed = 8f;

    [Header("Movement (simulation)")]
    [SerializeField] private float moveSpeed = 0.25f;               // ampiezza del movimento per input unit
    [SerializeField] private float moveSmoothing = 8f;              // smoothing del movimento
    [SerializeField] private Vector2 movementLimitsX = new Vector2(-0.3f, 0.3f);
    [SerializeField] private Vector2 movementLimitsY = new Vector2(-0.15f, 0.15f);

    private Vector3 baseLocalPosition;
    private Vector3 targetLocalPosition;
    private Vector3 velocitySmooth = Vector3.zero; // per SmoothDamp

    //private Vector3 recoilOffset = Vector3.zero;

    void Start()
    {
        // salva la posizione locale di riposo (usata come riferimento per movimento + rinculo)
        baseLocalPosition = transform.localPosition;
        targetLocalPosition = baseLocalPosition;
    }

    void LateUpdate()
    {
        // --- INPUT MOVIMENTO (frecce) ---
        float inputH = (Keyboard.current.rightArrowKey.isPressed ? 1f : 0f) - (Keyboard.current.leftArrowKey.isPressed ? 1f : 0f);
        float inputV = (Keyboard.current.upArrowKey.isPressed ? 1f : 0f) - (Keyboard.current.downArrowKey.isPressed ? 1f : 0f);

        // aggiorna il target solo se c’è input
        if (Mathf.Abs(inputH) > 0f || Mathf.Abs(inputV) > 0f)
        {
            // 1) calcola l'offset in world space lungo gli assi locali desiderati
            Vector3 worldOffset = -transform.forward * (inputH * moveSpeed) + transform.up * (inputV * moveSpeed);

            // 2) converti l'offset in local space (per poter clampare x/y locali)
            Vector3 localOffset = transform.InverseTransformDirection(worldOffset);

            // 3) clamp sulle componenti locali
            localOffset.x = Mathf.Clamp(localOffset.x, movementLimitsX.x, movementLimitsX.y);
            localOffset.y = Mathf.Clamp(localOffset.y, movementLimitsY.x, movementLimitsY.y);

            // 4) aggiorna il target (targetLocalPosition è in local space)
            targetLocalPosition += localOffset * Time.deltaTime * moveSmoothing;
        }
        else
        {
            velocitySmooth = Vector3.zero;
        }

        // --- MUOVI IL FUCILE VERSO IL TARGET ---
        transform.localPosition = Vector3.SmoothDamp(
            transform.localPosition,
            targetLocalPosition,
            ref velocitySmooth,
            1f / moveSmoothing,
            Mathf.Infinity,
            Time.deltaTime
        );

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Shoot();
        }
 
    // FUTURO: sostituire con trigger joystick / VR
    // Es: XR Grab Interactable -> OnActivate() -> gun.Shoot()
}

public void Shoot()
    {
        if (muzzlePoint == null || projectilePrefab == null) return;


        // spawn del proiettile 

        Vector3 shootDirection = muzzlePoint.transform.TransformDirection(Vector3.up);
        Vector3 spawnPos = muzzlePoint.position + shootDirection * (spawnOffset + 0.05f);
        Quaternion spawnRot = muzzlePoint.rotation;

        GameObject projGO = Instantiate(projectilePrefab, spawnPos, spawnRot);
        Rigidbody rb = projGO.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = gunRigidbody != null ? gunRigidbody.linearVelocity : Vector3.zero;
            rb.AddForce(shootDirection * muzzleVelocity, ForceMode.VelocityChange);
        }

        // rinculo visivo lungo la direzione opposta
        //recoilOffset += -shootDirection * recoilKick;
    }
}
