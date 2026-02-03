using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("Splash Screen")]
    [SerializeField] private CanvasGroup splashPanel;
    [SerializeField] private Image splashImage;
    [SerializeField] private float splashDuration = 3f;
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("Main Menu")]
    [SerializeField] private CanvasGroup mainMenuPanel;
    [SerializeField] private Image logoImage;
    [SerializeField] private CanvasGroup logoCanvasGroup;

    [Header("Menu Buttons")]
    [SerializeField] private Image playImage;
    [SerializeField] private Image optionsImage;
    [SerializeField] private Image tutorialImage;
    [SerializeField] private Image quitImage;

    [Header("Scene References")]
    [SerializeField] private string playSceneName = "GameScene";
    [SerializeField] private string tutorialSceneName = "TutorialScene";

    [Header("Hover Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(0.7f, 0.7f, 0.7f, 1f); // Darker gray

    private void Start()
    {
        // Set initial states
        if (mainMenuPanel != null)
            mainMenuPanel.alpha = 0f;

        if (splashPanel != null)
            splashPanel.alpha = 1f;

        // Start splash image as transparent
        if (splashImage != null)
        {
            Color splashColor = splashImage.color;
            splashColor.a = 0f;
            splashImage.color = splashColor;
        }

        if (logoCanvasGroup == null && logoImage != null)
            logoCanvasGroup = logoImage.GetComponent<CanvasGroup>();

        // Setup button listeners
        SetupButtonListeners();

        // Start splash screen sequence
        StartCoroutine(PlaySplashScreen());
    }

    private IEnumerator PlaySplashScreen()
    {
        // Fade in splash image
        yield return StartCoroutine(FadeInSplashImage());

        // Wait for splash duration
        yield return new WaitForSeconds(splashDuration);

        // Fade out splash and fade in main menu simultaneously
        yield return StartCoroutine(FadeTransition());
    }

    private IEnumerator FadeInSplashImage()
    {
        float elapsed = 0f;
        float fadeDuration = 0.5f;

        while (elapsed < fadeDuration && splashImage != null)
        {
            elapsed += Time.deltaTime;
            Color splashColor = splashImage.color;
            splashColor.a = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            splashImage.color = splashColor;
            yield return null;
        }

        if (splashImage != null)
        {
            Color splashColor = splashImage.color;
            splashColor.a = 1f;
            splashImage.color = splashColor;
        }
    }

    private IEnumerator FadeTransition()
    {
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;

            // Fade out splash
            if (splashPanel != null)
                splashPanel.alpha = Mathf.Lerp(1f, 0f, t);

            if (splashImage != null)
            {
                Color splashColor = splashImage.color;
                splashColor.a = Mathf.Lerp(1f, 0f, t);
                splashImage.color = splashColor;
            }

            // Fade in main menu
            if (mainMenuPanel != null)
                mainMenuPanel.alpha = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        // Ensure final values
        if (splashPanel != null)
            splashPanel.alpha = 0f;
        if (mainMenuPanel != null)
            mainMenuPanel.alpha = 1f;

        // Disable splash panel to prevent interactions
        if (splashPanel != null)
            splashPanel.gameObject.SetActive(false);
    }

    private void SetupButtonListeners()
    {
        // Play Image
        if (playImage != null)
        {
            EventTrigger playTrigger = playImage.GetComponent<EventTrigger>();
            if (playTrigger == null)
                playTrigger = playImage.gameObject.AddComponent<EventTrigger>();

            AddEventTrigger(playTrigger, EventTriggerType.PointerEnter, OnPlayHoverEnter);
            AddEventTrigger(playTrigger, EventTriggerType.PointerExit, OnPlayHoverExit);
            AddEventTrigger(playTrigger, EventTriggerType.PointerClick, OnPlayClicked);
        }

        // Options Image
        if (optionsImage != null)
        {
            EventTrigger optionsTrigger = optionsImage.GetComponent<EventTrigger>();
            if (optionsTrigger == null)
                optionsTrigger = optionsImage.gameObject.AddComponent<EventTrigger>();

            AddEventTrigger(optionsTrigger, EventTriggerType.PointerEnter, OnOptionsHoverEnter);
            AddEventTrigger(optionsTrigger, EventTriggerType.PointerExit, OnOptionsHoverExit);
            AddEventTrigger(optionsTrigger, EventTriggerType.PointerClick, OnOptionsClicked);
        }

        // Tutorial Image
        if (tutorialImage != null)
        {
            EventTrigger tutorialTrigger = tutorialImage.GetComponent<EventTrigger>();
            if (tutorialTrigger == null)
                tutorialTrigger = tutorialImage.gameObject.AddComponent<EventTrigger>();

            AddEventTrigger(tutorialTrigger, EventTriggerType.PointerEnter, OnTutorialHoverEnter);
            AddEventTrigger(tutorialTrigger, EventTriggerType.PointerExit, OnTutorialHoverExit);
            AddEventTrigger(tutorialTrigger, EventTriggerType.PointerClick, OnTutorialClicked);
        }

        // Quit Image
        if (quitImage != null)
        {
            EventTrigger quitTrigger = quitImage.GetComponent<EventTrigger>();
            if (quitTrigger == null)
                quitTrigger = quitImage.gameObject.AddComponent<EventTrigger>();

            AddEventTrigger(quitTrigger, EventTriggerType.PointerEnter, OnQuitHoverEnter);
            AddEventTrigger(quitTrigger, EventTriggerType.PointerExit, OnQuitHoverExit);
            AddEventTrigger(quitTrigger, EventTriggerType.PointerClick, OnQuitClicked);
        }
    }

    private void AddEventTrigger(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction<BaseEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = eventType;
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }

    // Play Image Hover
    private void OnPlayHoverEnter(BaseEventData data) => ChangeImageColor(playImage, hoverColor);
    private void OnPlayHoverExit(BaseEventData data) => ChangeImageColor(playImage, normalColor);

    // Options Image Hover
    private void OnOptionsHoverEnter(BaseEventData data) => ChangeImageColor(optionsImage, hoverColor);
    private void OnOptionsHoverExit(BaseEventData data) => ChangeImageColor(optionsImage, normalColor);

    // Tutorial Image Hover
    private void OnTutorialHoverEnter(BaseEventData data) => ChangeImageColor(tutorialImage, hoverColor);
    private void OnTutorialHoverExit(BaseEventData data) => ChangeImageColor(tutorialImage, normalColor);

    // Quit Image Hover
    private void OnQuitHoverEnter(BaseEventData data) => ChangeImageColor(quitImage, hoverColor);
    private void OnQuitHoverExit(BaseEventData data) => ChangeImageColor(quitImage, normalColor);

    private void ChangeImageColor(Image image, Color color)
    {
        if (image != null)
            image.color = color;
    }

    // Image Click Handlers
    private void OnPlayClicked(BaseEventData data)
    {
        Debug.Log("Play button clicked!");
        // SceneManager.LoadScene(playSceneName);
    }

    private void OnOptionsClicked(BaseEventData data)
    {
        Debug.Log("Options button clicked!");
        // Open options menu
    }

    private void OnTutorialClicked(BaseEventData data)
    {
        Debug.Log("Tutorial button clicked!");
        SceneManager.LoadScene(tutorialSceneName);
    }

    private void OnQuitClicked(BaseEventData data)
    {
        Debug.Log("Quit button clicked!");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
