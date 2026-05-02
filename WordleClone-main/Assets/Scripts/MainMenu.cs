using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class MainMenu : MonoBehaviour
{
    [Header("Logo Reveal Screen")]
    public CanvasGroup blackScreenGroup;    // full-black panel, starts at alpha 1
    public CanvasGroup revealScreenGroup;   // panel containing the logo, behind the black screen
    public RectTransform revealLogoRect;    // logo RectTransform inside revealScreenGroup
    public float fadeInDuration = 0.8f;     // black → logo fade-in
    public float revealHoldDuration = 1.2f; // logo hold time
    public float revealFadeOutDuration = 0.6f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip logoRevealClip;   // slot in Inspector

    [Header("UI Elements")]
    public CanvasGroup logoGroup;
    public CanvasGroup bibleTextGroup;
    public CanvasGroup wordleTextGroup;
    public CanvasGroup button1Group;
    public CanvasGroup button2Group;
    public CanvasGroup button3Group;

    [Header("Button RectTransforms")]
    public RectTransform button1Rect;
    public RectTransform button2Rect;
    public RectTransform button3Rect;

    [Header("Marquee")]
    public RectTransform[] marqueeRowsLeft;
    public RectTransform[] marqueeRowsRight;
    public float marqueeSpeed = 80f;

    [Header("Timing")]
    public float fadeDuration = 0.55f;
    public float textFadeDuration = 0.45f;
    public float buttonFadeDuration = 0.4f;
    public float buttonSlide = 28f;
    public Ease fadeEase = Ease.OutCubic;

    void Start()
    {
        // Hide main menu content immediately
        blackScreenGroup.gameObject.SetActive(true);

        logoGroup.alpha = 0f;
        bibleTextGroup.alpha = 0f;
        wordleTextGroup.alpha = 0f;
        InitButtonGroup(button1Group, button1Rect);
        InitButtonGroup(button2Group, button2Rect);
        InitButtonGroup(button3Group, button3Rect);

        // Start marquee scrolling behind the reveal screen
        foreach (var row in marqueeRowsLeft) StartMarquee(row, scrollLeft: true);
        foreach (var row in marqueeRowsRight) StartMarquee(row, scrollLeft: false);

        RunRevealThenMenu();
    }

    // ── Black → logo reveal → main menu sequence ─────────────────────────────

    void RunRevealThenMenu()
    {
        // Ensure black screen starts fully opaque; logo panel starts hidden
        if (blackScreenGroup != null) blackScreenGroup.alpha = 1f;
        if (revealScreenGroup != null) revealScreenGroup.alpha = 0f;

        // Capture the scene-authored scale so the tween lands exactly there
        Vector3 logoFinalScale = revealLogoRect != null ? revealLogoRect.localScale : Vector3.one;
        if (revealLogoRect != null)
            revealLogoRect.localScale = logoFinalScale * 0.8f;

        Sequence seq = DOTween.Sequence();

        // 1. Fade out black to reveal the logo panel beneath
        if (blackScreenGroup != null)
            seq.Append(blackScreenGroup.DOFade(0f, fadeInDuration).SetEase(Ease.OutQuad)
                .OnComplete(() => blackScreenGroup.gameObject.SetActive(false)));

        // 2. Fade in logo panel + scale to authored size + play audio
        seq.AppendCallback(() =>
        {
            PlayAudio(logoRevealClip, loop: false);

            if (revealScreenGroup != null)
                revealScreenGroup.DOFade(1f, fadeInDuration * 0.6f).SetEase(Ease.OutQuad);

            if (revealLogoRect != null)
                revealLogoRect.DOScale(logoFinalScale, fadeInDuration * 0.8f).SetEase(Ease.OutBack);
        });

        // 3. Hold on logo
        seq.AppendInterval(revealHoldDuration);

        // 4. Fade out logo panel
        if (revealScreenGroup != null)
            seq.Append(revealScreenGroup.DOFade(0f, revealFadeOutDuration).SetEase(Ease.InQuad)
                .OnComplete(() => revealScreenGroup.gameObject.SetActive(false)));

        // 5. Run main menu animation
        seq.OnComplete(RunMenuSequence);
    }

    void RunMenuSequence()
    {
        Sequence seq = DOTween.Sequence();

        seq.Append(logoGroup.DOFade(1f, fadeDuration).SetEase(fadeEase));

        seq.AppendInterval(0.1f);
        seq.Append(bibleTextGroup.DOFade(1f, textFadeDuration).SetEase(fadeEase));

        seq.AppendInterval(0.05f);
        seq.Append(wordleTextGroup.DOFade(1f, textFadeDuration).SetEase(Ease.OutBack));

        seq.AppendInterval(0.2f);
        seq.Append(AnimateButton(button1Group, button1Rect));

        // seq.AppendInterval(0.08f);
        // seq.Append(AnimateButton(button2Group, button2Rect));

        seq.AppendInterval(0.08f);
        seq.Append(AnimateButton(button3Group, button3Rect));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    Tween AnimateButton(CanvasGroup group, RectTransform rt)
    {
        Vector2 end = rt.anchoredPosition;
        rt.anchoredPosition = end + Vector2.down * buttonSlide;

        Sequence s = DOTween.Sequence();
        s.Join(group.DOFade(1f, buttonFadeDuration).SetEase(Ease.OutCubic));
        s.Join(rt.DOAnchorPos(end, buttonFadeDuration).SetEase(Ease.OutBack));
        return s;
    }

    void StartMarquee(RectTransform row, bool scrollLeft)
    {
        float width = row.rect.width;
        float start = scrollLeft ? width * 0.5f : -width * 0.5f;
        float end = scrollLeft ? -width * 0.5f : width * 0.5f;
        float duration = width / marqueeSpeed;

        row.anchoredPosition = new Vector2(start, row.anchoredPosition.y);
        row.DOAnchorPosX(end, duration)
           .SetEase(Ease.Linear)
           .SetLoops(-1, LoopType.Restart)
           .OnStepComplete(() => row.anchoredPosition = new Vector2(start, row.anchoredPosition.y));
    }

    void InitButtonGroup(CanvasGroup group, RectTransform rt)
    {
        group.alpha = 0f;
        rt.anchoredPosition += Vector2.down * buttonSlide;
    }

    void PlayAudio(AudioClip clip, bool loop)
    {
        if (audioSource == null || clip == null) return;
        audioSource.loop = loop;
        audioSource.clip = clip;
        audioSource.Play();
    }

    public void OpenLevel(string s)
    {
        SceneManager.LoadScene(s);
    }
}
