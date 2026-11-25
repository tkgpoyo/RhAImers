using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using RhAImers.Battle;
using RhAImers.Input;
using RhAImers.VerseGeneration;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RhAImers.UI
{
    public sealed class TutorialController : MonoBehaviour
    {
        [Header("Existing battle UI")]
        public UIManager battleUI;
        public RhymeInputController inputController;
        public TMP_InputField inputField;
        public Button debugSubmitButton;
        public RectTransform opponentPanel;
        public RectTransform playerPanel;
        public RectTransform inputPanel;
        public RectTransform rhymeList;
        public RectTransform inputTimer;
        public RectTransform inputTheme;
        public RectTransform fusionRoot;
        public Canvas[] battleCanvases;

        [Header("Tutorial UI")]
        public TutorialDialogueView dialogue;
        public TutorialSpotlightOverlay spotlight;
        public GameObject selectionPanel;
        public Button[] themeButtons;
        public TMP_Text selectionTimer;
        public UIPanelTransition selectionTransition;
        public GameObject resultRoot;
        public RectTransform resultOverview;
        public GameObject scorePanel;
        public RectTransform countScore;
        public RectTransform hardnessScore;
        public RectTransform lengthScore;
        public RectTransform totalScore;
        public GameObject rankingPanel;
        public Button openRankingButton;
        public Button closeScoreButton;
        public Button resultReturnButton;
        public RectTransform commentRegion;
        public GameObject battleEndPanel;
        public Button battleEndResultButton;
        public ResultScoreRevealSequence scoreReveal;

        [Header("Tutorial audio")]
        public BattleBgmPresenter battleBgm;
        public global::SceneBgmPlayer resultBgm;
        public AudioSource navigationAudioSource;
        public AudioClip nextButtonClip;
        public AudioClip backButtonClip;
        public float navigationVolume = .85f;

        [Header("Fixed lesson data")]
        [TextArea(4, 8)] public string opponentVerse = "これはチュートリアル\nこれが君の練習になる\nまずは俺のラップとの比較\n楽しんでってねこの企画";
        [TextArea(4, 8)] public string playerVerse = "この一歩に乗せていく期待\n俺の言葉で動かす時代\n待つんじゃなく切り開く未来\n次の景色をこの目で見たい";
        public float generationPreviewSeconds = 2f;
        public float countdownPreviewSeconds = 5f;
        public string returnSceneName = "ModeSelectionScene";

        // The supplied script includes 06.5, 11.5, 11.6 and two separate passages under 16.
        private static readonly string[] Lines =
        {
            "チュートリアルへようこそ。ここでは、RhAImers（ライマーズ）の遊び方を説明していくぞ。\nチュートリアルでは「次へ」ボタンを押すことで次の説明へ移る。「戻る」ボタンで前の説明に戻る。理解できたら「次へ」を押してくれ。",
            "このゲームは、韻を踏む言葉を考えて、ラップを簡単に楽しむゲームだ。\n君の役目は、お題と韻を踏める単語を考えて入力すること。君が入力した単語を使って、AIが君のラップを作る。自分でラップの文章を書いたり、声に出して歌ったりする必要はないぞ。",
            "まずは決まった例で、操作を一緒に試していこう。説明を読んだら『次へ』。操作を頼んだ場面では、その操作ができるまで待つから、落ち着いて進めてくれ。",
            "最初に相手のラップが表示される。このように、ひとまとまりになったラップの文章を『バース』と呼ぶ。まずは読んでみよう。",
            "バースの中にある黄色い文字は、韻を踏んでいる単語だ。『比較』は『ひ・か・く』、『企画』は『き・か・く』。読むと響きが似ているだろ？　こうして音をそろえることを『韻を踏む』という。韻はライムとも呼ばれるぞ。",
            "韻を踏む手がかりは『あ・い・う・え・お』の音。これを母音という。母音を揃えることで、韻を踏むことができるんだ。\nさっきの例の『ひかく』と『きかく』は、どちらも母音が『い・あ・う』になる。だから韻を踏んだといえるんだ。",
            "次は君の番だ。まず、これから考える韻の基準になる『お題』を選ぶ。本番は3つの候補から10秒以内に選ぶが、今回は『未来』をクリックしてみよう。",
            "画面が変わったね。ここに先ほどのお題が出ている。今から、君にはこのお題で韻を踏める言葉を入力してもらうよ。",
            "お題の『未来』の読みは『み・ら・い』。母音は『い・あ・い』だ。同じく韻を踏める言葉として、『期待』や『時代』があるね。どちらも母音は『い・あ・い』で、同じ並びになる。意味や漢字が違っても、母音がそろえば韻を踏めるんだ。",
            "明るくなっている入力欄をクリックして、今回は漢字で『期待』と入力しよう。日本語の変換を確定してから、Enterキーで登録する。入力欄の下に『期待』が出れば成功だ。",
            "一覧に『期待』が出たな。これで韻が登録された。入力欄に文字があるだけでなく、この一覧に追加されたことを確かめよう。本番では、自分で考えた単語を登録できるぞ。",
            "同じ入力欄に、今度は『時代』と入力して登録してみよう。得点をつけるには2語以上の登録が必要だ。まずは『期待』と『時代』の二つをそろえよう。",
            "新しく登録した『時代』が上、その前の『期待』が下に並んでいるな。こうやって単語をどんどん追加することができる。",
            "取り消したい単語があれば、その単語の横の『×』を押す。取り消した単語は、もう一度入力すれば登録し直せるぞ。例として、韻を踏めていない単語である『練習』があるね。これを消してみよう。",
            "取り消しができたね。韻を踏めてない言葉を使うと、ラップの完成度が損なわれてしまう。できる限り韻を踏めている言葉を使うように注意しよう。",
            "入力欄の『00:30』は、韻を入力できる時間が残り30秒という意味だ。本番は、この時間内に単語を登録していく。『00:00』になると入力できる時間が終わる。今回は最後の5秒だけ進めるから、時計を見ていてくれ。",
            "入力はここまで。これから、登録した単語を材料にラップが作られる。単語が集まって光っている間は、その準備中だ。追加の操作はせず、表示されるまで待とう。今回は練習用に用意した文章を使うぞ。次に進んでくれ。",
            "これが君のバースだ。さっき登録した『期待』と『時代』が、文章に組み込まれているな。君が単語を選び、AIが文章にする。この役割分担でラップを作っていくんだ。",
            "相手のラップを見る、お題を選ぶ、単語を登録する、自分のラップを見る。ここまでで1ターンだ。本番は3ターン、同じように行う。今回は1ターンで終了する。右下の『NEXT』を押して、結果を見てみよう。",
            "それでは結果の見方を覚えよう。結果では、各ターンで君が入力した韻を元に、スコアが計算される。今からスコアの計算方法を説明しよう。",
            "『韻の個数』は、採点の対象になった登録単語の数だ。今回は『期待』『時代』の2個。実在しない言葉などは、採点から外れることがあるから気を付けてくれ。",
            "『韻の硬さ』は、お題と登録した単語の母音が、どれくらいそろっているかを表す。今回は『未来』『期待』『時代』の母音がそろっているので1.00だ。韻の硬さは0.00～1.00で変化し、母音がそろっているほど高くなる。",
            "『韻の長さ』は、登録単語の読みの平均の長さだ。今回は『きたい』『じだい』がどちらも3文字なので3.0。個数2 × 硬さ1.00 × 長さ3.0で、このターンは6点になる。本番は3ターン分を足した点数が最終スコアだ。",
            "画面右側の移動ボタンを押してみよう。ランキングでは過去の記録と点数を比べられ、コメントではラップへの評価を読める。長いコメントは、文章の上にマウスを置いてホイールでスクロールできるぞ。今回はどちらも練習用の表示だ。",
            "覚えておくのは、お題と音が似た単語を考え、一つずつ登録すること。より多くの、より硬い、より長い韻でハイスコアを目指そう。これでチュートリアルは終わりだ。『モード選択へ』で終えよう。"
        };

        private const int ThemeStep = 6;
        private const int FirstWordStep = 9;
        private const int SecondWordStep = 11;
        private const int RemovalStep = 13;
        private const int TimerStep = 15;
        private const int BattleEndStep = 18;
        private const int ScoreStep = 19;
        private const int RankingStep = 23;
        private int _step;
        private bool _busy;
        private bool _returning;
        private bool _themeChosen;
        private bool _resultTransitionStarted;
        private bool _changingInput;
        private string _expectedWord;
        private Coroutine _routine;
        private Coroutine _resultSlide;
        private Vector2 _scorePosition;
        private Vector2 _rankingPosition;

        public int CurrentStep => _step;
        public bool IsBusy => _busy;

        private void Awake()
        {
            inputController.UseVoiceInput = false;
            inputController.UseLiveTranscription = false;
            inputController.SetNotifySubmitRequested(false);
            if (debugSubmitButton != null) debugSubmitButton.gameObject.SetActive(false);
            battleEndResultButton.gameObject.SetActive(false);
            _scorePosition = ((RectTransform)scorePanel.transform).anchoredPosition;
            _rankingPosition = ((RectTransform)rankingPanel.transform).anchoredPosition;
        }

        private void OnEnable()
        {
            dialogue.NextRequested += Next;
            dialogue.BackRequested += Back;
            dialogue.ExitRequested += Exit;
            inputController.RhymeAdded += OnWordAdded;
            inputController.RhymeRemoved += OnWordRemoved;
            battleEndResultButton.onClick.AddListener(OnBattleEndResultClicked);
            openRankingButton.onClick.AddListener(OpenRanking);
            closeScoreButton.onClick.AddListener(CloseRanking);
            resultReturnButton.onClick.AddListener(ReturnFromResult);
            for (int i = 0; i < themeButtons.Length; i++)
            {
                int index = i;
                themeButtons[i].onClick.AddListener(() => ChooseTheme(index));
            }
        }

        private IEnumerator Start()
        {
            while (SceneFadeOverlay.IsTransitionActiveOrVisible) yield return null;
            Enter(0);
        }

        private void OnDisable()
        {
            dialogue.NextRequested -= Next;
            dialogue.BackRequested -= Back;
            dialogue.ExitRequested -= Exit;
            inputController.RhymeAdded -= OnWordAdded;
            inputController.RhymeRemoved -= OnWordRemoved;
            battleEndResultButton.onClick.RemoveListener(OnBattleEndResultClicked);
            openRankingButton.onClick.RemoveListener(OpenRanking);
            closeScoreButton.onClick.RemoveListener(CloseRanking);
            resultReturnButton.onClick.RemoveListener(ReturnFromResult);
            foreach (Button b in themeButtons) b.onClick.RemoveAllListeners();
            if (_routine != null) StopCoroutine(_routine);
            if (_resultSlide != null) StopCoroutine(_resultSlide);
            Time.timeScale = 1f;
        }

        public void Next()
        {
            if (_busy || !dialogue.nextButton.interactable) return;
            PlayNavigationSound(nextButtonClip);
            if (_step == TimerStep) _routine = StartCoroutine(InputCountdown());
            else if (_step == Lines.Length - 1) Exit();
            else Enter(_step + 1);
        }

        public void Back()
        {
            if (_busy || _step == 0) return;
            PlayNavigationSound(backButtonClip);
            Enter(_step - 1);
        }

        private void PlayNavigationSound(AudioClip clip)
        {
            if (navigationAudioSource != null && clip != null)
                navigationAudioSource.PlayOneShot(clip, navigationVolume);
        }

        private void Enter(int step)
        {
            _step = step;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(PresentStep());
        }

        private void ShowDialogue(bool ready, bool canGoBack = true)
            => dialogue.Show(Lines[_step], _step, Lines.Length, ready, canGoBack && _step > 0);

        private IEnumerator PresentStep()
        {
            _busy = true;
            _expectedWord = null;
            if (_step < ScoreStep)
            {
                if (resultBgm != null) resultBgm.Stop();
                if (battleBgm != null)
                {
                    if (_step < BattleEndStep) battleBgm.PlayBgm(false);
                    else battleBgm.StopBgm();
                }
            }
            battleEndPanel.SetActive(false);
            battleEndResultButton.gameObject.SetActive(false);
            foreach (Canvas canvas in battleCanvases) canvas.enabled = _step < ScoreStep;
            if (_resultSlide != null) { StopCoroutine(_resultSlide); _resultSlide = null; }
            selectionPanel.SetActive(false);
            if (_step < ScoreStep)
            {
                resultRoot.SetActive(false);
                scorePanel.SetActive(false);
                rankingPanel.SetActive(false);
            }
            inputField.interactable = false;
            if (debugSubmitButton != null) debugSubmitButton.gameObject.SetActive(false);
            ShowDialogue(false);
            RectTransform target = null;
            bool allowInput = false;
            bool ready = true;

            if (_step >= 3 && _step <= TimerStep &&
                !opponentPanel.GetComponentsInChildren<TMP_Text>(false).Any(t => t.name.StartsWith("VerseLine_")))
            {
                Focus(opponentPanel);
                yield return battleUI.ShowOpponentVerseAsync(MakeVerse(opponentVerse, "リアル", "になる", "比較", "企画")).ToCoroutine();
            }

            if (_step <= 2)
            {
                battleUI.HideBattlePhaseGroupsImmediate();
            }
            else if (_step <= 5)
            {
                battleUI.ShowOpponentVersePhase();
                target = opponentPanel;
            }
            else if (_step == ThemeStep)
            {
                battleUI.ShowOpponentVersePhase();
                selectionPanel.SetActive(true);
                selectionTimer.text = "残り 10 秒";
                foreach (Button button in themeButtons) button.interactable = false;
                themeButtons[0].interactable = true;
                selectionTransition.Show();
                target = (RectTransform)themeButtons[0].transform;
                allowInput = true;
                ready = _themeChosen;
            }
            else if (_step <= TimerStep)
            {
                if (_step <= FirstWordStep) PrepareInput();
                else if (_step <= SecondWordStep) PrepareInput("期待");
                else if (_step == RemovalStep) PrepareInput("期待", "時代", "練習");
                else PrepareInput("期待", "時代");

                if (_step == 7 || _step == 8) target = inputTheme;
                else if (_step == FirstWordStep || _step == SecondWordStep)
                {
                    _expectedWord = _step == FirstWordStep ? "期待" : "時代";
                    target = (RectTransform)inputField.transform;
                    inputField.interactable = true;
                    allowInput = true;
                    ready = false;
                }
                else if (_step == RemovalStep)
                {
                    Focus(rhymeList);
                    yield return null;
                    Button remove = FindPracticeRemoveButton();
                    foreach (Button button in rhymeList.GetComponentsInChildren<Button>(false))
                        button.interactable = button == remove;
                    target = remove != null ? (RectTransform)remove.transform.parent : rhymeList;
                    allowInput = remove != null;
                    ready = false;
                }
                else target = _step == TimerStep ? inputTimer : rhymeList;
            }
            else if (_step == 16)
            {
                battleUI.ShowGenerationLoading();
                Focus(fusionRoot);
                yield return new WaitForSecondsRealtime(Mathf.Max(0f, generationPreviewSeconds));
                target = fusionRoot;
            }
            else if (_step == 17)
            {
                Focus(playerPanel);
                yield return battleUI.ShowGeneratedVerseAsync(MakeVerse(playerVerse, "期待", "時代")).ToCoroutine();
                target = playerPanel;
            }
            else if (_step == BattleEndStep)
            {
                yield return ShowBattleEndAndScores();
                _routine = null;
                Enter(ScoreStep);
                yield break;
            }
            else if (_step < RankingStep)
            {
                battleUI.HideBattlePhaseGroupsImmediate();
                ShowResultPage(false);
                PlayResultBgm();
                target = _step == 20 ? countScore : _step == 21 ? hardnessScore : resultOverview;
            }
            else if (_step == RankingStep)
            {
                battleUI.HideBattlePhaseGroupsImmediate();
                ShowResultPage(false);
                PlayResultBgm();
                target = (RectTransform)openRankingButton.transform;
                allowInput = true;
                ready = false;
            }
            else
            {
                battleUI.HideBattlePhaseGroupsImmediate();
                ShowResultPage(true);
                PlayResultBgm();
            }

            ShowDialogue(ready);
            Focus(target, allowInput);
            _busy = false;
            _routine = null;
        }

        private void PrepareInput(params string[] words)
        {
            _changingInput = true;
            inputController.StartInput("未来");
            foreach (string word in words) inputController.AddRhyme(word);
            _changingInput = false;
            battleUI.UpdateInputTimer(30);
            inputField.interactable = false;
            if (debugSubmitButton != null) debugSubmitButton.gameObject.SetActive(false);
        }

        private void PlayResultBgm()
        {
            if (battleBgm != null) battleBgm.StopBgm();
            if (resultBgm != null) resultBgm.Play();
        }

        private Button FindPracticeRemoveButton()
        {
            TMP_Text word = rhymeList.GetComponentsInChildren<TMP_Text>(false)
                .FirstOrDefault(t => t.name == "WordText" && t.text == "練習");
            return word != null ? word.transform.parent.GetComponentsInChildren<Button>(false)
                .FirstOrDefault(b => b.name == "RemoveButton") : null;
        }

        private void ChooseTheme(int index)
        {
            if (_step != ThemeStep || index != 0) return;
            _themeChosen = true;
            dialogue.SetReady(true);
        }

        private void OnWordAdded(string word)
        {
            if (_changingInput || _expectedWord == null) return;
            bool correct = word == _expectedWord && inputController.Rhymes.Count(w => w == word) == 1;
            if (!correct)
            {
                _changingInput = true;
                inputController.RemoveRhymeAt(inputController.Rhymes.Count - 1);
                _changingInput = false;
                return;
            }
            inputField.interactable = false;
            dialogue.SetReady(true);
        }

        private void OnWordRemoved(string word)
        {
            if (_changingInput || _step != RemovalStep || word != "練習") return;
            if (inputController.Rhymes.Contains("練習")) return;
            dialogue.SetReady(true);
            Focus(rhymeList);
        }

        private IEnumerator InputCountdown()
        {
            _busy = true;
            ShowDialogue(false, false);
            Focus(inputTimer);
            float remaining = Mathf.Max(0f, countdownPreviewSeconds);
            while (remaining > 0f)
            {
                battleUI.UpdateInputTimer(Mathf.CeilToInt(remaining));
                yield return null;
                remaining -= Time.unscaledDeltaTime;
            }
            battleUI.UpdateInputTimer(0);
            inputController.Submit();
            yield return new WaitForSecondsRealtime(.5f);
            _routine = null;
            Enter(16);
        }

        private IEnumerator ShowBattleEndAndScores()
        {
            _resultTransitionStarted = false;
            battleUI.ShowScoringLoading();
            battleEndResultButton.gameObject.SetActive(true);
            battleEndResultButton.interactable = true;
            ShowDialogue(false);
            Focus((RectTransform)battleEndResultButton.transform, true);
            _busy = false;
            while (!_resultTransitionStarted) yield return null;
            while (SceneFadeOverlay.IsTransitionActiveOrVisible || !resultReturnButton.interactable) yield return null;
            battleEndPanel.SetActive(false);
            battleEndResultButton.gameObject.SetActive(false);
            dialogue.gameObject.SetActive(true);
            spotlight.gameObject.SetActive(true);
        }

        private void OnBattleEndResultClicked()
        {
            if (_step != BattleEndStep || _resultTransitionStarted || _returning) return;
            _busy = true;
            if (battleEndResultButton is global::BgmSceneTransitionButton || battleEndResultButton is global::FadeTransitionButton)
                ShowScoresAtBlack();
            else
                SceneFadeOverlay.Instance.PlayTransition(Color.black, .45f, .45f, .05f, true, ShowScoresAtBlack);
        }

        private void ShowScoresAtBlack()
        {
            if (this == null || _returning) return;
            _resultTransitionStarted = true;
            // Hide the canvas, allowing the native button to finish its reset coroutine.
            dialogue.gameObject.SetActive(false);
            spotlight.gameObject.SetActive(false);
            ShowResultPage(false);
            scoreReveal.Play();
        }

        private static Verse MakeVerse(string text, params string[] words)
            => new Verse(text, VerseHighlightFinder.Find(text, words));

        private void Focus(RectTransform target, bool allowInput = false)
        {
            spotlight.Focus(target, allowInput);
            dialogue.PlaceAwayFrom(target);
        }

        private void ShowResultPage(bool ranking)
        {
            foreach (Canvas canvas in battleCanvases) canvas.enabled = false;
            resultRoot.SetActive(true);
            scorePanel.SetActive(!ranking);
            rankingPanel.SetActive(ranking);
            ((RectTransform)scorePanel.transform).anchoredPosition = _scorePosition;
            ((RectTransform)rankingPanel.transform).anchoredPosition = _rankingPosition;
            SetPageAlpha(scorePanel, 1f);
            SetPageAlpha(rankingPanel, 1f);
            SetNavigation(openRankingButton, !ranking);
            SetNavigation(closeScoreButton, ranking);
            SetNavigation(resultReturnButton, true);
        }

        private static void SetPageAlpha(GameObject page, float alpha)
        {
            CanvasGroup group = page.GetComponent<CanvasGroup>();
            if (group == null) return;
            group.alpha = alpha; group.blocksRaycasts = alpha > .99f; group.interactable = alpha > .99f;
        }

        private static void SetNavigation(Button button, bool visible)
        {
            button.gameObject.SetActive(visible);
            button.interactable = visible;
            CanvasGroup group = button.GetComponent<CanvasGroup>();
            if (group != null) { group.alpha = visible ? 1f : 0f; group.blocksRaycasts = visible; group.interactable = visible; }
        }

        public void OpenRanking()
        {
            if (_step != RankingStep || _resultSlide != null) return;
            _resultSlide = StartCoroutine(SlideResult(true));
        }

        public void CloseRanking()
        {
            if (_step != RankingStep || _resultSlide != null) return;
            _resultSlide = StartCoroutine(SlideResult(false));
        }

        private IEnumerator SlideResult(bool opening)
        {
            _busy = true;
            dialogue.SetReady(false);
            Focus(null);
            scorePanel.SetActive(true); rankingPanel.SetActive(true);
            SetNavigation(openRankingButton, false); SetNavigation(closeScoreButton, false);
            var score = (RectTransform)scorePanel.transform;
            var ranking = (RectTransform)rankingPanel.transform;
            float width = ((RectTransform)score.parent).rect.width;
            float elapsed = 0f;
            while (elapsed < .45f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / .45f));
                if (!opening) t = 1f - t;
                score.anchoredPosition = _scorePosition + Vector2.left * width * t;
                ranking.anchoredPosition = _rankingPosition + Vector2.right * width * (1f - t);
                SetPageAlpha(scorePanel, Mathf.Lerp(1f, .25f, t));
                SetPageAlpha(rankingPanel, t);
                yield return null;
            }
            ShowResultPage(opening);
            dialogue.SetReady(opening);
            Focus(opening ? commentRegion : (RectTransform)openRankingButton.transform, true);
            _busy = false;
            _resultSlide = null;
        }

        private void ReturnFromResult()
        {
            if (resultReturnButton is global::BgmSceneTransitionButton || resultReturnButton is global::FadeTransitionButton)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(returnSceneName);
            }
            else Exit();
        }

        public void Exit()
        {
            if (_returning) return;
            _returning = true;
            dialogue.nextButton.interactable = false;
            dialogue.exitButton.interactable = false;
            SceneFadeOverlay.Instance.PlayTransition(Color.black, .4f, .4f, .05f, true,
                () => { Time.timeScale = 1f; SceneManager.LoadScene(returnSceneName); });
        }
    }
}
