using UnityEngine;

/// <summary>Put this on the chair root; its visible colliders can be children.</summary>
[DisallowMultipleComponent]
public class ChairInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private PlayerSeatController playerSeat;

    [Header("Audio")]
    [SerializeField] private AudioClip sitClip;
    [SerializeField] private AudioClip standClip;
    [SerializeField, Range(0f, 1f)] private float chairVolume = 1f;

    public bool CanInteract => Seat != null && Seat.HasPositions &&
                               !Seat.IsTransitioning;

    private PlayerSeatController Seat => playerSeat != null
        ? playerSeat : PlayerSeatController.Instance;

    public void Interact(GameObject player)
    {
        PlayerSeatController seat = player != null
            ? player.GetComponent<PlayerSeatController>() : Seat;
        if (CanInteract && seat != null && seat == Seat)
        {
            WorldAudioOneShot.Play(seat.IsSeated ? standClip : sitClip,
                transform.position, chairVolume);
            seat.ToggleSeat();
        }
    }
}
