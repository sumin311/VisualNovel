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
        public List<VNVariable> variables = new List<VNVariable>();
        public string savedAt;
        public string preview;
    }

    /// <summary>세이브 파일은 Application.persistentDataPath/save_XX.json 에 저장됩니다.</summary>
    public static class VNSaveSystem
    {
        public const int QuickSlot = 0;
        public const int SlotCount = 8;

        static string PathOf(int slot) => Path.Combine(Application.persistentDataPath, $"save_{slot:00}.json");

        public static bool Exists(int slot) => File.Exists(PathOf(slot));

        public static void Save(int slot, VNSaveData data)
        {
            try { File.WriteAllText(PathOf(slot), JsonUtility.ToJson(data, true)); }
            catch (Exception e) { Debug.LogError($"[VN] 저장 실패 (슬롯 {slot}): {e.Message}"); }
        }

        public static VNSaveData Load(int slot)
        {
            if (!Exists(slot)) return null;
            try { return JsonUtility.FromJson<VNSaveData>(File.ReadAllText(PathOf(slot))); }
            catch (Exception e) { Debug.LogError($"[VN] 불러오기 실패 (슬롯 {slot}): {e.Message}"); return null; }
        }

        public static void Delete(int slot)
        {
            if (Exists(slot)) File.Delete(PathOf(slot));
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
