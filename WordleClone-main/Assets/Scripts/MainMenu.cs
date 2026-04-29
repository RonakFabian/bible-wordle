using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class MainMenu : MonoBehaviour
{
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

    [Header("Timing")]
    public float fadeDuration = 0.55f;
    public float textFadeDuration = 0.45f;
    public float buttonFadeDuration = 0.4f;
    public float buttonSlide = 28f;
    public Ease fadeEase = Ease.OutCubic;

    void Start()
    {
        logoGroup.alpha = 0f;
        bibleTextGroup.alpha = 0f;
        wordleTextGroup.alpha = 0f;
        InitButtonGroup(button1Group, button1Rect);
        InitButtonGroup(button2Group, button2Rect);
        InitButtonGroup(button3Group, button3Rect);

        Sequence seq = DOTween.Sequence();

        seq.Append(logoGroup.DOFade(1f, fadeDuration).SetEase(fadeEase));

        seq.AppendInterval(0.1f);
        seq.Append(bibleTextGroup.DOFade(1f, textFadeDuration).SetEase(fadeEase));

        seq.AppendInterval(0.05f);
        seq.Append(wordleTextGroup.DOFade(1f, textFadeDuration).SetEase(Ease.OutBack));

        seq.AppendInterval(0.2f);
        seq.Append(AnimateButton(button1Group, button1Rect));

        seq.AppendInterval(0.08f);
        seq.Append(AnimateButton(button2Group, button2Rect));

        seq.AppendInterval(0.08f);
        seq.Append(AnimateButton(button3Group, button3Rect));
    }

    Tween AnimateButton(CanvasGroup group, RectTransform rt)
    {
        Vector2 end = rt.anchoredPosition;
        rt.anchoredPosition = end + Vector2.down * buttonSlide;

        Sequence s = DOTween.Sequence();
        s.Join(group.DOFade(1f, buttonFadeDuration).SetEase(Ease.OutCubic));
        s.Join(rt.DOAnchorPos(end, buttonFadeDuration).SetEase(Ease.OutBack));
        return s;
    }

    void InitButtonGroup(CanvasGroup group, RectTransform rt)
    {
        group.alpha = 0f;
        rt.anchoredPosition += Vector2.down * buttonSlide;
    }

    public void OpenLevel(string s)
    {
        SceneManager.LoadScene(s);
    }
}
