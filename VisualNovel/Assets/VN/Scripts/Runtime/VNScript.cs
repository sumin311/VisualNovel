using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VN
{
    public enum VNLineType { Dialogue, Command, Choice }

    public class VNChoiceOption
    {
        public string text;
        public string target; // 라벨, 파일, 또는 파일:라벨. null이면 다음 줄로 진행
    }

    public class VNLine
    {
        public VNLineType type;
        public int sourceLine;      // 원본 .txt 의 줄 번호 (오류 표시용)

        // Dialogue
        public string speaker;      // null = 나레이션
        public string expression;   // 이름(표정): 형식일 때
        public string text;

        // Command
        public string command;
        public string[] args;

        // Choice
        public List<VNChoiceOption> options;
    }

    /// <summary>
    /// Assets/Resources/Story/ 안의 .txt 스토리 파일을 읽어 실행 가능한 줄 목록으로 변환합니다.
    /// 문법은 프로젝트 루트의 "스토리_작성_가이드.md" 참고.
    /// </summary>
    public class VNScript
    {
        public const string Folder = "Story/";
        public const string DefaultExpression = "기본";

        public static readonly HashSet<string> KnownCommands = new HashSet<string>
        {
            "bg", "show", "hide", "cg", "bgm", "se", "wait", "jump", "set", "if", "end"
        };

        public string name;
        public readonly List<VNLine> lines = new List<VNLine>();
        public readonly Dictionary<string, int> labels = new Dictionary<string, int>();

        static readonly Dictionary<string, Color> nameColors = new Dictionary<string, Color>();

        // 이름: 대사  /  이름(표정): 대사   (이름에는 공백이 없어야 함)
        static readonly Regex dialogueRegex =
            new Regex(@"^([^\s:：()（）]+)\s*(?:[(（]([^)）]*)[)）])?\s*[:：]\s*(.*)$");

        public static VNScript Load(string name)
        {
            var asset = Resources.Load<TextAsset>(Folder + name);
            if (asset == null)
            {
                Debug.LogError($"[VN] 스토리 파일을 찾을 수 없습니다: Assets/Resources/{Folder}{name}.txt");
                return null;
            }
            return Parse(name, asset.text);
        }

        public static Color GetNameColor(string speaker)
        {
            if (speaker != null && nameColors.TryGetValue(speaker, out var c)) return c;
            return Color.white;
        }

        public static VNScript Parse(string name, string source)
        {
            var script = new VNScript { name = name };
            string[] raw = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            for (int i = 0; i < raw.Length; i++)
            {
                string line = raw[i].Trim();
                int lineNo = i + 1;
                if (IsSkippable(line)) continue;

                // # 라벨
                if (line.StartsWith("#"))
                {
                    string label = line.Substring(1).Trim();
                    if (script.labels.ContainsKey(label))
                        Warn(name, lineNo, $"라벨 '{label}'이(가) 중복되었습니다.");
                    else
                        script.labels[label] = script.lines.Count;
                    continue;
                }

                // @명령어
                if (line.StartsWith("@"))
                {
                    string[] parts = SplitArgs(line.Substring(1));
                    if (parts.Length == 0) continue;
                    string cmd = parts[0].ToLowerInvariant();
                    string[] args = new string[parts.Length - 1];
                    System.Array.Copy(parts, 1, args, 0, args.Length);

                    if (cmd == "color")
                    {
                        if (args.Length >= 2 && ColorUtility.TryParseHtmlString(args[1], out var col))
                            nameColors[args[0]] = col;
                        else
                            Warn(name, lineNo, "@color 사용법: @color 이름 #RRGGBB");
                        continue;
                    }

                    if (cmd == "choice")
                    {
                        var options = new List<VNChoiceOption>();
                        while (i + 1 < raw.Length)
                        {
                            string next = raw[i + 1].Trim();
                            if (IsSkippable(next)) { i++; continue; }
                            if (!next.StartsWith("-")) break;
                            i++;
                            string body = next.Substring(1).Trim();
                            var opt = new VNChoiceOption { text = body };
                            int arrow = body.LastIndexOf("->");
                            if (arrow >= 0)
                            {
                                opt.text = body.Substring(0, arrow).Trim();
                                opt.target = body.Substring(arrow + 2).Trim();
                            }
                            options.Add(opt);
                        }
                        if (options.Count == 0)
                        {
                            Warn(name, lineNo, "@choice 아래에 '- 선택지 -> 라벨' 형식의 줄이 없습니다.");
                            continue;
                        }
                        script.lines.Add(new VNLine { type = VNLineType.Choice, sourceLine = lineNo, options = options });
                        continue;
                    }

                    script.lines.Add(new VNLine { type = VNLineType.Command, sourceLine = lineNo, command = cmd, args = args });
                    continue;
                }

                // 대사 / 나레이션
                var vl = new VNLine { type = VNLineType.Dialogue, sourceLine = lineNo };
                if (line.StartsWith(">"))
                {
                    vl.text = line.Substring(1).Trim(); // > 로 시작하면 무조건 나레이션
                }
                else
                {
                    var m = dialogueRegex.Match(line);
                    if (m.Success)
                    {
                        vl.speaker = m.Groups[1].Value;
                        vl.expression = m.Groups[2].Success ? m.Groups[2].Value.Trim() : null;
                        vl.text = m.Groups[3].Value;
                    }
                    else vl.text = line;
                }
                vl.text = vl.text.Replace("\\n", "\n");
                script.lines.Add(vl);
            }
            return script;
        }

        static bool IsSkippable(string line) => line.Length == 0 || line.StartsWith("//");

        static void Warn(string file, int lineNo, string msg) =>
            Debug.LogWarning($"[VN] {file}.txt {lineNo}번째 줄: {msg}");

        static string[] SplitArgs(string s)
        {
            var list = new List<string>();
            var sb = new StringBuilder();
            bool quote = false;
            foreach (char ch in s)
            {
                if (ch == '"') { quote = !quote; continue; }
                if (!quote && char.IsWhiteSpace(ch))
                {
                    if (sb.Length > 0) { list.Add(sb.ToString()); sb.Clear(); }
                    continue;
                }
                sb.Append(ch);
            }
            if (sb.Length > 0) list.Add(sb.ToString());
            return list.ToArray();
        }
    }
}
