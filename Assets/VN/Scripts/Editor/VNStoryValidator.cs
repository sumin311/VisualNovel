using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VN.EditorTools
{
    /// <summary>
    /// VN > 스토리 파일 검사 : 모든 스토리 파일의 오타, 없는 라벨, 없는 이미지/사운드를 찾아 Console에 표시합니다.
    /// </summary>
    public static class VNStoryValidator
    {
        const string StoryRoot = "Assets/Resources/Story/";

        class Ctx
        {
            public Dictionary<string, VNScript> scripts;
            public Dictionary<string, TextAsset> assets;
            public int errors, warnings, todos;
        }

        [MenuItem("VN/스토리 파일 검사", priority = 1)]
        public static void ValidateMenu()
        {
            var ctx = Validate();
            EditorUtility.DisplayDialog("스토리 파일 검사",
                ctx.errors == 0 && ctx.warnings == 0
                    ? $"문제 없음! (파일 {ctx.scripts.Count}개)\n대사 작성 필요([작성]): {ctx.todos}곳"
                    : $"오류 {ctx.errors}개, 경고 {ctx.warnings}개\n대사 작성 필요([작성]): {ctx.todos}곳\n자세한 내용은 Console 창을 확인하세요.\n(항목을 클릭하면 해당 파일이 선택됩니다)",
                "확인");
        }

        public static (int errors, int warnings) ValidateQuiet()
        {
            var c = Validate();
            return (c.errors, c.warnings);
        }

        static Ctx Validate()
        {
            var ctx = new Ctx
            {
                scripts = new Dictionary<string, VNScript>(),
                assets = new Dictionary<string, TextAsset>(),
            };

            foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { StoryRoot.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".txt")) continue;
                string rel = path.Substring(StoryRoot.Length);
                rel = rel.Substring(0, rel.Length - 4);
                var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                ctx.assets[rel] = ta;
                ctx.scripts[rel] = VNScript.Parse(rel, ta.text);
            }

            foreach (var kv in ctx.scripts)
            {
                var s = kv.Value;
                foreach (var l in s.lines)
                {
                    switch (l.type)
                    {
                        case VNLineType.Dialogue:
                            if (l.text != null && l.text.Contains("[작성]")) ctx.todos++;
                            if (l.speaker != null && !string.IsNullOrEmpty(l.expression))
                                CheckCharacter(ctx, s, l, l.speaker, l.expression);
                            break;

                        case VNLineType.Choice:
                            foreach (var o in l.options)
                                if (!string.IsNullOrEmpty(o.target)) CheckTarget(ctx, s, l, o.target);
                            break;

                        case VNLineType.Command:
                            CheckCommand(ctx, s, l);
                            break;
                    }
                }
            }

            Debug.Log($"[VN 검사] 스토리 파일 {ctx.scripts.Count}개 검사 완료 — 오류 {ctx.errors}개, 경고 {ctx.warnings}개, 대사 작성 필요([작성]) {ctx.todos}곳");
            return ctx;
        }

        static void CheckCommand(Ctx ctx, VNScript s, VNLine l)
        {
            string[] a = l.args;
            string a0 = a.Length > 0 ? a[0] : null;

            if (!VNScript.KnownCommands.Contains(l.command))
            {
                Error(ctx, s, l, $"알 수 없는 명령어 @{l.command}");
                return;
            }

            switch (l.command)
            {
                case "bg":
                    if (a0 != null && a0 != "none" && a0 != "off") CheckSprite(ctx, s, l, "Backgrounds/" + a0);
                    break;
                case "show":
                    if (a0 == null) Error(ctx, s, l, "@show 뒤에 캐릭터 이름이 필요합니다.");
                    else CheckCharacter(ctx, s, l, a0, a.Length > 1 ? a[1] : VNScript.DefaultExpression);
                    break;
                case "cg":
                    if (a0 != null && a0 != "off") CheckSprite(ctx, s, l, "CG/" + a0);
                    break;
                case "bgm":
                    if (a0 != null && a0 != "off" && a0 != "stop" && Resources.Load<AudioClip>("Audio/BGM/" + a0) == null)
                        Warn(ctx, s, l, $"BGM 파일 없음: Assets/Resources/Audio/BGM/{a0}");
                    break;
                case "se":
                    if (a0 != null && Resources.Load<AudioClip>("Audio/SE/" + a0) == null)
                        Warn(ctx, s, l, $"효과음 파일 없음: Assets/Resources/Audio/SE/{a0}");
                    break;
                case "jump":
                    if (a0 == null) Error(ctx, s, l, "@jump 뒤에 대상이 필요합니다.");
                    else CheckTarget(ctx, s, l, a0);
                    break;
                case "if":
                {
                    string joined = string.Join(" ", a);
                    int arrow = joined.LastIndexOf("->");
                    if (arrow < 0) Error(ctx, s, l, "@if 에 '-> 라벨' 이 없습니다.");
                    else CheckTarget(ctx, s, l, joined.Substring(arrow + 2).Trim());
                    break;
                }
                case "ending":
                    if (a0 == null) Error(ctx, s, l, "@ending 뒤에 엔딩 이름이 필요합니다.");
                    break;
                case "set":
                    if (a.Length == 0) Error(ctx, s, l, "사용법: @set 변수 = 값");
                    break;
            }
        }

        static void CheckTarget(Ctx ctx, VNScript s, VNLine l, string target)
        {
            string file = null, label = target;
            int colon = target.IndexOf(':');
            if (colon >= 0)
            {
                file = target.Substring(0, colon).Trim();
                label = target.Substring(colon + 1).Trim();
            }
            else if (!s.labels.ContainsKey(target))
            {
                if (ctx.scripts.ContainsKey(target)) return; // 파일 이동
                Error(ctx, s, l, $"'{target}' 라벨(또는 파일)을 찾을 수 없습니다.");
                return;
            }

            var dest = s;
            if (!string.IsNullOrEmpty(file) && !ctx.scripts.TryGetValue(file, out dest))
            {
                Error(ctx, s, l, $"스토리 파일 '{file}.txt' 이(가) 없습니다.");
                return;
            }
            if (!string.IsNullOrEmpty(label) && !dest.labels.ContainsKey(label))
                Error(ctx, s, l, $"{dest.name}.txt 에 라벨 '# {label}' 이(가) 없습니다.");
        }

        static void CheckCharacter(Ctx ctx, VNScript s, VNLine l, string name, string expr)
        {
            if (Exists($"Characters/{name}_{expr}") || Exists($"Characters/{name}")) return;
            Warn(ctx, s, l, $"캐릭터 이미지 없음: Assets/Resources/Characters/{name}_{expr}.png");
        }

        static void CheckSprite(Ctx ctx, VNScript s, VNLine l, string path)
        {
            if (!Exists(path)) Warn(ctx, s, l, $"이미지 없음: Assets/Resources/{path}");
        }

        static bool Exists(string resPath) => Resources.Load<Object>(resPath) != null;

        static void Error(Ctx ctx, VNScript s, VNLine l, string msg)
        {
            ctx.errors++;
            Debug.LogError($"[VN 검사] {s.name}.txt {l.sourceLine}번째 줄: {msg}", ctx.assets[s.name]);
        }

        static void Warn(Ctx ctx, VNScript s, VNLine l, string msg)
        {
            ctx.warnings++;
            Debug.LogWarning($"[VN 검사] {s.name}.txt {l.sourceLine}번째 줄: {msg}", ctx.assets[s.name]);
        }

        [MenuItem("VN/세이브 폴더 열기", priority = 40)]
        static void OpenSaveFolder() => EditorUtility.RevealInFinder(Path.Combine(Application.persistentDataPath, "."));

        [MenuItem("VN/세이브 데이터 전부 삭제", priority = 41)]
        static void DeleteSaves()
        {
            if (!EditorUtility.DisplayDialog("세이브 삭제", "모든 세이브 파일을 삭제할까요?", "삭제", "취소")) return;
            for (int i = 0; i <= VNSaveSystem.SlotCount; i++) VNSaveSystem.Delete(i);
            Debug.Log("[VN] 세이브 데이터를 삭제했습니다.");
        }

        [MenuItem("VN/갤러리 전부 해금 (테스트용)", priority = 42)]
        static void UnlockAll() => SetAllUnlocks(true);

        [MenuItem("VN/갤러리 해금 초기화", priority = 43)]
        static void ResetUnlocks()
        {
            if (!EditorUtility.DisplayDialog("갤러리 초기화", "해금된 CG와 엔딩 기록을 모두 지울까요?", "초기화", "취소")) return;
            SetAllUnlocks(false);
        }

        static void SetAllUnlocks(bool unlocked)
        {
            foreach (var t in Resources.LoadAll<Texture2D>("CG"))
                if (unlocked) VNUnlocks.UnlockCG(t.name); else PlayerPrefs.DeleteKey("vn.cg." + t.name);
            foreach (var e in VNGallery.AllEndings())
                if (unlocked) VNUnlocks.UnlockEnding(e); else PlayerPrefs.DeleteKey("vn.ending." + e);
            PlayerPrefs.Save();
            Debug.Log(unlocked ? "[VN] 갤러리를 전부 해금했습니다." : "[VN] 갤러리 해금 기록을 지웠습니다.");
        }
    }
}
