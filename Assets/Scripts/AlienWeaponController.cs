using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Added only to InnovationScene; the standard level keeps its original controls.
public class AlienWeaponController : MonoBehaviour
{
    [SerializeField] private PacStudentController player;
    [SerializeField] private RoundController round;
    [SerializeField] private LevelGenerator level;
    [SerializeField] private EnergyProjectile projectilePrefab;
    [SerializeField] private Text fireHint;
    [SerializeField] private AudioSource effectsAudio;
    [SerializeField] private AudioClip fireSound;

    private void LateUpdate()
    {
        // Read the facing after player movement has resolved buffered turns.
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) TryFire();
        fireHint.gameObject.SetActive(round.IsPlaying);
    }

    public bool TryFire()
    {
        if (!round.TryConsumePowerPill()) return false;
        EnergyProjectile shot = Instantiate(projectilePrefab, player.transform.position, Quaternion.identity);
        shot.Launch(round, level, player.FacingDirection);
        effectsAudio.PlayOneShot(fireSound);
        return true;
    }
}
