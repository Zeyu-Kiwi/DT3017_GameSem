using UnityEngine;

/// <summary>Plays prying sounds by observing the BoardedHole minigame's public state.</summary>
[DisallowMultipleComponent]
public sealed class BoardedHoleAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardedHolePryingMinigame minigame;
    [Tooltip("Assign the same planks, in the same order, as BoardedHolePryingMinigame.")]
    [SerializeField] private Transform[] planks;
    [Tooltip("Match the minigame's Pry Key (currently Space).")]
    [SerializeField] private KeyCode pryKey = KeyCode.Space;

    [Header("Missing crowbar (World SFX)")]
    [SerializeField] private AudioClip missingToolClip;
    [SerializeField, Range(0f, 1f)] private float missingToolVolume = 1f;

    [Header("Crowbar attach (World SFX)")]
    [SerializeField] private AudioClip crowbarAttachClip;
    [SerializeField, Range(0f, 1f)] private float crowbarAttachVolume = 1f;

    [Header("Prying (World SFX)")]
    [SerializeField] private AudioClip pryingClip;
    [SerializeField, Range(0f, 1f)] private float pryingVolume = 1f;
    [Tooltip("Random pitch range for each accepted pry press.")]
    [SerializeField] private Vector2 pryingPitchRange = new Vector2(0.9f, 1.1f);

    [Header("Plank detach (World SFX)")]
    [SerializeField] private AudioClip plankDetachClip;
    [SerializeField, Range(0f, 1f)] private float plankDetachVolume = 1f;

    [Header("Plank floor impact (World SFX)")]
    [SerializeField] private AudioClip plankImpactClip;
    [SerializeField, Range(0f, 1f)] private float plankImpactVolume = 1f;

    private BoardedHolePryingMinigame.PryStage previousStage;
    private int previousSelectedPlank;
    private int previousRemovedPlanks;

    private void Awake()
    {
        if (minigame == null) minigame = GetComponent<BoardedHolePryingMinigame>();
        if (minigame == null)
            Debug.LogError("BoardedHoleAudio needs the BoardedHolePryingMinigame on this object or in its Minigame field.", this);
    }

    private void OnEnable()
    {
        Snapshot();
    }

    // Connect ToolSelectionInteractable's On No Tools Available event to this method.
    public void PlayMissingToolSound()
    {
        WorldAudioOneShot.Play(missingToolClip, transform.position, missingToolVolume);
    }

    private void LateUpdate()
    {
        if (minigame == null) return;

        var stage = minigame.Stage;
        int selected = minigame.SelectedPlankIndex;
        int removed = minigame.RemovedPlanks;

        if (minigame.IsRunning && Time.timeScale > 0f)
        {
            if (previousStage == BoardedHolePryingMinigame.PryStage.MovingCrowbar &&
                stage == BoardedHolePryingMinigame.PryStage.Prying)
            {
                WorldAudioOneShot.Play(crowbarAttachClip, PlankPosition(selected), crowbarAttachVolume);
            }

            // The minigame processes Space in Update. By LateUpdate, a positive
            // mash value and the current stage confirm an accepted press.
            if (Input.GetKeyDown(pryKey) && minigame.MashValue > 0f &&
                (stage == BoardedHolePryingMinigame.PryStage.Prying ||
                 stage == BoardedHolePryingMinigame.PryStage.Ripping) &&
                (previousStage == BoardedHolePryingMinigame.PryStage.Prying ||
                 stage == BoardedHolePryingMinigame.PryStage.Prying))
            {
                WorldAudioOneShot.Play(pryingClip, PlankPosition(selected), pryingVolume,
                    Random.Range(pryingPitchRange.x, pryingPitchRange.y));
            }

            if (removed > previousRemovedPlanks)
            {
                int detachedIndex = previousSelectedPlank >= 0 ? previousSelectedPlank : selected;
                Transform plank = PlankAt(detachedIndex);
                Vector3 position = plank != null ? plank.position : transform.position;
                WorldAudioOneShot.Play(plankDetachClip, position, plankDetachVolume);

                // The minigame creates a Rigidbody when the plank detaches.
                // Its first physics collision happens after this LateUpdate.
                if (plank != null && plankImpactClip != null)
                    plank.gameObject.AddComponent<AudioImpactOnCollision>()
                        .Configure(plankImpactClip, plankImpactVolume);
            }
        }

        previousStage = stage;
        if (selected >= 0) previousSelectedPlank = selected;
        previousRemovedPlanks = removed;
    }

    private Transform PlankAt(int index)
    {
        return planks != null && index >= 0 && index < planks.Length ? planks[index] : null;
    }

    private Vector3 PlankPosition(int index)
    {
        Transform plank = PlankAt(index);
        return plank != null ? plank.position : transform.position;
    }

    private void Snapshot()
    {
        if (minigame == null) return;
        previousStage = minigame.Stage;
        previousSelectedPlank = minigame.SelectedPlankIndex;
        previousRemovedPlanks = minigame.RemovedPlanks;
    }

    private void OnValidate()
    {
        if (minigame == null) minigame = GetComponent<BoardedHolePryingMinigame>();
        pryingPitchRange.x = Mathf.Clamp(pryingPitchRange.x, 0.1f, 3f);
        pryingPitchRange.y = Mathf.Clamp(pryingPitchRange.y, pryingPitchRange.x, 3f);
    }
}
