[b]AutoWorldStart-1.0.3[/b]

Автор / Author: HunterFenrir67

Автоматически загружает выбранный мир и персонажа при запуске Valheim.
Automatically loads your selected world and character when Valheim starts.

[img width=250,height=250]https://drive.google.com/thumbnail?id=1YpTh2yoNsoor7qq27KB5d-2fxH2c9n99&sz=w250[/img] [img width=250,height=250]https://drive.google.com/thumbnail?id=1BesiCpikxSbybdFZn8_D1NNnmCy0v5AF&sz=w250[/img]

[b]Русский[/b]

- Требуются Valheim и BepInExPack.
- Пропускает стартовую видеозаставку, если SkipIntroVideo включён.
- Скрывает лес и персонажа фона меню при ожидающей автозагрузке. Служебная сцена меню всё ещё загружается штатно. При ошибке или отмене фон возвращается.
- После готовности персонажа и HUD наблюдает ввод 1 секунду. Клавиша или движение мыши отменяет будущую паузу без открытия Esc. Если ввода нет, ставит одиночную игру на паузу без меню Esc. Последующий ввод снимает паузу.
- F7 включает/выключает настройку паузы после входа. F8 включает/выключает автозагрузку. Это переключатели настроек, а не кнопки открытия/закрытия меню. Настройки сохраняются.
- При отключении F7 во время автоматической паузы она снимается. Открытое вручную меню Esc не затрагивается.
- После выхода из мира автозагрузка повторно не запускается.

[b]English[/b]

Requires Valheim and BepInExPack. Skips the startup video when SkipIntroVideo is enabled. Hides menu scenery during pending autoload; the game's menu scene still initializes normally. Cancellation or errors restore the background.

After player/HUD readiness, observes input for 1 second. Any key or mouse movement cancels the pending pause without opening Esc. Without input, pauses single-player silently without Esc; later input resumes play. F7 toggles the after-entry pause setting; F8 toggles autoload. Settings persist. These are setting toggles, not manual menu buttons. Turning F7 off releases an owned automatic pause; manually opened menus are unaffected. Logging out does not repeat autoload.

[b]Configuration[/b]

[code]
[AutoWorldStart]
Enabled = true
WorldName = Your world name
PlayerName = Your character name
SkipIntroVideo = true
PauseAfterLoad = true
PauseInputWindowSeconds = 1

[Hotkeys]
TogglePauseAfterLoad = F7
ToggleAutoLoad = F8
[/code]

Если автозагрузка не запускается, проверьте настройки WorldName и PlayerName: укажите точные названия уже созданного мира и персонажа, как они отображаются в игре. Старые настройки Simple Auto Game Start читаются автоматически, если нет собственных значений AutoWorldStart.
If autoload does not start, check WorldName and PlayerName: enter the exact names of an existing world and character as displayed in the game. Legacy Simple Auto Game Start settings are read when AutoWorldStart has no values of its own.

[b]Configuration: PauseInputWindowSeconds[/b]

Время после готовности персонажа, в течение которого ввод отменяет будущую паузу. По умолчанию 1 секунда; диапазон 0,1–30 секунд. Изменение применяется при следующем входе.
Seconds after player/HUD readiness during which input cancels the pending pause. Default: 1; range: 0.1–30 seconds. Changes apply on the next entry.

[b]Поддержка / Support[/b]

Если мод оказался полезен, можно поддержать разработку добровольным донатом. Мод остаётся бесплатным.
If you find this mod useful, you can support development with a voluntary donation. The mod remains free.

[url=https://ko-fi.com/hunterfenrir][img width=125,height=31]https://storage.ko-fi.com/cdn/kofi5.png?v=6[/img][/url] [url=https://paypal.me/HunterFenrir67][img width=50,height=31]https://www.paypalobjects.com/webstatic/mktg/logo/pp_cc_mark_111x69.jpg[/img][/url]

[url=https://ko-fi.com/hunterfenrir]Ko-fi[/url] · [url=https://paypal.me/HunterFenrir67]PayPal[/url]

[b]Социальные сети / Social links[/b]

[url=https://discord.gg/wsaFn6y6][img]https://img.shields.io/badge/Discord-5865F2?style=for-the-badge&logo=discord&logoColor=white[/img][/url]
[url=https://www.youtube.com/@HunterFenrir67][img]https://img.shields.io/badge/YouTube-FF0000?style=for-the-badge&logo=youtube&logoColor=white[/img][/url]
[url=https://steamcommunity.com/profiles/76561199009317610/][img]https://img.shields.io/badge/Steam-171A21?style=for-the-badge&logo=steam&logoColor=white[/img][/url]

- [url=https://discord.gg/wsaFn6y6]Discord — сервер HunterFenrir67 / community server[/url]
- [url=https://www.youtube.com/@HunterFenrir67]YouTube — @HunterFenrir67[/url]
- [url=https://steamcommunity.com/profiles/76561199009317610/]Steam — профиль / profile[/url]
