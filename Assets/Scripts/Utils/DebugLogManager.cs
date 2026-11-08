using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Utils {
    public class DebugLogManager : BetterSingleton<DebugLogManager> {
        [Header("Refs")] 
        [SerializeField] GameObject ViewLogObject;
        [SerializeField] TMP_Text logText;
        [SerializeField] ScrollRect scrollRect;
        [Header("Settings")]
        [SerializeField] int maxLogLines = 100;

        int currentLogCount;

        void OnEnable() => Application.logMessageReceived += HandleLogMessageReceived;
        
        void OnDisable() => Application.logMessageReceived -= HandleLogMessageReceived;
        
        public void ToggleConsole() => ViewLogObject.SetActive(!ViewLogObject.activeSelf);
        
        void HandleLogMessageReceived(string condition, string stackTrace, LogType type) {
            var shouldDisplayToConsole = type is LogType.Log or LogType.Warning or LogType.Exception;
            if (shouldDisplayToConsole)
                condition += "\n" + stackTrace;

            var color = type switch {
                LogType.Error or LogType.Exception or LogType.Assert => "red",
                LogType.Warning => "yellow",
                _ => "white"
            };

            logText.text += $"<color={color}>[{type}] {condition}</color>\n\n";
            currentLogCount++;
            if (maxLogLines > currentLogCount) {
                logText.text = logText.text[(logText.text.IndexOf('\n') + 1)..];
                currentLogCount--;
            }

            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
