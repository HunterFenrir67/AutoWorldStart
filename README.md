# AutoWorldStart

Automatically loads your selected world and character when Valheim starts.
Автоматически загружает выбранный мир и персонажа при запуске Valheim.

Maintainer / Поддержка: HunterFenrir67 (Thunderstore team: Hunter9732ru).
Based on Simple Auto Game Start by ABearCodes. Licensed under MIT; see LICENSE.

## Исходники / Source

Исходная версия проекта: 1.0.3. Plugin.cs восстановлен из сохранённого исходника 1.1.6; в последующих выпусках этой ветки менялись метаданные версии и оформление пакета. Проект сборки добавлен отдельно. Новая компиляция и проверка в игре для этого архива не выполнялись.

Source release: 1.0.3. Plugin.cs was recovered from the retained 1.1.6 source; later releases in this branch changed version metadata and package presentation. The build project was added separately. This source archive has not been newly compiled or tested in-game.

## Возможности / Features

- Автозагрузка существующего локального мира и персонажа по именам, один раз за запуск игры.
- Пропуск вступительного видео и скрытие фона меню во время ожидающей автозагрузки.
- Пауза в одиночной игре без открытия Esc, если после входа не было ввода в течение заданного времени (по умолчанию 1 секунда). Последующий ввод снимает автоматическую паузу.
- F7 переключает настройку паузы после входа, F8 — автозагрузку.
- Работает на клиенте; серверу мод не нужен.

Loads an existing local world and character once per game launch. Supports intro skipping, temporary menu-background hiding, and an input-sensitive single-player pause after loading. F7 toggles the after-entry pause setting; F8 toggles autoload. Client only.

## Настройка / Configuration

`BepInEx/config/hunter9732ru.autoworldstart.cfg`

Укажите точные названия уже созданного мира и персонажа, как в игре.
Enter the exact names of an existing world and character as displayed in-game.

```ini
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
```

Не устанавливайте одновременно с Simple Auto Game Start.
Do not install alongside Simple Auto Game Start.

## Сборка / Build

Нужны .NET SDK, установленный Valheim и BepInEx 5. DLL игры и BepInEx в репозиторий не входят. Используйте свои локальные файлы. Если BepInEx находится в профиле Gale/r2modman, передайте его путь отдельно.

Requires a .NET SDK, a local Valheim installation, and BepInEx 5. Game and BepInEx assemblies are not included. Set BepInExPath separately when using a mod-manager profile.

PowerShell / Windows:

```powershell
dotnet build AutoWorldStart.csproj -c Release -p:ValheimPath="F:/Steam/steamapps/common/Valheim"
```

С отдельной папкой BepInEx / With a separate BepInEx directory:

```powershell
dotnet build AutoWorldStart.csproj -c Release -p:ValheimPath="F:/Steam/steamapps/common/Valheim" -p:BepInExPath="D:/YourProfile/BepInEx"
```

Результат / Output: `bin/Release/net472/AutoWorldStart.dll`.

Для выпуска с другим номером измените BepInPlugin в Plugin.cs и Version в AutoWorldStart.csproj; manifest.json пакета должен содержать тот же номер. GUID оставьте неизменным для сохранения настроек.

For another release number, update BepInPlugin in Plugin.cs and Version in AutoWorldStart.csproj; use the same number in the package manifest.json. Keep the plugin GUID unchanged to preserve configuration identity.

## Поддержка / Support

Мод бесплатный. Пожертвования добровольные. The mod is free; donations are optional.

[Ko-fi](https://ko-fi.com/hunterfenrir) · [PayPal](https://paypal.me/HunterFenrir67)
