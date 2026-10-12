using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VN
{
    [Serializable]
    public class VNVariable
    {
        public string key;
        public int value;
    }

    [Serializable]
    public class VNCharacterState
    {
        public string name;
        public string expression;
        public float x;
    }

    [Serializable]
    public class VNSaveData
    {
        public string scriptName;
        public int lineIndex;
        public string background;
        public string cg;
        public string bgm;
        public List<VNCharacterState> characters = new List<VNCharacterState>();
        public string speaker;      // 오른쪽에 크게 서 있던 화자
        public List<VNVariable> variables = new List<VNVariable>();
        public string dayLabel;
        public bool hudVisible;
        public string gauges;       // 표시 중인 호감도 게이지 (공백 구분)
        public string modeName;     // 게임 모드 이름 (슬롯 표시용)
        public string startScript;  // 게임 모드의 첫 스토리 파일
        public string savedAt;
        public string preview;
    }

    /// <summary>세이브 파일은 Application.persistentDataPath/save_XX.json 에 저장됩니다.</summary>
    public static class VNSaveSystem
    {
        public const int QuickSlot = 0;
        public const int SlotCount = 8;

        static string PathOf(int slot) => Path.Combine(Application.persistentDataPath, $"save_{slot:00}.json");

        // 웹 버전은 브라우저 저장소(PlayerPrefs)에 저장 (파일 저장은 브라우저에서 바로 반영되지 않음)
        static bool UsePrefs => Application.platform == RuntimePlatform.WebGLPlayer;
        static string KeyOf(int slot) => $"vn.save.{slot:00}";

        public static bool Exists(int slot) => UsePrefs ? PlayerPrefs.HasKey(KeyOf(slot)) : File.Exists(PathOf(slot));

        public static void Save(int slot, VNSaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, !UsePrefs);
                if (UsePrefs) { PlayerPrefs.SetString(KeyOf(slot), json); PlayerPrefs.Save(); }
                else File.WriteAllText(PathOf(slot), json);
            }
            catch (Exception e) { Debug.LogError($"[VN] 저장 실패 (슬롯 {slot}): {e.Message}"); }
        }

        public static VNSaveData Load(int slot)
        {
            if (!Exists(slot)) return null;
            try
            {
                string json = UsePrefs ? PlayerPrefs.GetString(KeyOf(slot)) : File.ReadAllText(PathOf(slot));
                return JsonUtility.FromJson<VNSaveData>(json);
            }
            catch (Exception e) { Debug.LogError($"[VN] 불러오기 실패 (슬롯 {slot}): {e.Message}"); return null; }
        }

        public static void Delete(int slot)
        {
            if (!Exists(slot)) return;
            if (UsePrefs) { PlayerPrefs.DeleteKey(KeyOf(slot)); PlayerPrefs.Save(); }
            else File.Delete(PathOf(slot));
        }
    }

    /// <summary>갤러리 해금 기록 (PlayerPrefs에 저장, 세이브와 별개라 새 게임을 해도 유지)</summary>
    public static class VNUnlocks
    {
        public static bool HasCG(string name) => PlayerPrefs.GetInt("vn.cg." + name, 0) == 1;
        public static bool HasEnding(string name) => PlayerPrefs.GetInt("vn.ending." + name, 0) == 1;

        public static void UnlockCG(string name)
        {
            if (HasCG(name)) return;
            PlayerPrefs.SetInt("vn.cg." + name, 1);
            PlayerPrefs.Save();
        }

        public static void UnlockEnding(string name)
        {
            if (HasEnding(name)) return;
            PlayerPrefs.SetInt("vn.ending." + name, 1);
            PlayerPrefs.Save();
        }
    }

    /// <summary>환경설정 값 (PlayerPrefs에 저장)</summary>
    public static class VNSettings
    {
        public static float TextSpeed
        {
            get => PlayerPrefs.GetFloat("vn.textSpeed", 40f);
            set => PlayerPrefs.SetFloat("vn.textSpeed", value);
        }

        public static float AutoDelay
        {
            get => PlayerPrefs.GetFloat("vn.autoDelay", 1.5f);
            set => PlayerPrefs.SetFloat("vn.autoDelay", value);
        }

        public static float BgmVolume
        {
            get => PlayerPrefs.GetFloat("vn.bgmVolume", 0.7f);
            set => PlayerPrefs.SetFloat("vn.bgmVolume", value);
        }

        public static float SeVolume
        {
            get => PlayerPrefs.GetFloat("vn.seVolume", 0.8f);
            set => PlayerPrefs.SetFloat("vn.seVolume", value);
        }

        public static void Save() => PlayerPrefs.Save();
    }
}
