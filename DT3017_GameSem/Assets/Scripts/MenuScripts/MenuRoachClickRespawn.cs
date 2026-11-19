using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class MenuRoachClickRespawn : MonoBehaviour
{
    [Header("Respawn")]
    [SerializeField] private Transform spawnPoint;
    [Min(0f)] [SerializeField] private float respawnDelay = 2f;

    [Header("Death sound (2D)")]
    [SerializeField] private AudioClip deathClip;
    [Range(0f, 1f)] [SerializeField] private float deathVolume = 1f;

    [Header("Death effect")]
    [SerializeField] private ParticleSystem deathVfxPrefab;
    [Min(0.1f)] [SerializeField] private float deathVfxLifetime = 5f;

    private RoachWander wander;
    private AudioSource audioSource;
    private Renderer[] renderers;
    private Collider[] colliders;
    private bool[] rendererStates;
    private bool[] colliderStates;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool dead;

    private void Awake()
    {
        wander = GetComponent<RoachWander>();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        rendererStates = new bool[renderers.Length];
        colliderStates = new bool[colliders.Length];
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    private void Update()
    {
        if (dead || !Input.GetMouseButtonDown(0) || PointerIsOnClickableUI()) return;

        // OnMouseDown only sees cameras rendering to the screen. The menu's
        // world camera renders to a texture, so cast from it explicitly.
        Camera worldCamera = Camera.main;
        if (worldCamera == null) return;
        Ray ray = worldCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, worldCamera.farClipPlane,
                worldCamera.cullingMask, QueryTriggerInteraction.Collide) ||
            hit.collider.GetComponentInParent<MenuRoachClickRespawn>() != this)
            return;

        StartCoroutine(DieAndRespawn());
    }

    private static bool PointerIsOnClickableUI()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return false;

        var pointer = new PointerEventData(eventSystem) { position = Input.mousePosition };
        var hits = new List<RaycastResult>();
        eventSystem.RaycastAll(pointer, hits);

        foreach (RaycastResult hit in hits)
        {
            if (hit.gameObject.GetComponentInParent<Selectable>() != null ||
                ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject) != null)
                return true;
        }
        return false;
    }

    private IEnumerator DieAndRespawn()
    {
        dead = true;
        Vector3 deathPosition = transform.position;

        if (deathClip != null)
            audioSource.PlayOneShot(deathClip, deathVolume);

        if (deathVfxPrefab != null)
        {
            ParticleSystem effect = Instantiate(deathVfxPrefab, deathPosition, Quaternion.identity);
            effect.Play(true);
            Destroy(effect.gameObject, deathVfxLifetime);
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            rendererStates[i] = renderers[i].enabled;
            renderers[i].enabled = false;
        }
        for (int i = 0; i < colliders.Length; i++)
        {
            colliderStates[i] = colliders[i].enabled;
            colliders[i].enabled = false;
        }
        if (wander != null)
        {
            if (wander.animator != null) wander.animator.speed = 0f;
            wander.enabled = false;
        }

        yield return new WaitForSeconds(respawnDelay);

        transform.SetPositionAndRotation(
            spawnPoint != null ? spawnPoint.position : initialPosition,
            spawnPoint != null ? spawnPoint.rotation : initialRotation);
        if (wander != null)
        {
            wander.ReturnToWanderArea();
            wander.enabled = true;
        }
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].enabled = rendererStates[i];
        for (int i = 0; i < colliders.Length; i++)
            if (colliders[i] != null) colliders[i].enabled = colliderStates[i];
        dead = false;
    }
}
