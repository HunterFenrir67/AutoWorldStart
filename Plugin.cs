// AutoWorldStart maintained by HunterFenrir67, team Hunter9732ru. Based on Simple Auto Game Start by ABearCodes. MIT.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using HarmonyLib;

namespace Hunter9732ru.AutoWorldStart
{
    [BepInPlugin("hunter9732ru.autoworldstart", "AutoWorldStart-1.0.3", "1.0.3")]
    [BepInIncompatibility("com.github.abearcodes.valheim.simpleautogamestart")]
    public sealed class Plugin : BaseUnityPlugin
    {
        static Plugin instance;
        Harmony harmony;
        ConfigEntry<bool> skipIntroVideo, pauseAfterLoad;
        ConfigEntry<float> pauseInputWindow;
        static readonly MethodInfo CanPauseMethod = typeof(Game).GetMethod("CanPause", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        bool pausePending, autoPauseOwned;
        DateTime resumeInputBaseline, pendingInputBaseline;
        GameObject hiddenWorldPanel;
        Camera hiddenMenuCamera;
        int originalCullingMask;
        CameraClearFlags originalClearFlags;
        Color originalBackground;
        FejdStartup hiddenStartup;
        float restoreMenuAt;
        static readonly FieldInfo StartingWorld = typeof(FejdStartup).GetField("m_startingWorld", Flags);
        float pauseDeadline, nextPauseCheck, pauseReadyAt = -1f;
        ConfigEntry<bool> autoEnabled;
        ConfigEntry<string> worldName, playerName;
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly FieldInfo Profiles = typeof(FejdStartup).GetField("m_profiles", Flags);
        static readonly FieldInfo ProfileIndex = typeof(FejdStartup).GetField("m_profileIndex", Flags);
        static readonly FieldInfo SelectedWorld = typeof(FejdStartup).GetField("m_world", Flags);
        static readonly FieldInfo QueuedJoin = typeof(FejdStartup).GetField("m_queuedJoinServer", Flags);
        bool finished, worldAttempted;
        ConfigEntry<KeyboardShortcut> togglePauseKey, toggleAutoLoadKey;
        float deadline;

        void Awake()
        {
            instance = this;
            skipIntroVideo = Config.Bind("AutoWorldStart", "SkipIntroVideo", true,
                "Skip the startup video before the main menu. Does not skip loading or in-world cinematics. / Пропускать видео перед главным меню.");
            try
            {
                // Use the game's existing SkipIntroCinematic branch, including its
                // FadeIn/menu initialization. No persistent game preference is changed.
                var getter = AccessTools.Method(typeof(PlatformPrefs), "GetBool", new[] { typeof(string), typeof(bool) });
                if (getter == null) throw new MissingMethodException("PlatformPrefs.GetBool(string, bool)");
                harmony = new Harmony("hunter9732ru.autoworldstart.intro");
                harmony.Patch(getter, postfix: new HarmonyMethod(typeof(Plugin), nameof(IntroPreferencePostfix)));
            }
            catch (Exception e) { Logger.LogWarning("Intro skip unavailable; automatic world loading remains enabled: " + e.Message); }
            pauseAfterLoad = Config.Bind("AutoWorldStart", "PauseAfterLoad", true,
                "After entry, silently pause only if no input occurs during PauseInputWindowSeconds. / После входа пауза без Esc, если за время PauseInputWindowSeconds не было ввода.");
            pauseInputWindow = Config.Bind("AutoWorldStart", "PauseInputWindowSeconds", 1f,
                new ConfigDescription("Seconds after player/HUD readiness during which input cancels the pending pause. Applied on the next entry. / Сколько секунд после готовности персонажа ввод отменяет будущую паузу. По умолчанию 1; изменение действует при следующем входе.",
                    new AcceptableValueRange<float>(0.1f, 30f)));
            // Only read the legacy config. Existing settings in this mod's own
            // config take priority; the old file is never rewritten.
            var legacyPath = Path.Combine(Paths.ConfigPath, "com.github.abearcodes.valheim.simpleautogamestart.cfg");
            bool initialEnabled = true;
            string initialWorld = "", initialPlayer = "";
            if (File.Exists(legacyPath))
            {
                try
                {
                    var legacy = new ConfigFile(legacyPath, false);
                    legacy.SaveOnConfigSet = false;
                    initialEnabled = legacy.Bind("SimpleAutoGameStart", "Enabled", true).Value;
                    initialWorld = legacy.Bind("SimpleAutoGameStart", "WorldName", "").Value;
                    initialPlayer = legacy.Bind("SimpleAutoGameStart", "PlayerName", "").Value;
                }
                catch (Exception e) { Logger.LogWarning("Cannot read old settings: " + e.Message); }
            }
            autoEnabled = Config.Bind("AutoWorldStart", "Enabled", initialEnabled, "Automatically load the named local world once when Valheim starts.");
            worldName = Config.Bind("AutoWorldStart", "WorldName", initialWorld, "Existing world display name, exactly as shown in the game. Never creates a world.");
            playerName = Config.Bind("AutoWorldStart", "PlayerName", initialPlayer, "Existing character name, exactly as shown in the game.");
            togglePauseKey = Config.Bind("Hotkeys", "TogglePauseAfterLoad", new KeyboardShortcut(KeyCode.F7), "Toggle automatic pause AFTER loading; does not open or close Esc. / Переключатель паузы после загрузки.");
            toggleAutoLoadKey = Config.Bind("Hotkeys", "ToggleAutoLoad", new KeyboardShortcut(KeyCode.F8), "Toggle automatic world loading. Saved for next launch; in the initial menu can resume a cancelled attempt. / Переключатель автозагрузки мира.");
            ScheduleAutoLoad();
        }
        void ScheduleAutoLoad()
        {
            CancelInvoke(nameof(TryStart));
            if (!autoEnabled.Value || worldAttempted || Player.m_localPlayer != null) { finished = true; return; }
            finished = false;
            if (string.IsNullOrWhiteSpace(worldName.Value) || string.IsNullOrWhiteSpace(playerName.Value))
            { Logger.LogWarning("Set WorldName and PlayerName in the config. Automatic start skipped."); finished = true; return; }
            deadline = Time.realtimeSinceStartup + 180f;
            InvokeRepeating(nameof(TryStart), 0.1f, 0.5f);
        }
        void Finish() { finished = true; CancelInvoke(nameof(TryStart)); if (!worldAttempted) RestoreWorldPanel(); }
        static bool Same(string a, string b)
        { return string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.OrdinalIgnoreCase); }
        void TryStart()
        {
            if (finished) return;
            if (!autoEnabled.Value) { Finish(); return; }
            try { StartFromMenu(); }
            catch (Exception e)
            {
                Finish();
                pausePending = false;
                RestoreWorldPanel();
                Logger.LogError("Automatic start stopped; use the normal menu. " + e);
            }
        }
        void StartFromMenu()
        {
            if (Time.realtimeSinceStartup > deadline)
            { Finish(); Logger.LogWarning("Main menu was not ready within 180 seconds. Automatic start skipped."); return; }
            var startup = FejdStartup.instance;
            if (startup == null) return;
            if (startup.m_loading != null && startup.m_loading.activeInHierarchy) return;
            // Wait for FejdStartup.Start and the intro to finish. Do not bypass login,
            // save recovery or world validation dialogs.
            if (startup.m_menuList == null || !startup.m_menuList.activeInHierarchy) return;
            if (Profiles == null || ProfileIndex == null || SelectedWorld == null)
                throw new MissingFieldException("FejdStartup selection fields changed.");
            var profiles = Profiles.GetValue(startup) as List<PlayerProfile>;
            if (profiles == null) return;
            if (QueuedJoin != null)
            {
                object queued = QueuedJoin.GetValue(startup);
                var valid = queued == null ? null : queued.GetType().GetProperty("IsValid");
                if (valid != null && (bool)valid.GetValue(queued, null))
                { Finish(); Logger.LogInfo("A server connection is queued; local automatic start skipped."); return; }
            }
            // Modern Valheim moved these APIs from World/PlayerProfile to SaveSystem.
            var worlds = SaveSystem.GetWorldList();
            if (worlds == null) return;
            // Count matches without LINQ lists, anonymous profile objects or closures.
            int worldCount = 0, playerCount = 0, selectedIndex = -1;
            World chosenWorld = null;
            string targetWorld = worldName.Value.Trim(), targetPlayer = playerName.Value.Trim();
            for (int i = 0; i < worlds.Count; i++)
            {
                World candidate = worlds[i];
                if (!Same(candidate.m_worldName, targetWorld) && !Same(candidate.m_name, targetWorld)) continue;
                worldCount++; chosenWorld = candidate;
            }
            for (int i = 0; i < profiles.Count; i++)
            {
                if (!Same(profiles[i].GetName(), targetPlayer)) continue;
                playerCount++; selectedIndex = i;
            }
            if (worldCount != 1 || playerCount != 1)
            {
                Finish();
                Logger.LogError("Automatic start needs one matching world and character. World matches: " + worldCount + "; character matches: " + playerCount + ".");
                Logger.LogInfo("Available worlds: " + string.Join(" | ", worlds.Select(w => w.m_worldName).ToArray()));
                Logger.LogInfo("Available characters: " + string.Join(" | ", profiles.Select(p => p.GetName()).ToArray()));
                return;
            }
            // One attempt only: logging out must return to the menu without a loop.
            worldAttempted = true;
            Finish();
            ProfileIndex.SetValue(startup, selectedIndex);
            // Use the game's character selection path to set Game's profile, then
            // the game's normal world-start path for save and compatibility checks.
            // Hide rendering before character/world panels are opened, not after
            // OnWorldStart. Scene initialization remains the game's responsibility.
            HideMenuBackground(startup);
            startup.OnCharacterStart();
            SelectedWorld.SetValue(startup, chosenWorld);
            if (startup.m_openServerToggle != null) startup.m_openServerToggle.isOn = false;
            if (startup.m_publicServerToggle != null) startup.m_publicServerToggle.isOn = false;
            Logger.LogInfo("Starting world '" + chosenWorld.m_worldName + "' with character '" + profiles[selectedIndex].GetName() + "'.");
            startup.OnWorldStart();
            // Hide only after the game accepted the world start. Keep its ordinary
            // save/compatibility prompts available if it declined to start.
            if (StartingWorld != null && (bool)StartingWorld.GetValue(startup) && startup.m_startGamePanel != null)
            {
                hiddenWorldPanel = startup.m_startGamePanel;
                hiddenStartup = startup;
                restoreMenuAt = Time.realtimeSinceStartup + 20f;
                hiddenWorldPanel.SetActive(false);
            }
            else { RestoreWorldPanel(); pausePending = false; return; }
            nextPauseCheck = 0f;
            pausePending = pauseAfterLoad.Value;
            pauseDeadline = Time.realtimeSinceStartup + 600f;
            pauseReadyAt = -1f;
        }
        void Update()
        {
            if ((hiddenWorldPanel != null || hiddenMenuCamera != null) &&
                (hiddenStartup == null || Time.realtimeSinceStartup >= restoreMenuAt))
                RestoreWorldPanel();
            // Hide menu scenery as soon as its camera exists, before save selection.
            // Only pending, configured autoload may own this temporary change.
            if (!finished && !worldAttempted && autoEnabled.Value &&
                hiddenMenuCamera == null && Player.m_localPlayer == null && FejdStartup.instance != null)
                HideMenuBackground(FejdStartup.instance);
            if (autoPauseOwned) UpdateAutoPause();
            // Inspect UI focus only on an actual shortcut press, not every frame.
            bool pausePressed = togglePauseKey.Value.IsDown();
            bool autoPressed = toggleAutoLoadKey.Value.IsDown();
            if ((pausePressed || autoPressed) && !TypingInField())
            {
                if (pausePressed)
                {
                    pauseAfterLoad.Value = !pauseAfterLoad.Value;
                    SaveSettings();
                    Notify("Pause after load / Пауза после загрузки: " + (pauseAfterLoad.Value ? "ON / ВКЛ" : "OFF / ВЫКЛ"));
                }
                if (autoPressed)
                {
                    autoEnabled.Value = !autoEnabled.Value;
                    SaveSettings();
                    if (!autoEnabled.Value) Finish();
                    else if (!worldAttempted && FejdStartup.instance != null && Player.m_localPlayer == null) ScheduleAutoLoad();
                    Notify("Automatic world load / Автозагрузка: " + (autoEnabled.Value ? "ON / ВКЛ" : "OFF / ВЫКЛ"));
                }
            }
            if (!pausePending) return;
            if (!pauseAfterLoad.Value || Time.realtimeSinceStartup > pauseDeadline)
            { pausePending = false; return; }
            float now = Time.realtimeSinceStartup;
            // Observe every frame only during this short window. A single key tap
            // must cancel the future pause even between readiness checks.
            if (pauseReadyAt >= 0f)
            {
                if (HasInputSince(pendingInputBaseline)) { pausePending = false; return; }
                // No repeated player/HUD queries while the input window runs.
                // Revalidate readiness once when the window ends.
                if (now < pauseReadyAt) return;
            }
            if (now < nextPauseCheck) return;
            nextPauseCheck = now + 0.1f;
            try
            {
                var player = Player.m_localPlayer;
                var menu = global::Menu.instance;
                if (player == null || menu == null || Hud.instance == null ||
                    !Hud.instance.IsVisible() || player.IsTeleporting() || player.InCutscene() || player.IsDead())
                { pauseReadyAt = -1f; return; }
                // Start a configured inactivity window only after actual entry.
                if (pauseReadyAt < 0f)
                {
                    float seconds = pauseInputWindow.Value;
                    if (float.IsNaN(seconds) || float.IsInfinity(seconds)) seconds = 1f;
                    seconds = Math.Max(0.1f, Math.Min(30f, seconds));
                    pauseReadyAt = now + seconds;
                    pendingInputBaseline = ZInput.instance == null ? DateTime.MinValue : ZInput.instance.GetLastInputTimer();
                    if (HasInputSince(pendingInputBaseline)) pausePending = false;
                    return;
                }
                if (Time.realtimeSinceStartup < pauseReadyAt) return;
                pausePending = false;
                if (!global::Menu.IsVisible() && CanPauseMethod != null && (bool)CanPauseMethod.Invoke(null, null) && !Game.IsPaused())
                {
                    Game.Pause();
                    autoPauseOwned = true;
                    resumeInputBaseline = ZInput.instance == null ? DateTime.MinValue : ZInput.instance.GetLastInputTimer();
                }
            }
            catch (Exception e)
            {
                pausePending = false;
                Logger.LogWarning("Cannot pause after loading: " + e.Message);
            }
        }
        void HideMenuBackground(FejdStartup startup)
        {
            if (hiddenMenuCamera != null) return;
            // Change only the pending autoload's menu camera.
            // Keep the UI layer visible for loading text and native dialogs.
            var camera = startup.m_mainCamera == null ? null : startup.m_mainCamera.GetComponent<Camera>();
            if (camera == null) return;
            hiddenMenuCamera = camera;
            hiddenStartup = startup;
            restoreMenuAt = deadline;
            originalCullingMask = camera.cullingMask;
            originalClearFlags = camera.clearFlags;
            originalBackground = camera.backgroundColor;
            camera.backgroundColor = Color.black;
            camera.clearFlags = CameraClearFlags.SolidColor;
            int uiLayer = LayerMask.NameToLayer("UI");
            camera.cullingMask = uiLayer < 0 ? originalCullingMask : originalCullingMask & (1 << uiLayer);
        }
        void RestoreWorldPanel()
        {
            if (hiddenMenuCamera != null)
            {
                hiddenMenuCamera.cullingMask = originalCullingMask;
                hiddenMenuCamera.clearFlags = originalClearFlags;
                hiddenMenuCamera.backgroundColor = originalBackground;
            }
            hiddenMenuCamera = null;
            if (hiddenWorldPanel != null) hiddenWorldPanel.SetActive(true);
            hiddenWorldPanel = null;
            hiddenStartup = null;
        }
        void UpdateAutoPause()
        {
            // A manually opened Esc menu takes ownership of pause.
            // Never dismiss that menu or release its pause automatically.
            if (global::Menu.IsVisible()) { autoPauseOwned = false; return; }
            if (Player.m_localPlayer == null || !pauseAfterLoad.Value || !Game.IsPaused())
            { ReleaseAutoPause(); return; }
            if (HasInputSince(resumeInputBaseline)) ReleaseAutoPause();
        }
        static bool HasInputSince(DateTime baseline)
        {
            var input = ZInput.instance;
            return UnityEngine.Input.anyKeyDown ||
                (input != null && input.GetLastInputTimer() > baseline) ||
                ZInput.GetMouseDelta().sqrMagnitude > 0f || ZInput.GetMouseScrollWheel() != 0f;
        }
        void ReleaseAutoPause()
        {
            if (!autoPauseOwned) return;
            autoPauseOwned = false;
            if (!global::Menu.IsVisible()) Game.Unpause();
        }
        static bool TypingInField()
        {
            var events = UnityEngine.EventSystems.EventSystem.current;
            var selected = events == null ? null : events.currentSelectedGameObject;
            return selected != null && (selected.GetComponent<UnityEngine.UI.InputField>() != null || selected.GetComponent<TMPro.TMP_InputField>() != null);
        }
        void SaveSettings()
        {
            // ConfigEntry.Value already saves when this standard BepInEx option is on.
            if (!Config.SaveOnConfigSet) Config.Save();
        }
        void Notify(string text)
        {
            Logger.LogInfo(text);
            if (MessageHud.instance != null)
                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, text);
        }
        static void IntroPreferencePostfix(string __0, ref bool __result)
        {
            if (__0 == "SkipIntroCinematic" && instance != null && instance.skipIntroVideo != null && instance.skipIntroVideo.Value)
                __result = true;
        }
        void OnDestroy()
        {
            CancelInvoke(nameof(TryStart));
            ReleaseAutoPause();
            RestoreWorldPanel();
            if (harmony != null) harmony.UnpatchSelf();
            if (instance == this) instance = null;
        }
    }
}
