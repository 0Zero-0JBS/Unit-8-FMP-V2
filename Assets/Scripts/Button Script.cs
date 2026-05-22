using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class ButtonScript : MonoBehaviour, IPointerEnterHandler
{
    [Header("Local Audio Settings")]
    public AudioSource audioSource;
    public AudioClip clickSound;

    [Header("Hover Loop Configuration")]
    public bool loopHoverSound = false;

    [Header("Controller Focus Settings")]
    public bool isFirstSelectedButton = false;

    private Selectable cachedSelectable;
    private bool isCurrentlyHighlighted = false;

    void Awake()
    {
        cachedSelectable = GetComponent<Selectable>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void OnEnable()
    {
        if (isFirstSelectedButton)
        {
            StartCoroutine(SafeFocusRoutine());
        }
    }

    private IEnumerator SafeFocusRoutine()
    {
        yield return null;
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (EventSystem.current == null || cachedSelectable == null || !cachedSelectable.interactable) return;

        if (Input.GetAxisRaw("Mouse X") != 0 || Input.GetAxisRaw("Mouse Y") != 0)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }

    public void PlayClickSound()
    {
        if (clickSound != null)
        {
            if (audioSource != null) audioSource.PlayOneShot(clickSound);
            else if (AudioManagerScript.Instance != null) AudioManagerScript.Instance.PlaySFX(clickSound);
        }
    }
}
