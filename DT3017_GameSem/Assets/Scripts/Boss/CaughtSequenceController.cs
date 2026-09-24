using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public class CaughtSequenceController : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private PlayerResetController playerResetController;
    [SerializeField] private DailyQuotaManager quotaManager;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [Tooltip("Canvas child containing the RawImage that displays the video's RenderTexture.")]
    [SerializeField] private GameObject videoRoot;

    [Header("Optional Events")]
    [SerializeField] private UnityEvent onSequenceStarted;
    [SerializeField] private UnityEvent onSequenceFinished;

    [Header("Runtime State (Debug)")]
    [SerializeField] private bool sequenceRunning;

    private BossController activeBoss;

    public bool SequenceRunning => sequenceRunning;

    private void Awake()
    {
        if (quotaManager == null)
        {
            quotaManager = DailyQuotaManager.Instance;
        }

        if (videoRoot != null)
        {
            videoRoot.SetActive(false);
        }

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
        }
    }

    public void PlayCaughtSequence(BossController boss)
    {
        if (sequenceRunning)
        {
            return;
        }

        sequenceRunning = true;
        activeBoss = boss;

        if (activeBoss != null)
        {
            activeBoss.SetSequencePaused(true);
            activeBoss.ResetOutsideAndRestartTimer();
            activeBoss.SetSequencePaused(true);
        }

        if (playerResetController != null)
        {
            playerResetController.LockPlayer();
            playerResetController.ResetPlayerAndStation();
        }

        if (quotaManager != null)
        {
            quotaManager.AddCaughtPenalty();
        }

        if (videoRoot != null)
        {
            videoRoot.SetActive(true);
        }

        onSequenceStarted?.Invoke();

        if (!HasConfiguredVideo())
        {
            Debug.LogError(
                "CaughtSequenceController has no playable video configured.",
                this);
            FinishSequence();
            return;
        }

        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.Stop();
        videoPlayer.Play();
    }

    private bool HasConfiguredVideo()
    {
        if (videoPlayer == null)
        {
            return false;
        }

        if (videoPlayer.source == VideoSource.VideoClip)
        {
            return videoPlayer.clip != null;
        }

        return !string.IsNullOrEmpty(videoPlayer.url);
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        FinishSequence();
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError("Caught video failed: " + message, this);
        FinishSequence();
    }

    private void FinishSequence()
    {
        if (!sequenceRunning)
        {
            return;
        }

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.Stop();
        }

        if (videoRoot != null)
        {
            videoRoot.SetActive(false);
        }

        if (playerResetController != null)
        {
            playerResetController.UnlockPlayer();
        }

        if (activeBoss != null)
        {
            activeBoss.SetSequencePaused(false);
        }

        activeBoss = null;
        sequenceRunning = false;
        onSequenceFinished?.Invoke();
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.errorReceived -= OnVideoError;
        }
    }
}
