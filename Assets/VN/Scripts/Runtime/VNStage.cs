using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    /// <summary>배경 / 캐릭터 스탠딩 / CG 표시를 담당합니다.</summary>
    public class VNStage : MonoBehaviour
    {
        [Header("레이어 (씬 생성 시 자동 연결)")]
        public Image backgroundBack;
        public Image backgroundFront;
        public RectTransform characterLayer;
        public Image cgImage;

        [Header("캐릭터 표시")]
        [Tooltip("스탠딩 이미지 높이 (기준 해상도 1920x1080)")]
        public float characterHeight = 1000f;
        [Tooltip("말하지 않는 캐릭터를 어둡게 표시")]
        public bool dimInactiveCharacters = true;
        public Color inactiveTint = new Color(0.6f, 0.6f, 0.65f, 1f);

        [Header("화자 배치 (말하는 사람 = 오른쪽 크게, 나머지 = 왼쪽에 좌우반전)")]
        [Tooltip("끄면 @show 에 적은 위치 그대로 표시")]
        public bool speakerLayout = true;
        [Tooltip("말하는 사람의 가로 위치 (0 = 왼쪽 끝, 1 = 오른쪽 끝)")]
        public float speakerX = 0.74f;
        [Tooltip("말하는 사람의 크기 배율")]
        public float speakerScale = 1.05f;
        [Tooltip("주변 사람들이 서는 구역 (가로 위치 시작~끝)")]
        public float sideMinX = 0.08f;
        public float sideMaxX = 0.40f;
        [Tooltip("주변 사람들의 크기 배율")]
        public float sideScale = 0.82f;
        [Tooltip("주변 사람들을 좌우반전 (말하는 사람 쪽을 보도록)")]
        public bool flipSide = true;

        public string CurrentBackground { get; private set; }
        public string CurrentCG { get; private set; }
        /// <summary>오른쪽에 크게 서 있는 화자 (없으면 null)</summary>
        public string CurrentSpeaker { get; private set; }

        class Character
        {
            public string name;
            public string expression;
            public float x;             // @show 로 정한 위치 (화자가 없을 때 사용)
            public Image image;
            public RectTransform rect;
            public Coroutine fade;
            public Coroutine move;
            public bool hiding;
        }

        readonly Dictionary<string, Character> characters = new Dictionary<string, Character>();
        readonly List<string> order = new List<string>();
        Coroutine bgRoutine, cgRoutine;
        Sprite pendingBg;

        // ───────────────────────── 이미지 로드 ─────────────────────────

        public static Sprite LoadSprite(string path, bool warnIfMissing = true)
        {
            var sp = Resources.Load<Sprite>(path);
            if (sp != null) return sp;
            var tex = Resources.Load<Texture2D>(path);
            if (tex != null)
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            if (warnIfMissing) Debug.LogWarning($"[VN] 이미지를 찾을 수 없습니다: Assets/Resources/{path}");
            return null;
        }

        public static float ParsePosition(string s, float fallback)
        {
            if (string.IsNullOrEmpty(s)) return fallback;
            switch (s.ToLowerInvariant())
            {
                case "left": case "왼쪽": return 0.22f;
                case "center": case "가운데": return 0.5f;
                case "right": case "오른쪽": return 0.78f;
            }
            if (float.TryParse(s, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float v))
                return Mathf.Clamp01(v);
            Debug.LogWarning($"[VN] 알 수 없는 위치 '{s}' (left / center / right 또는 0~1 숫자)");
            return fallback;
        }

        // ───────────────────────── 배경 ─────────────────────────

        public void SetBackground(string name, float duration)
        {
            if (name == "none" || name == "off") name = null;
            CurrentBackground = name;
            Sprite sp = string.IsNullOrEmpty(name) ? null : LoadSprite("Backgrounds/" + name);

            if (bgRoutine != null) { StopCoroutine(bgRoutine); bgRoutine = null; FinishBg(); }
            pendingBg = sp;

            if (duration <= 0f) { FinishBg(); return; }
            bgRoutine = StartCoroutine(BgFade(sp, duration));
        }

        IEnumerator BgFade(Sprite sp, float d)
        {
            if (sp != null)
            {
                SetImage(backgroundFront, sp, 0f);
                yield return Fade(backgroundFront, 0f, 1f, d);
            }
            else
            {
                yield return Fade(backgroundBack, backgroundBack.color.a, 0f, d);
            }
            FinishBg();
            bgRoutine = null;
        }

        void FinishBg()
        {
            SetImage(backgroundBack, pendingBg, 1f);
            SetImage(backgroundFront, null, 0f);
        }

        // ───────────────────────── 캐릭터 ─────────────────────────

        /// <param name="x">null 이면 기존 위치 유지 (새 캐릭터는 가운데)</param>
        public void ShowCharacter(string name, string expression, float? x, float fade)
        {
            if (characters.TryGetValue(name, out var c) && !c.hiding)
            {
                if (!string.IsNullOrEmpty(expression) && expression != c.expression)
                {
                    c.expression = expression;
                    ApplySprite(c);
                }
                if (x.HasValue && !Mathf.Approximately(x.Value, c.x))
                {
                    c.x = x.Value;
                    Arrange(fade > 0f ? 0.3f : 0f);
                }
                return;
            }

            if (c != null) RemoveImmediate(c);

            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = gameObject.layer;
            go.transform.SetParent(characterLayer, false);
            c = new Character
            {
                name = name,
                expression = string.IsNullOrEmpty(expression) ? VNScript.DefaultExpression : expression,
                x = x ?? 0.5f,
                image = go.GetComponent<Image>(),
                rect = (RectTransform)go.transform,
            };
            c.image.raycastTarget = false;
            c.image.preserveAspect = true;
            characters[name] = c;
            order.Add(name);

            ApplySprite(c);
            SetAlpha(c.image, 0f);
            Arrange(fade > 0f ? 0.3f : 0f, c);
            c.fade = StartCoroutine(Fade(c.image, 0f, 1f, fade));
        }

        public void SetExpression(string name, string expression)
        {
            if (characters.TryGetValue(name, out var c) && !c.hiding)
                ShowCharacter(name, expression, null, 0f);
        }

        public void HideCharacter(string name, float fade)
        {
            if (!characters.TryGetValue(name, out var c) || c.hiding) return;
            c.hiding = true;
            if (c.fade != null) StopCoroutine(c.fade);
            if (name == CurrentSpeaker) CurrentSpeaker = null;
            if (fade <= 0f) RemoveImmediate(c);
            else c.fade = StartCoroutine(HideRoutine(c, fade));
            Arrange(fade > 0f ? 0.3f : 0f);
        }

        public void HideAll(float fade)
        {
            foreach (var n in order.ToArray()) HideCharacter(n, fade);
        }

        /// <summary>
        /// 대사가 나올 때마다 호출. 무대 위의 사람이 말하면 그 사람을 오른쪽에 크게 세우고
        /// 나머지는 왼쪽으로. 나레이션이나 무대 밖 사람('나' 등)이 말하면 지금 배치를 유지.
        /// </summary>
        public void Focus(string speaker)
        {
            if (speaker != null && characters.TryGetValue(speaker, out var c) && !c.hiding && speaker != CurrentSpeaker)
            {
                CurrentSpeaker = speaker;
                Arrange(0.25f);
            }
        }

        /// <summary>화자 배치 규칙에 따라 모든 캐릭터의 위치·크기·반전·밝기를 맞춤</summary>
        /// <param name="instant">이 캐릭터는 애니메이션 없이 바로 제자리에 (새로 등장한 캐릭터)</param>
        void Arrange(float d, Character instant = null)
        {
            var active = new List<Character>();
            foreach (var n in order)
                if (!characters[n].hiding) active.Add(characters[n]);

            bool useSpeaker = speakerLayout && CurrentSpeaker != null &&
                              characters.TryGetValue(CurrentSpeaker, out var sp) && !sp.hiding;

            var side = new List<Character>();
            foreach (var c in active)
                if (!useSpeaker || c.name != CurrentSpeaker) side.Add(c);

            for (int i = 0; i < side.Count; i++)
            {
                var c = side[i];
                if (useSpeaker)
                {
                    float x = side.Count == 1 ? (sideMinX + sideMaxX) * 0.5f
                                              : Mathf.Lerp(sideMinX, sideMaxX, (float)i / (side.Count - 1));
                    MoveTo(c, x, flipSide ? -sideScale : sideScale, sideScale, c == instant ? 0f : d);
                    SetTint(c, dimInactiveCharacters ? inactiveTint : Color.white);
                }
                else
                {
                    MoveTo(c, c.x, 1f, 1f, c == instant ? 0f : d);
                    SetTint(c, Color.white);
                }
                c.rect.SetAsLastSibling();
            }

            if (useSpeaker)
            {
                var s = characters[CurrentSpeaker];
                MoveTo(s, speakerX, speakerScale, speakerScale, s == instant ? 0f : d);
                SetTint(s, Color.white);
                s.rect.SetAsLastSibling(); // 화자를 맨 앞에
            }
        }

        void MoveTo(Character c, float x, float scaleX, float scaleY, float d)
        {
            if (c.move != null) StopCoroutine(c.move);
            c.move = null;
            if (d <= 0f || !isActiveAndEnabled) { Place(c, x, scaleX, scaleY); return; }
            c.move = StartCoroutine(Move(c, x, scaleX, scaleY, d));
        }

        static void SetTint(Character c, Color tint)
        {
            tint.a = c.image.color.a;
            c.image.color = tint;
        }

        public List<VNCharacterState> GetStates()
        {
            var list = new List<VNCharacterState>();
            foreach (var n in order)
            {
                var c = characters[n];
                if (!c.hiding) list.Add(new VNCharacterState { name = c.name, expression = c.expression, x = c.x });
            }
            return list;
        }

        void ApplySprite(Character c)
        {
            Sprite sp = LoadSprite($"Characters/{c.name}_{c.expression}", false)
                        ?? LoadSprite($"Characters/{c.name}", false);
            if (sp == null)
                Debug.LogWarning($"[VN] 캐릭터 이미지를 찾을 수 없습니다: Assets/Resources/Characters/{c.name}_{c.expression}.png");

            c.image.sprite = sp;
            c.image.enabled = sp != null;
            float aspect = sp != null ? sp.rect.width / sp.rect.height : 0.5f;
            c.rect.sizeDelta = new Vector2(characterHeight * aspect, characterHeight);
        }

        static void Place(Character c, float x, float scaleX, float scaleY)
        {
            c.rect.anchorMin = c.rect.anchorMax = new Vector2(x, 0f);
            c.rect.pivot = new Vector2(0.5f, 0f);
            c.rect.anchoredPosition = Vector2.zero;
            c.rect.localScale = new Vector3(scaleX, scaleY, 1f);
        }

        IEnumerator Move(Character c, float toX, float toScaleX, float toScaleY, float d)
        {
            float fromX = c.rect.anchorMin.x;
            Vector3 fromScale = c.rect.localScale;
            for (float t = 0f; t < d; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / d);
                Place(c, Mathf.Lerp(fromX, toX, k), Mathf.Lerp(fromScale.x, toScaleX, k), Mathf.Lerp(fromScale.y, toScaleY, k));
                yield return null;
            }
            Place(c, toX, toScaleX, toScaleY);
            c.move = null;
        }

        IEnumerator HideRoutine(Character c, float d)
        {
            yield return Fade(c.image, c.image.color.a, 0f, d);
            c.fade = null;
            RemoveImmediate(c);
        }

        void RemoveImmediate(Character c)
        {
            if (c.fade != null) StopCoroutine(c.fade);
            if (c.move != null) StopCoroutine(c.move);
            if (characters.TryGetValue(c.name, out var cur) && cur == c)
            {
                characters.Remove(c.name);
                order.Remove(c.name);
            }
            if (c.image != null) Destroy(c.image.gameObject);
        }

        // ───────────────────────── CG ─────────────────────────

        public void ShowCG(string name, float d)
        {
            CurrentCG = name;
            var sp = LoadSprite("CG/" + name);
            if (cgRoutine != null) StopCoroutine(cgRoutine);
            cgImage.sprite = sp;
            cgImage.enabled = sp != null;
            cgRoutine = StartCoroutine(Fade(cgImage, d > 0f ? 0f : 1f, 1f, d));
        }

        public void HideCG(float d)
        {
            CurrentCG = null;
            if (cgRoutine != null) { StopCoroutine(cgRoutine); cgRoutine = null; }
            if (!cgImage.enabled) return;
            if (d <= 0f) { SetImage(cgImage, null, 0f); return; }
            cgRoutine = StartCoroutine(HideCGRoutine(d));
        }

        IEnumerator HideCGRoutine(float d)
        {
            yield return Fade(cgImage, cgImage.color.a, 0f, d);
            SetImage(cgImage, null, 0f);
            cgRoutine = null;
        }

        // ───────────────────────── 공통 ─────────────────────────

        public void ClearAll()
        {
            foreach (var n in order.ToArray()) RemoveImmediate(characters[n]);
            CurrentSpeaker = null;
            SetBackground(null, 0f);
            HideCG(0f);
        }

        static void SetImage(Image img, Sprite sp, float alpha)
        {
            img.sprite = sp;
            img.enabled = sp != null;
            SetAlpha(img, alpha);
        }

        static void SetAlpha(Graphic g, float a)
        {
            var c = g.color;
            c.a = a;
            g.color = c;
        }

        static IEnumerator Fade(Graphic g, float from, float to, float d)
        {
            for (float t = 0f; t < d; t += Time.deltaTime)
            {
                SetAlpha(g, Mathf.Lerp(from, to, t / d));
                yield return null;
            }
            SetAlpha(g, to);
        }
    }
}
