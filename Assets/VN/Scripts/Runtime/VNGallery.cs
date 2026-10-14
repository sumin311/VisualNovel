using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    /// <summary>
    /// 타이틀의 "갤러리". 게임에서 본 CG(@cg)와 엔딩(@ending)이 해금되어 여기서 다시 볼 수 있어요.
    /// CG 목록 = Assets/Resources/CG/ 안의 모든 그림 (이름순)
    /// 엔딩 목록 = 스토리 파일들에 적힌 @ending 전부 (파일 이름순, 위에서부터)
    /// </summary>
    public class VNGallery : MonoBehaviour
    {
        [Header("참조 (씬 생성 시 자동 연결)")]
        public Button cgTabButton;
        public Button endingTabButton;
        public Button closeButton;
        public Text countText;
        public GameObject cgPage;
        public RectTransform cgContainer;
        [Tooltip("Thumb(Image) / Label(Text) 자식을 가진 버튼")]
        public Button cgSlotTemplate;
        public GameObject endingPage;
        public RectTransform endingContainer;
        public Text endingRowTemplate;
        [Header("CG 크게 보기")]
        public GameObject viewer;
        public Image viewerImage;

        public bool IsOpen => gameObject.activeSelf;
        public bool ViewerOpen => viewer.activeSelf;

        readonly List<GameObject> spawned = new List<GameObject>();
        bool cgTab = true;

        static readonly Color TabOn = new Color(0.88f, 0.38f, 0.56f);
        static readonly Color TabOff = new Color(0.55f, 0.44f, 0.41f);

        void Awake()
        {
            MakeScrollable();
            closeButton.onClick.AddListener(Close);
            cgTabButton.onClick.AddListener(() => ShowTab(true));
            endingTabButton.onClick.AddListener(() => ShowTab(false));
            viewer.GetComponent<Button>().onClick.AddListener(CloseViewer);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            viewer.SetActive(false);
            ShowTab(cgTab);
        }

        public void Close() => gameObject.SetActive(false);
        public void CloseViewer() => viewer.SetActive(false);

        /// <summary>CG 가 많아져도 창 밖으로 넘치지 않게, CG 칸 묶음을 위아래로 스크롤되는 영역에 넣음</summary>
        void MakeScrollable()
        {
            var page = (RectTransform)cgPage.transform;
            if (page.GetComponentInParent<ScrollRect>() != null) return;

            var scroll = new GameObject("CGScroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect))
                .GetComponent<RectTransform>();
            scroll.gameObject.layer = page.gameObject.layer;
            scroll.SetParent(page.parent, false);
            scroll.SetSiblingIndex(page.GetSiblingIndex());
            scroll.anchorMin = page.anchorMin;
            scroll.anchorMax = page.anchorMax;
            scroll.pivot = page.pivot;
            scroll.offsetMin = page.offsetMin;
            scroll.offsetMax = page.offsetMax;
            scroll.GetComponent<Image>().color = new Color(0, 0, 0, 0); // 빈 곳을 끌어도 스크롤되게

            page.SetParent(scroll, false);
            page.anchorMin = new Vector2(0, 1);
            page.anchorMax = new Vector2(1, 1);
            page.pivot = new Vector2(0.5f, 1);
            page.offsetMin = page.offsetMax = Vector2.zero;
            var grid = page.GetComponent<GridLayoutGroup>();
            if (grid != null) grid.padding = new RectOffset(0, 0, 8, 8);
            page.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sr = scroll.GetComponent<ScrollRect>();
            sr.content = page;
            sr.viewport = scroll;
            sr.horizontal = false;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 40;
            cgPage = scroll.gameObject;
        }

        void ShowTab(bool cg)
        {
            cgTab = cg;
            cgPage.SetActive(cg);
            endingPage.SetActive(!cg);
            cgTabButton.GetComponentInChildren<Text>().color = cg ? TabOn : TabOff;
            endingTabButton.GetComponentInChildren<Text>().color = cg ? TabOff : TabOn;

            foreach (var g in spawned) Destroy(g);
            spawned.Clear();
            if (cg) BuildCGs();
            else BuildEndings();
        }

        void BuildCGs()
        {
            var names = new List<string>();
            foreach (var t in Resources.LoadAll<Texture2D>("CG"))
                if (!names.Contains(t.name)) names.Add(t.name);
            names.Sort(System.StringComparer.Ordinal);

            int unlocked = 0;
            foreach (var n in names)
            {
                bool open = VNUnlocks.HasCG(n);
                if (open) unlocked++;
                var b = Instantiate(cgSlotTemplate, cgContainer);
                b.gameObject.SetActive(true);
                spawned.Add(b.gameObject);
                var thumb = b.transform.Find("Thumb").GetComponent<Image>();
                var label = b.transform.Find("Label").GetComponent<Text>();
                if (open)
                {
                    var sp = VNStage.LoadSprite("CG/" + n);
                    thumb.sprite = sp;
                    thumb.enabled = sp != null;
                    label.text = n.Replace('_', ' ');
                    b.onClick.AddListener(() => ShowViewer(sp));
                }
                else
                {
                    thumb.enabled = false;
                    label.text = "?";
                    b.interactable = false;
                }
            }
            countText.text = $"CG  {unlocked} / {names.Count}";
        }

        void BuildEndings()
        {
            var all = AllEndings();
            int unlocked = 0;
            foreach (var e in all)
            {
                bool open = VNUnlocks.HasEnding(e);
                if (open) unlocked++;
                var row = Instantiate(endingRowTemplate, endingContainer);
                row.gameObject.SetActive(true);
                spawned.Add(row.gameObject);
                row.text = open ? "<color=#E0628F>♥</color>  " + e : "<color=#B8A39B>?  아직 못 본 엔딩</color>";
            }
            countText.text = $"엔딩  {unlocked} / {all.Count}";
        }

        /// <summary>스토리 파일에서 @ending 을 모두 찾아 순서대로 반환</summary>
        public static List<string> AllEndings()
        {
            var assets = new List<TextAsset>(Resources.LoadAll<TextAsset>(VNScript.Folder.TrimEnd('/')));
            assets.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            var list = new List<string>();
            foreach (var a in assets)
                foreach (var l in VNScript.Parse(a.name, a.text).lines)
                    if (l.type == VNLineType.Command && l.command == "ending" && l.args.Length > 0)
                    {
                        string name = string.Join(" ", l.args);
                        if (!list.Contains(name)) list.Add(name);
                    }
            return list;
        }

        void ShowViewer(Sprite sp)
        {
            if (sp == null) return;
            viewerImage.sprite = sp;
            viewer.SetActive(true);
        }
    }
}
