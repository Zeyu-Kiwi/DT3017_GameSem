using UnityEngine;

public class PlayerResetController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private GameObject player;
    [SerializeField] private Transform workstationReturnPoint;

    [Header("Workstation")]
    [SerializeField] private CraftingStation craftingStation;

    private FirstPersonController firstPersonController;
    private PlayerInteractor playerInteractor;
    private PlayerInteractUI playerInteractUI;
    private Rigidbody playerRigidbody;
    private bool controlsLocked;

    public Transform WorkstationReturnPoint => workstationReturnPoint;
    public bool ControlsLocked => controlsLocked;

    private void Awake()
    {
        CachePlayerComponents();
    }

    public void LockPlayer()
    {
        CachePlayerComponents();

        if (playerInteractUI != null)
        {
            playerInteractUI.Hide();
        }

        if (playerInteractor != null)
        {
            playerInteractor.enabled = false;
        }

        if (firstPersonController != null)
        {
            firstPersonController.DisableController();
            firstPersonController.enabled = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        StopPlayerMotion();
        controlsLocked = true;
    }

    public void ResetPlayerAndStation()
    {
        if (craftingStation != null)
        {
            craftingStation.ResetStationImmediately(false);
        }

        ReturnPlayerToWorkstation();
    }

    public void ReturnPlayerToWorkstation()
    {
        CachePlayerComponents();

        if (player == null || workstationReturnPoint == null)
        {
            Debug.LogError(
                "PlayerResetController needs a Player and Workstation Return Point.",
                this);
            return;
        }

        StopPlayerMotion();
        player.transform.SetPositionAndRotation(
            workstationReturnPoint.position,
            workstationReturnPoint.rotation);

        if (firstPersonController != null)
        {
            firstPersonController.ResetLook();
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.position = workstationReturnPoint.position;
            playerRigidbody.rotation = workstationReturnPoint.rotation;
        }

        Physics.SyncTransforms();
    }

    public void UnlockPlayer()
    {
        CachePlayerComponents();

        if (firstPersonController != null)
        {
            firstPersonController.enabled = true;
            firstPersonController.EnableController();
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (playerInteractor != null)
        {
            playerInteractor.enabled = true;
        }

        controlsLocked = false;
    }

    private void CachePlayerComponents()
    {
        if (player == null)
        {
            return;
        }

        if (firstPersonController == null)
        {
            firstPersonController = player.GetComponent<FirstPersonController>();
        }

        if (playerInteractor == null)
        {
            playerInteractor = player.GetComponent<PlayerInteractor>();
        }

        if (playerInteractUI == null)
        {
            playerInteractUI = player.GetComponent<PlayerInteractUI>();
        }

        if (playerRigidbody == null)
        {
            playerRigidbody = player.GetComponent<Rigidbody>();
        }
    }

    private void StopPlayerMotion()
    {
        if (playerRigidbody == null)
        {
            return;
        }

        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;
    }
}
