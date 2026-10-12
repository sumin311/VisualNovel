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

        public string CurrentBackground { get; private set; }
        public string CurrentCG { get; private set; }

        class Character
        {
            public string name;
            public string expression;
            public float x;
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
                    if (c.move != null) StopCoroutine(c.move);
                    c.move = StartCoroutine(Move(c, x.Value, fade > 0f ? 0.3f : 0f));
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
            PlaceAt(c, c.x);
            SetAlpha(c.image, 0f);
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
            if (fade <= 0f) { RemoveImmediate(c); return; }
            c.fade = StartCoroutine(HideRoutine(c, fade));
        }

        public void HideAll(float fade)
        {
            foreach (var n in order.ToArray()) HideCharacter(n, fade);
        }

        /// <summary>말하는 캐릭터만 밝게, 나머지는 어둡게</summary>
        public void Focus(string speaker)
        {
            bool speakerOnStage = speaker != null && characters.ContainsKey(speaker);
            foreach (var c in characters.Values)
            {
                Color tint = (!dimInactiveCharacters || !speakerOnStage || c.name == speaker) ? Color.white : inactiveTint;
                tint.a = c.image.color.a;
                c.image.color = tint;
            }
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

        static void PlaceAt(Character c, float x)
        {
            c.rect.anchorMin = c.rect.anchorMax = new Vector2(x, 0f);
            c.rect.pivot = new Vector2(0.5f, 0f);
            c.rect.anchoredPosition = Vector2.zero;
        }

        IEnumerator Move(Character c, float to, float d)
        {
            float from = c.rect.anchorMin.x;
            for (float t = 0f; t < d; t += Time.deltaTime)
            {
                PlaceAt(c, Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / d)));
                yield return null;
            }
            PlaceAt(c, to);
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
