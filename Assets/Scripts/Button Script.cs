using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class ButtonScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Local Audio Settings")]
    [Tooltip("If unassigned, sounds will route to the Global Audio Manager pool automatically.")]
    public AudioSource audioSource;
    public AudioClip hoverSound;
    public AudioClip clickSound;

    [Header("Hover Loop Configuration")]
    [Tooltip("Check this if you want the hover sound to loop continuously while highlighted!")]
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

    void OnDisable()
    {
        StopHoverPlayback();
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
        if (EventSystem.current == null || cachedSelectable == null) return;

        if (EventSystem.current.currentSelectedGameObject != gameObject && cachedSelectable.interactable)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
        {
            return;
        }
        StopHoverPlayback();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (isCurrentlyHighlighted) return;
        isCurrentlyHighlighted = true;

        if (hoverSound != null)
        {
            if (loopHoverSound && audioSource != null)
            {
                audioSource.clip = hoverSound;
                audioSource.loop = true;

                if (AudioManagerScript.Instance != null)
                    audioSource.volume = AudioManagerScript.Instance.globalSfxVolume;

                audioSource.Play();
            }
            else
            {
                if (audioSource != null) audioSource.PlayOneShot(hoverSound);
                else if (AudioManagerScript.Instance != null) AudioManagerScript.Instance.PlaySFX(hoverSound);
            }
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        StopHoverPlayback();
    }

    private void StopHoverPlayback()
    {
        isCurrentlyHighlighted = false;
        if (audioSource != null && audioSource.clip == hoverSound)
        {
            audioSource.Stop();
            audioSource.loop = false;
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
