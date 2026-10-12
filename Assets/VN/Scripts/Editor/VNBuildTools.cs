using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VN.EditorTools
{
    /// <summary>
    /// VN > 웹 버전 빌드 : 브라우저(아이폰 사파리 포함)에서 플레이하는 버전을 Builds/WebGL 에 만듭니다.
    /// 만들어진 폴더를 통째로 웹에 올리면 링크로 플레이할 수 있어요.
    /// </summary>
    public static class VNBuildTools
    {
        public const string WebGLOutput = "Builds/WebGL";

        [MenuItem("VN/웹 버전 빌드", priority = 20)]
        static void BuildWebGLMenu()
        {
            if (BuildWebGL())
                EditorUtility.RevealInFinder(Path.Combine(WebGLOutput, "index.html"));
        }

        /// <summary>배치 모드용: Unity.exe -batchmode -buildTarget WebGL -executeMethod VN.EditorTools.VNBuildTools.BuildWebGLBatch</summary>
        public static void BuildWebGLBatch()
        {
            bool ok = BuildWebGL();
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool BuildWebGL()
        {
            // 프로젝트가 OneDrive 폴더 안에 있으면 빌드 도구가 지난번 임시 폴더를 못 지워서 실패하는 일이 있어
            // 미리 지워 둠 (다음 빌드가 조금 더 오래 걸릴 뿐 문제 없음)
            const string beeCache = "Library/Bee/artifacts/WebGL";
            try { ForceDelete(beeCache); }
            catch (System.Exception e) { Debug.LogWarning("[VN] 빌드 임시 폴더 정리 실패 (무시하고 계속): " + e.Message); }

            PlayerSettings.WebGL.template = "PROJECT:VN";
            // 어떤 웹 호스팅에서도 열리도록: 압축은 하되, 서버가 압축 설정을 못 해 줘도 브라우저가 풀게 함
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.runInBackground = false;

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { VNSceneBuilder.ScenePath },
                locationPathName = WebGLOutput,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });
            bool ok = report.summary.result == BuildResult.Succeeded;
            if (ok) CopyToDocs();
            Debug.Log(ok
                ? $"[VN] 웹 버전 빌드 완료: {WebGLOutput} ({report.summary.totalSize / (1024 * 1024)} MB)"
                : $"[VN] 웹 버전 빌드 실패: {report.summary.result}");
            return ok;
        }

        /// <summary>
        /// 빌드 결과를 저장소의 docs/ 폴더로 복사 (GitHub Pages 가 이 폴더를 웹사이트로 보여줌).
        /// GitHub Desktop 으로 커밋·푸시하면 몇 분 뒤 링크에 반영돼요.
        /// </summary>
        public const string DocsFolder = "docs";

        static void CopyToDocs()
        {
            ForceDelete(DocsFolder);
            CopyDir(WebGLOutput, DocsFolder);
            File.WriteAllText(Path.Combine(DocsFolder, ".nojekyll"), ""); // GitHub Pages 가 파일을 가공하지 않게
            Debug.Log($"[VN] 웹 버전을 {DocsFolder}/ 에 복사했어요. 커밋·푸시하면 GitHub Pages 에 올라가요.");
        }

        static void CopyDir(string from, string to)
        {
            Directory.CreateDirectory(to);
            foreach (var f in Directory.GetFiles(from)) File.Copy(f, Path.Combine(to, Path.GetFileName(f)), true);
            foreach (var d in Directory.GetDirectories(from)) CopyDir(d, Path.Combine(to, Path.GetFileName(d)));
        }

        /// <summary>읽기 전용 표시(OneDrive가 붙이는 경우가 있음)를 풀고 폴더를 통째로 지움</summary>
        static void ForceDelete(string dir)
        {
            if (!Directory.Exists(dir)) return;
            var root = new DirectoryInfo(dir);
            root.Attributes = FileAttributes.Normal;
            foreach (var info in root.GetFileSystemInfos("*", SearchOption.AllDirectories))
                info.Attributes = FileAttributes.Normal;
            root.Delete(true);
        }
    }
}
