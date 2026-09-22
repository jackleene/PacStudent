using System.Collections;
using UnityEngine;

public class PacStudentMovement : MonoBehaviour
{
    [SerializeField] private Transform topLeft;
    [SerializeField] private Transform topRight;
    [SerializeField] private Transform bottomRight;
    [SerializeField] private Transform bottomLeft;
    [SerializeField] private float speed = 5f;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource moveAudio;

    private Transform[] points;

    private void Start()
    {
        points = new Transform[]
        {
            topLeft,
            topRight,
            bottomRight,
            bottomLeft
        };

        transform.position = topLeft.position;

        if (moveAudio != null)
        {
            moveAudio.loop = true;
            moveAudio.Play();
        }

        StartCoroutine(MoveLoop());
    }

    private IEnumerator MoveLoop()
    {
        int index = 0;

        while (true)
        {
            int nextIndex = (index + 1) % points.Length;

            PlayDirection(index);

            yield return MoveBetween(
                points[index].position,
                points[nextIndex].position
            );

            index = nextIndex;
        }
    }

    private IEnumerator MoveBetween(Vector3 start, Vector3 end)
    {
        float distance = Vector3.Distance(start, end);
        float duration = distance / speed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        transform.position = end;
    }

    private void PlayDirection(int direction)
    {
        if (animator == null)
        {
            return;
        }

        switch (direction)
        {
            case 0:
                animator.Play("PacStudent_Right", 0, 0f);
                break;

            case 1:
                animator.Play("PacStudent_Down", 0, 0f);
                break;

            case 2:
                animator.Play("PacStudent_Left", 0, 0f);
                break;

            case 3:
                animator.Play("PacStudent_Up", 0, 0f);
                break;
        }
    }
}