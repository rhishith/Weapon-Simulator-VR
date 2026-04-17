using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class WeaponScript : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float range = 100f;
    [SerializeField] private float fireRate = 0.1f;
    [SerializeField] private bool canShoot = true;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private GameObject hitEffect;
    
    private float lastFireTime;
    private bool isDeadly = true;

    public float Damage => damage;
    public float Range => range;
    public float FireRate => fireRate;
    public bool CanShoot => canShoot;
    public bool IsDeadly => isDeadly;

    private void Start()
    {
        InitializeWeapon();
    }

    private void InitializeWeapon()
    {
        // Setup weapon collider
        var collider = GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    private void Update()
    {
        HandleWeaponLogic();
    }

    private void HandleWeaponLogic()
    {
        if (!canShoot) return;

        // Fire weapon if needed
        float currentTime = Time.realtimeSinceStartup;
        if (currentTime - lastFireTime >= fireRate)
        {
            FireWeapon();
            lastFireTime = currentTime;
        }
    }

    private void FireWeapon()
    {
        // Fire weapon logic
        if (muzzleFlash != null)
        {
            muzzleFlash.SetActive(true);
            StartCoroutine(DeactivateMuzzleFlash());
        }

        // Raycast for hit detection
        RaycastHit hit;
        if (Physics.Raycast(transform.forward, out hit, range))
        {
            ApplyDamage(hit.transform);
            
            if (hitEffect != null)
            {
                SpawnHitEffect(hit.point);
            }
        }
    }

    private void ApplyDamage(GameObject target)
    {
        if (target == null || !isDeadly) return;

        var healthComponent = target.GetComponent<IHealth>();
        if (healthComponent != null)
        {
            healthComponent.TakeDamage(damage);
        }

        Debug.Log($"Damaged {target.name} for {damage} damage");
    }

    private void SpawnHitEffect(Vector3 position)
    {
        if (hitEffect == null) return;

        var instance = Instantiate(hitEffect, position, Quaternion.identity);
        Destroy(instance.gameObject, 2f);
    }

    private IEnumerator DeactivateMuzzleFlash()
    {
        yield return new WaitForSeconds(0.1f);
        if (muzzleFlash != null)
        {
            muzzleFlash.SetActive(false);
        }
    }

    // Public methods for external control
    public void SetDamage(float newDamage) => damage = newDamage;
    public void SetRange(float newRange) => range = newRange;
    public void SetFireRate(float newFireRate) => fireRate = newFireRate;
    public void SetCanShoot(bool value) => canShoot = value;
    public void SetIsDeadly(bool value) => isDeadly = value;
    public void SetMuzzleFlash(GameObject flash) => muzzleFlash = flash;
    public void SetHitEffect(GameObject effect) => hitEffect = effect;
}

public interface IHealth
{
    void TakeDamage(float amount);
}