# 🎮 LaunchBox RomM Plugin

![License](https://img.shields.io/badge/license-GPL--3.0-blue)
![Platform](https://img.shields.io/badge/platform-LaunchBox-orange)
![Integration](https://img.shields.io/badge/integration-RomM-green)

> Sync, install and manage your RomM library directly from LaunchBox and BigBox.

Integração completa entre **RomM Server** e **LaunchBox / BigBox**.

---

# 🇺🇸 English

## Overview

The **LaunchBox RomM Plugin** connects your local LaunchBox installation directly to a RomM server, allowing you to synchronize, install, manage and launch games seamlessly.

The plugin was designed to create an automated workflow between a self-hosted RomM server and a local LaunchBox setup.

Supports both traditional ROM setups and PC/native games depending on your LaunchBox configuration.

---

## ✨ Main Features

### 🌐 Internationalization

- English (default) and Portuguese (Brazil) support
- Language selector in Settings
- All UI elements translated including menus, forms, dialogs, and status messages

---

### 📚 Library Synchronization

- Single unified sync button for all operations
- Sync platforms directly from RomM
- Sync games directly from RomM
- Automatically create missing LaunchBox platforms
- Preserve installed/uninstalled state
- Background synchronization processing
- Keep LaunchBox and RomM synchronized
- Platform selection persistence (remember which platforms to sync)
- Admin mode for bidirectional sync (pull + push)
- Resume interrupted syncs from where they stopped
- Non-admin mode: pull everything, screenshots always bidirectional
- Auto sync on startup with configurable interval

---

### 📋 Metadata Synchronization (RomM → LaunchBox)

Auto-fill LaunchBox metadata from the RomM server with configurable priority: **LaunchBoxMetadata > ScreenScraper > IGDB > RomM Metadata**.

Fields synced when available:
- Release date, max players, play mode
- Video URL (YouTube), Wikipedia URL
- Community rating, community rating votes
- ESRB rating, synopsis / notes
- Genre, companies, game modes
- LaunchBox ID mapping (LaunchBoxDbId)

**KeepLocalData** setting controls overwrite behavior:
- `true` — only fills empty/null fields, preserves existing data; admin can push local changes to server
- `false` — overwrites all fields with server data

> **Note:** Screenshots are always bidirectional regardless of `KeepLocalData` — local screenshots are uploaded and remote screenshots are downloaded in both modes.

---

### 🖼️ Cover Art Download

- Downloads Box - Front cover art automatically from the RomM server
- Only downloads if the game has no existing cover in LaunchBox
- Uses `ForceReload` after sync so images appear immediately

---

### ⬇️ Install, Uninstall & Update Games

Right-click any RomM game in LaunchBox to access context menu actions:

| Action | Description |
|---|---|
| `RomM: Install` | Downloads and installs the game from RomM server |
| `RomM: Uninstall` | Removes local files and marks the game as uninstalled |
| `RomM: Update Metadata` | Refreshes game metadata from the RomM server |

The plugin automatically:

- Downloads games from RomM
- Extracts ZIP files
- Fixes nested folder structures
- Configures executable paths
- Marks games as installed/uninstalled
- Supports DLC packages

---

### ⬇️ Game Manager

A unified game management form with download queue support, combining download monitoring and installed game management in a single window.

**Features:**

- Unified list showing all games: pending downloads, active downloads, installed games, and failed items
- Download queue with up to 5 concurrent downloads
- Resume interrupted downloads (HTTP Range headers)
- Automatic retry (5 attempts with exponential backoff)
- Real-time speed and estimated time remaining
- Progress bar with percentage display
- Cancel individual or all downloads
- Uninstall selected games or all installed games at once
- Remove completed/failed items from the list
- Automatic sync with LaunchBox process (closes when LaunchBox closes)
- Persistent state (downloads resume after restart)

**How to use:**

1. Click `RomM: Game Manager` in the RomM menu
2. The game manager window opens showing all games with their status
3. Downloads are automatically queued when installing games
4. Monitor progress, cancel, uninstall, or clear as needed

> **Note:** Games remain in the list until they are uninstalled or removed. Failed downloads stay visible so you can retry them.

**Menu actions:**

| Action | Description |
|---|---|
| `Retry` | Retry a failed download |
| `Cancel` | Cancel selected downloads |
| `Cancel All` | Cancel all active downloads |
| `Uninstall` | Uninstall selected games |
| `Uninstall All` | Uninstall all installed games |
| `Clear` | Clear completed/failed items from the list |

---

### 📦 Automatic Archive Handling

Compressed game files are automatically extracted during installation.

The plugin also attempts to fix common nested archive structures automatically to ensure LaunchBox points to the correct executable.

---

### 🧠 Installed State Detection

The plugin automatically tracks installation status for synchronized games.

**Persistence:** Installation records are saved to `installed-games.json` in the plugin directory, ensuring survival across LaunchBox data resets.

**Recovery:** If the persistence file is lost, the system can scan `{RomsPath}/romm/` and cross-reference with LaunchBox data to rebuild the installed games list.

Installed games will:

- Have executable paths configured
- Be marked as installed inside LaunchBox
- Be tracked in `installed-games.json`
- Support uninstall actions via the Game Manager

Uninstalled games remain visible in the library without local files.

---

### ⚙️ Advanced `_launchbox.json` Support

Games may contain a `_launchbox.json` file for advanced LaunchBox integration.

Supported features include:

- Custom executable selection
- Additional applications
- Pre-loaders
- Post-loaders
- Custom command line arguments
- DLC handling

Example:

```json
{
  "DefaultFileName": "Game.exe",
  "HasDLC": false,
  "AdditionalApplications": [
    {
      "Name": "Config",
      "Path": "Config.exe"
    }
  ],
  "PreLoaders": [
    {
      "Name": "Launcher",
      "Path": "..\\Apps\\Loader.exe",
      "CommandLine": "\"%romsFolder%\"",
      "WaitToExit": false,
      "FromLaunchBoxRoot": true
    }
  ],
  "PosLoaders": [
    {
      "Name": "PostProcess",
      "Path": "PostProcess.exe",
      "CommandLine": "\"%romsFolder%\""
    }
  ]
}
```

---

### 🔹 PreLoaders

`PreLoaders` are executed before the main game starts.

They can be used for:

- Custom launchers
- Setup applications
- Virtual disk mounting
- Dependency initialization
- Compatibility tools

Supported fields:

| Field | Description |
|---|---|
| `Name` | Name displayed inside LaunchBox |
| `Path` | Executable path |
| `CommandLine` | Custom command line arguments |
| `WaitToExit` | Waits for completion before launching the game |
| `FromLaunchBoxRoot` | When `true`, `Path` is relative to the LaunchBox root instead of the game folder. Default: `false` |

---

### 🔹 PosLoaders

`PosLoaders` are executed after the main game closes.

They can be used for:

- Temporary file cleanup
- Virtual disk unmounting
- Auxiliary process termination
- Post-processing
- Automated scripts

Supported fields:

| Field | Description |
|---|---|
| `Name` | Name displayed inside LaunchBox |
| `Path` | Executable path |
| `CommandLine` | Custom command line arguments |
| `FromLaunchBoxRoot` | When `true`, `Path` is relative to the LaunchBox root instead of the game folder. Default: `false` |

---

### 🔹 AdditionalApplications

`AdditionalApplications` allow adding extra tools directly to the game inside LaunchBox.

Examples:

- Configurators
- Alternative launchers
- Editors
- Setup tools
- Trainers
- Auxiliary tools

Supported fields:

| Field | Description |
|---|---|
| `Name` | Application name |
| `Path` | Executable path |

---

### 🔹 Supported Variables

The plugin supports dynamic variables inside `CommandLine`.

| Variable | Description |
|---|---|
| `%romsFolder%` | Folder where the game was installed |

This allows portable and automated configurations.

---

### 🔄 Metadata Synchronization (LaunchBox → RomM)

One of the most powerful features of the plugin is the ability to use your existing LaunchBox metadata as the source for RomM metadata.

This allows RomM to inherit all your already-organized LaunchBox data automatically.

When **Admin mode** is enabled in Settings, the sync button performs bidirectional sync: it first pulls metadata from RomM, then pushes local LaunchBox metadata back to the server. This replaces the separate "Update Metadata" menu item with a single unified operation.

The plugin can synchronize:

- Game titles
- Descriptions
- Genres
- Release dates
- Developers
- Publishers
- Ratings
- Play modes
- Media references
- Cover art
- Screenshots (Gameplay, Title Screen)

> **Note:** Screenshots are uploaded to the RomM user's asset storage (not the ROM library folder). The `PublicScreenshots` setting controls whether screenshots are visible to all users or only the uploading user.

### Screenshot Sync (Bidirectional)

The plugin synchronizes screenshots between LaunchBox and RomM — always bidirectional:

- **Upload**: Local screenshots not on the server are always uploaded
- **Download**: Remote screenshots not local are always downloaded
- **Launch/exit sync**: When `UpdateStatsOnGameLaunch` is enabled, screenshots are synced when you launch or exit a game
- **Public control**: Only admins with `PublicScreenshots` enabled can set screenshots as public

The `KeepLocalData` setting does NOT affect screenshot sync — screenshots are always synchronized in both directions.

### Game Play Stats Sync (Bidirectional)

The plugin can synchronize play count, play time, and last played date between LaunchBox and RomM:

- **On game launch/exit**: When `UpdateStatsOnGameLaunch` is enabled, the plugin sends play sessions to RomM when you close a game
- **Bidirectional merge**: During metadata sync, the plugin compares stats from both sources and uses the most recent `LastPlayedDate`
- LaunchBox fields synced: `PlayCount`, `PlayTime` (seconds), `LastPlayedDate`
- RomM fields synced: `play_count`, `total_playtime_ms`, `last_played`

> **Note:** The `UpdateStatsOnGameLaunch` setting is opt-in (default: `false`). Enable it in Settings if you want automatic stat tracking.
- Clear Logo

Available actions:

| Action | Description |
|---|---|
| `Sync` | Pulls metadata from RomM. If Admin mode is enabled, also pushes local metadata to server |
| `Clear RomM Metadata` | Removes synchronized metadata from RomM |

### Admin vs Non-Admin Behavior

The sync behavior changes based on the `IsAdmin` setting:

| Operation | Admin | Non-Admin |
|---|---|---|
| Pull metadata from server | ✅ | ✅ |
| Pull cover art from server | ✅ | ✅ |
| Pull screenshots from server | ✅ | ✅ |
| Push local metadata to server | ✅ | ❌ |
| Push local artwork to server | ✅ | ❌ |
| Push local screenshots to server | ✅ | ✅ |
| Set screenshots as public | ✅ | ❌ (always private) |
| Send play sessions | ✅ | ✅ |
| Receive play sessions | ✅ | ✅ |
| Auto sync on startup | ✅ | ✅ |

> **Note:** Play sessions are per-user (authenticated by token), so non-admin users can send their own gameplay data. Metadata and artwork are global, so only admins can push them.

### Resume Interrupted Sync

If a sync is interrupted (crash, network error, user closes LaunchBox), the plugin saves the sync state and offers to resume on the next sync.

**State tracking:**
- `SyncInProgress` — flag indicating a sync was in progress
- `CompletedPlatformIds` — list of platform IDs that were fully processed
- `CompletedGameIdsByPlatform` — per-platform game IDs already processed (allows resuming mid-platform)

**Resume dialog:**
- **Yes** — Continue from where it stopped (skips completed platforms and games, uses previously selected platforms)
- **No** — Start fresh (clears all resume state, reprocesses everything)
- **Cancel** — Exit without syncing

> **Note:** When resuming, the platform selector form is skipped and the previously selected platforms are used automatically.

### Recommended Workflow

1. Organize metadata inside LaunchBox
2. Enable Admin mode in Settings if you want bidirectional sync
3. Click `RomM: Sync`
4. Choose which platforms to sync (selection is remembered)
5. RomM receives all LaunchBox metadata automatically (when Admin mode is enabled)

This keeps your LaunchBox and RomM metadata fully synchronized.

### Sync Performance Optimizations

The sync uses a hash-based comparison to skip unchanged games:

- **Hash-based skip**: Each game's remote metadata is hashed; if unchanged since last sync, the game is skipped entirely (no API calls)
- **Single-pass bidirectional**: One pass per game — compare, decide direction, execute. No separate pull/push passes
- **Headless mode**: Auto sync runs without dialogs (selects all platforms automatically)
- **Per-game resume tracking**: `CompletedGameIdsByPlatform` allows resuming mid-platform
- **Performance**: 8000 games with 50 changes = ~51 API calls (vs ~24000 in previous version)

---

### ⚠️ Known Limitations

#### Large Image Files

Very large artwork images (over ~10MB) may fail to upload during metadata synchronization. This is a **RomM server limitation**, not a plugin bug.

When the server takes too long to process a large file, it may close the connection before the upload completes. The plugin will retry automatically, but if the file is consistently too large, the upload will continue to fail.

**Recommendation:** Keep cover art images under **5MB** for best results. If you have very high-resolution scans, consider reducing them before adding to LaunchBox.

---

### 🗑️ Automatic Log Cleanup

The plugin automatically cleans up old log files based on the `LogRetentionDays` setting.

- Default retention: 7 days
- Logs older than the configured number of days are deleted on startup
- Helps prevent log files from accumulating over time

---

### 🔄 Force Full Resync

A settings option to reprocess all platforms from scratch on the next sync.

**When to use:**

- When the library gets out of sync and normal sync doesn't fix it
- After adding/removing platforms on the RomM server
- When you want to force a complete re-comparison of all games

**What it does:**

1. Shows a confirmation dialog before proceeding
2. Clears the resume state in `sync_information.json` (SyncInProgress + CompletedPlatformIds)
3. All platforms will be re-processed on the next sync
4. Local data is preserved and compared with the server (non-destructive)

**How to use:**

1. Open plugin settings (`RomM: Settings`)
2. Check `Force full resync on next sync?`
3. Save settings
4. Run `RomM: Sync`
5. Confirm the dialog when prompted

---

### 🔒 Parents Hierarchy Fix

After each sync, the plugin directly manipulates `Parents.xml` to ensure correct hierarchy.

**What it fixes:**
- RomM categories (e.g., "RomM | Arcade") are set with parent "RomM"
- RomM platforms are linked to their correct parent category
- Duplicate Parent entries are removed

This uses direct XML manipulation because the LaunchBox API does not support setting parent categories programmatically.

---

### 🧠 Internal Processing Features

- Background event queue system
- `pending.json` event tracking (install + uninstall)
- `installed-games.json` for persistent install state
- Automatic cleanup of completed operations

---

## 📦 Requirements

- LaunchBox / BigBox
- Active RomM server
- Windows environment
- Network access to RomM server

---

## 📦 Installation

### 1. Download the Plugin

Download the latest release from the GitHub Releases page.

### 2. Extract Into LaunchBox

Extract the plugin folder into:

```text
LaunchBox/Plugins/RomM LaunchBox Integration
```

Expected structure:

```text
LaunchBox
 └── Plugins
      └── RomM LaunchBox Integration
```

### 3. Configure the Plugin

| Setting | Description |
|---|---|
| `RommBaseUrl` | RomM server URL (e.g. http://192.168.1.100:9000) |
| `Username` | RomM username |
| `Password` | RomM password |
| `ClientApiToken` | RomM Client API token (`rmm_...`). If set, it is used instead of username/password |
| `RomsPath` | Local folder where games will be installed |
| `KeepLocalData` | `true` = preserve existing LaunchBox data, `false` = overwrite |
| `Language` | UI language code (`en` or `pt-BR`). Default: `en` |
| `ForceFullResync` | `true` = clear resume state and reprocess all platforms on next sync (non-destructive) |
| `ProcessPendingOnStartup` | `true` = auto-process pending install/uninstall events on LaunchBox startup (default: `true`) |
| `PublicScreenshots` | `true` = uploaded screenshots are visible to all users (default: `true`) |
| `UpdateStatsOnGameLaunch` | `true` = sync play count/time on game launch/exit (default: `false`) |
| `DetailedSyncLogs` | `true` = log detailed timing for each sync operation (default: `false`) |
| `LogRetentionDays` | Number of days to keep log files before automatic deletion (default: `7`) |
| `IsAdmin` | `true` = enable bidirectional sync: pull from RomM + push local metadata to server (default: `false`) |
| `ForcePushToServer` | `true` = admin only. Push all local metadata, artwork and screenshots to server, overwriting remote data (default: `false`) |

You can configure via `settings.json` or the LaunchBox plugin settings UI.

#### Authentication: Client API token vs username/password

You can authenticate either with a username/password or with a **Client API token**, which
is more secure than storing credentials. Generate a token in RomM under
**Administration → Client API Tokens** (format `rmm_` + 64 hex characters) and paste it into the
`Client API Token` field.

- If a token is provided, it **takes priority** over username/password (sent as
  `Authorization: Bearer rmm_...`).
- When you save with both a token and a username/password present, the plugin asks whether to
  clear the stored username and password.
- Provide **either** a token **or** a username and password.

#### Test Connection

The settings UI includes a **Test Connection** button that validates your server URL and
credentials against the RomM server before saving, so you can confirm everything works up front.

### Sync State (`sync_information.json`)

The plugin stores sync progress in a separate `sync_information.json` file, keeping user settings clean.

| Field | Description |
|---|---|
| `SyncInProgress` | `true` when a sync was interrupted (managed automatically) |
| `CompletedPlatformIds` | Platform IDs already fully processed (managed automatically) |
| `CompletedGameIdsByPlatform` | Per-platform game IDs already processed (managed automatically) |
| `UnselectedPlatformIds` | Platform IDs deselected by the user in the selector |
| `CurrentPlatforms` | All known RomM platforms (managed automatically) |

> **Note:** This file is managed automatically. Do not edit manually unless you know what you are doing.

### 4. Open LaunchBox

Start LaunchBox normally. The plugin menus will become available automatically.

### 5. Synchronize Your Library

Use the menu option:

```text
RomM: Sync
```

The plugin will:

- Connect to RomM
- Retrieve platforms and games
- Create missing platforms automatically
- Apply metadata from the server (with KeepLocalData respect)
- Download cover art for games without existing covers
- Remove games that no longer exist on the server and clean up orphan images
- Reload the LaunchBox library automatically

---

## 🧠 Internal Sync System

The plugin uses a `pending.json` file internally to process queued events.

This system manages:

- Install events
- Uninstall events
- Metadata synchronization
- Automatic cleanup
- Background execution

This ensures LaunchBox and RomM stay synchronized safely.

---

## 🕹️ Recommended Usage Flow

```text
RomM Server
     ↓
Sync Library
     ↓
LaunchBox Imports Games
     ↓
Right-click → RomM: Install
     ↓
Plugin Downloads + Configures Game
     ↓
Play Through LaunchBox / BigBox
```

---

## ⚠️ Known Limitations

- Requires an active RomM server connection
- Installation folders must be writable
- Some emulators may still require manual LaunchBox configuration
- Metadata synchronization depends on existing LaunchBox metadata
- Large libraries (1000+ games) may take 15-30 minutes for full metadata sync depending on server performance and network speed

---

## 🤝 Contributing

Contributions are welcome.

If you find bugs or want improvements:

- Open an issue
- Submit a pull request
- Include reproduction steps whenever possible

---

## 📄 License

GPL-3.0 License

---

# 🇧🇷 Português

## Visão Geral

O **LaunchBox RomM Plugin** conecta sua instalação local do LaunchBox diretamente a um servidor RomM, permitindo sincronizar, instalar, gerenciar e executar jogos de forma integrada.

O plugin foi desenvolvido para criar um fluxo automatizado entre um servidor RomM self-hosted e uma instalação local do LaunchBox.

Suporta tanto bibliotecas tradicionais de ROMs quanto jogos nativos de PC, dependendo da configuração do seu LaunchBox.

---

## ✨ Funcionalidades

### 🌐 Internacionalização

- Suporte a Inglês (padrão) e Português (Brasil)
- Seletor de idioma nas Configurações
- Todos os elementos da UI traduzidos, incluindo menus, formulários, diálogos e mensagens de status

---

### 📚 Sincronização da Biblioteca

- Botão único unificado para todas as operações
- Sincroniza plataformas diretamente do RomM
- Sincroniza jogos diretamente do RomM
- Cria plataformas automaticamente no LaunchBox
- Mantém o status de instalado/desinstalado
- Processamento de sincronização em background
- Mantém LaunchBox e RomM sincronizados
- Persistência da seleção de plataformas (lembra quais plataformas sincronizar)
- Modo Admin para sync bidirecional (pull + push)
- Retomada de sincronização interrompida de onde parou
- Modo non-admin: puxa tudo, screenshots sempre bidirecionais
- Auto sync na inicialização com intervalo configurável

---

### 📋 Sincronização de Metadados (RomM → LaunchBox)

Preenche automaticamente os metadados do LaunchBox com dados do servidor, com prioridade configurável: **LaunchBoxMetadata > ScreenScraper > IGDB > RomM Metadata**.

Campos sincronizados quando disponíveis:
- Data de lançamento, máximo de jogadores, modo de jogo
- Vídeo (YouTube), Wikipedia
- Rating comunitário, votos do rating
- Classificação ESRB, sinopse / notas
- Gênero, empresas, modos de jogo
- LaunchBox ID (LaunchBoxDbId)

**KeepLocalData** controla a sobrescrição:
- `true` — só preenche campos vazios, preserva dados existentes; admin pode enviar alterações locais ao servidor
- `false` — sobrescreve todos os campos com dados do servidor

> **Nota:** Screenshots são sempre bidirecionais independente de `KeepLocalData` — screenshots locais são enviados e screenshots remotos são baixados em ambos os modos.

---

### 🖼️ Download de Capa

- Baixa a capa (Box - Front) automaticamente do servidor RomM
- Só baixa se o jogo não tiver capa no LaunchBox
- Usa `ForceReload` após a sync para as imagens aparecerem imediatamente

---

### ⬇️ Instalar, Desinstalar e Atualizar Jogos

Clique com o botão direito em qualquer jogo RomM no LaunchBox para acessar as ações do menu de contexto:

| Ação | Descrição |
|---|---|
| `RomM: Instalar` | Baixa e instala o jogo do servidor RomM |
| `RomM: Desinstalar` | Remove arquivos locais e marca o jogo como desinstalado |
| `RomM: Atualizar Metadados` | Atualiza os metadados do jogo a partir do servidor RomM |

O plugin automaticamente:

- Faz download dos jogos pelo RomM
- Extrai arquivos ZIP
- Corrige estruturas de pastas aninhadas
- Configura caminhos de executáveis automaticamente
- Marca jogos como instalados/desinstalados
- Suporta pacotes de DLC

---

### ⬇️ Gerenciador de Jogos

Um formulário unificado de gerenciamento de jogos com suporte a fila de downloads, combinando monitoramento de downloads e gerenciamento de jogos instalados em uma única janela.

**Funcionalidades:**

- Lista unificada mostrando todos os jogos: downloads pendentes, downloads ativos, jogos instalados e itens com falha
- Fila de downloads com até 5 downloads simultâneos
- Retomada de downloads interrompidos (HTTP Range headers)
- Retry automático (5 tentativas com backoff exponencial)
- Velocidade em tempo real e tempo estimado restante
- Barra de progresso com exibição de porcentagem
- Cancelar individual ou todos os downloads
- Desinstalar jogos selecionados ou todos os jogos instalados de uma vez
- Remover itens concluídos/com falha da lista
- Sincronização automática com o processo do LaunchBox (fecha quando o LaunchBox fecha)
- Estado persistente (downloads retomam após reinício)

**Como usar:**

1. Clique em `RomM: Gerenciador de Jogos` no menu do RomM
2. A janela do gerenciador de jogos abre mostrando todos os jogos com seu status
3. Os downloads são automaticamente enfileirados ao instalar jogos
4. Monitore o progresso, cancele, desinstale ou limpe conforme necessário

> **Nota:** Os jogos permanecem na lista até serem desinstalados ou removidos. Downloads com falha ficam visíveis para que você possa tentar novamente.

**Ações do menu:**

| Ação | Descrição |
|---|---|
| `Tentar Novamente` | Tentar novamente um download com falha |
| `Cancelar` | Cancelar downloads selecionados |
| `Cancelar Tudo` | Cancelar todos os downloads ativos |
| `Desinstalar` | Desinstalar jogos selecionados |
| `Desinstalar Tudo` | Desinstalar todos os jogos instalados |
| `Limpar` | Limpar itens concluídos/com falha da lista |

---

### 📦 Manipulação Automática de Arquivos Compactados

Arquivos compactados são extraídos automaticamente durante a instalação.

O plugin também tenta corrigir automaticamente estruturas comuns de pastas aninhadas para garantir que o LaunchBox aponte para o executável correto.

---

### 🧠 Detecção de Estado de Instalação

O plugin rastreia automaticamente o status de instalação dos jogos sincronizados.

**Persistência:** Os registros de instalação são salvos em `installed-games.json` na pasta do plugin, garantindo sobrevivência a redefinições de dados do LaunchBox.

**Recuperação:** Se o arquivo de persistência for perdido, o sistema pode escanear `{RomsPath}/romm/` e cruzar com dados do LaunchBox para reconstruir a lista de jogos instalados.

Jogos instalados:

- Possuem caminhos de executáveis configurados
- São marcados como instalados dentro do LaunchBox
- São rastreados em `installed-games.json`
- Suportam ações de desinstalação via o Gerenciador de Jogos Instalados

Jogos não instalados permanecem visíveis na biblioteca sem arquivos locais.

---

### ⚙️ Suporte Avançado ao `_launchbox.json`

Os jogos podem conter um arquivo `_launchbox.json` para integração avançada com o LaunchBox.

Os recursos suportados incluem:

- Seleção personalizada de executável
- Aplicações adicionais
- Pre-loaders
- Pós-loaders
- Argumentos personalizados de linha de comando
- Suporte a DLC
- Flag `FromLaunchBoxRoot` para caminhos relativos à raiz do LaunchBox

Campos do JSON:

| Campo | Descrição |
|---|---|
| `DefaultFileName` | Executável principal do jogo (caminho relativo à pasta do jogo) |
| `HasDLC` | Se `true`, ativa detecção automática de DLCs na pasta `_DLCs` |
| `AdditionalApplications[*].Path` | Caminho relativo à **pasta do jogo** |
| `PreLoaders[*].Path` | Caminho relativo à pasta do jogo (ou ao LaunchBox se `FromLaunchBoxRoot: true`) |
| `PosLoaders[*].Path` | Caminho relativo à pasta do jogo (ou ao LaunchBox se `FromLaunchBoxRoot: true`) |
| `FromLaunchBoxRoot` | `true` = caminho relativo à raiz do LaunchBox, `false` = relativo à pasta do jogo (default) |

### 🔄 Sincronização de Metadados (LaunchBox → RomM)

Uma das funcionalidades mais poderosas do plugin é a possibilidade de utilizar os metadados já existentes no LaunchBox como fonte para os metadados do RomM.

Isso permite que o RomM herde automaticamente toda a organização já existente no LaunchBox.

Quando o **Modo Admin** está habilitado nas Configurações, o botão de sync realiza sincronização bidirecional: primeiro faz pull dos metadados do RomM, depois envia os metadados locais do LaunchBox de volta ao servidor. Isso substitui o item de menu separado "Atualizar Metadados" com uma operação unificada.

O plugin pode sincronizar:

- Nome dos jogos
- Descrições
- Gêneros
- Datas de lançamento
- Desenvolvedores
- Publishers
- Avaliações
- Modos de jogo
- Referências de mídia
- Capa
- Screenshots (Gameplay, Title Screen)

> **Nota:** Screenshots são enviados para o armazenamento de assets do usuário RomM (não na pasta da ROM). A configuração `PublicScreenshots` controla se os screenshots ficam visíveis para todos os usuários ou apenas para quem os enviou.

### Sincronização de Screenshots (Bidirecional)

O plugin sincroniza screenshots entre LaunchBox e RomM — sempre bidirecional:

- **Upload**: Screenshots locais que não existem no servidor são sempre enviados
- **Download**: Screenshots remotos que não existem localmente são sempre baixados
- **Sync no launch/exit**: Quando `UpdateStatsOnGameLaunch` está habilitado, screenshots são sincronizados ao abrir ou fechar um jogo
- **Controle de publicidade**: Apenas admins com `PublicScreenshots` habilitado podem tornar screenshots públicos

A configuração `KeepLocalData` NÃO afeta a sincronização de screenshots — screenshots são sempre sincronizados em ambas as direções.

### Sincronização de Estatísticas de Jogo (Bidirecional)

O plugin pode sincronizar contagem de jogadas, tempo de jogo e última data de jogo entre LaunchBox e RomM:

- **Ao abrir/fechar jogo**: Quando `UpdateStatsOnGameLaunch` está habilitado, o plugin envia sessões de jogo para o RomM quando você fecha um jogo
- **Merge bidirecional**: Durante a sincronização de metadados, o plugin compara estatísticas de ambas as fontes e usa a `LastPlayedDate` mais recente
- Campos LaunchBox sincronizados: `PlayCount`, `PlayTime` (segundos), `LastPlayedDate`
- Campos RomM sincronizados: `play_count`, `total_playtime_ms`, `last_played`

> **Nota:** A configuração `UpdateStatsOnGameLaunch` é opcional (padrão: `false`). Habilite nas Configurações se quiser rastreamento automático de estatísticas.
- Clear Logo

Ações disponíveis:

| Ação | Descrição |
|---|---|
| `Sync` | Faz pull dos metadados do RomM. Se o Modo Admin estiver habilitado, também envia metadados locais ao servidor |
| `Clear RomM Metadata` | Remove os metadados sincronizados do RomM |

### Comportamento Admin vs Non-Admin

O comportamento da sincronização muda com base na configuração `IsAdmin`:

| Operação | Admin | Non-Admin |
|---|---|---|
| Puxar metadados do servidor | ✅ | ✅ |
| Puxar capa do servidor | ✅ | ✅ |
| Puxar screenshots do servidor | ✅ | ✅ |
| Enviar metadados locais ao servidor | ✅ | ❌ |
| Enviar artwork local ao servidor | ✅ | ❌ |
| Enviar screenshots locais ao servidor | ✅ | ✅ |
| Tornar screenshots públicos | ✅ | ❌ (sempre privados) |
| Enviar sessões de jogo | ✅ | ✅ |
| Receber sessões de jogo | ✅ | ✅ |
| Auto sync na inicialização | ✅ | ✅ |

> **Nota:** Sessões de jogo são porusuário (autenticadas por token), então usuários non-admin podem enviar seus próprios dados de gameplay. Metadados e artwork são globais, então apenas admins podem enviá-los.

### Retomada de Sincronização Interrompida

Se uma sincronização for interrompida (crash, erro de rede, usuário fecha o LaunchBox), o plugin salva o estado da sincronização e oferece retomar na próxima vez.

**Rastreamento de estado:**
- `SyncInProgress` — flag indicando que uma sincronização estava em andamento
- `CompletedPlatformIds` — lista de IDs de plataformas que foram processadas completamente
- `CompletedGameIdsByPlatform` — IDs de jogos por plataforma já processados (permite retomar no meio de uma plataforma)

**Diálogo de retomada:**
- **Sim** — Continuar de onde parou (pula plataformas e jogos concluídos, usa plataformas selecionadas anteriormente)
- **Não** — Recomeçar do zero (limpa todo o estado de resume, reprocessa tudo)
- **Cancelar** — Sair sem sincronizar

> **Nota:** Ao retomar, o formulário de seleção de plataformas é pulado e as plataformas selecionadas anteriormente são usadas automaticamente.

### Fluxo Recomendado

1. Organize os metadados dentro do LaunchBox
2. Habilite o Modo Admin nas Configurações se quiser sync bidirecional
3. Clique em `RomM: Sincronizar`
4. Escolha quais plataformas sincronizar (a seleção é lembrada)
5. O RomM receberá automaticamente todos os metadados do LaunchBox (quando o Modo Admin está habilitado)

### Otimizações de Performance do Sync

A sincronização usa comparação baseada em hash para pular jogos inalterados:

- **Skip por hash**: O metadata remoto de cada jogo é hasheado; se inalterado desde a última sync, o jogo é pulado completamente (sem chamadas API)
- **Passo único bidirecional**: Um passo por jogo — compara, decide direção, executa. Sem passes separados de pull/push
- **Modo headless**: Auto sync roda sem diálogos (seleciona todas as plataformas automaticamente)
- **Rastreamento de resume por jogo**: `CompletedGameIdsByPlatform` permite retomar no meio de uma plataforma
- **Performance**: 8000 jogos com 50 alterações = ~51 chamadas API (vs ~24000 na versão anterior)

---

### ⚠️ Limitações Conhecidas

#### Arquivos de Imagem Grandes

Imagens de artwork muito grandes (acima de ~10MB) podem falhar no upload durante a sincronização de metadados. Esta é uma **limitação do servidor RomM**, não um bug do plugin.

Quando o servidor demora muito para processar um arquivo grande, ele pode fechar a conexão antes que o upload seja concluído. O plugin fará retry automaticamente, mas se o arquivo for consistentemente grande demais, o upload continuará falhando.

**Recomendação:** Mantenha as imagens de cover art abaixo de **5MB** para melhores resultados. Se você memiliki digitalizações de alta resolução, considere reduzi-las antes de adicionar ao LaunchBox.

---

### 🗑️ Limpeza Automática de Logs

O plugin limpa automaticamente arquivos de log antigos com base na configuração `LogRetentionDays`.

- Retenção padrão: 7 dias
- Logs mais antigos que o número de dias configurados são deletados na inicialização
- Ajuda a evitar a acumulação de arquivos de log ao longo do tempo

---

### 🔄 Re-sincronização Completa

Uma opção nas configurações para reprocessar todas as plataformas do zero na próxima sincronização.

**Quando usar:**

- Quando a biblioteca fica dessincronizada e a sync normal não resolve
- Ao adicionar/remover plataformas no servidor RomM
- Quando você quer forçar uma re-comparação completa de todos os jogos

**O que faz:**

1. Exibe um diálogo de confirmação antes de prosseguir
2. Limpa o estado de resume no `sync_information.json` (SyncInProgress + CompletedPlatformIds)
3. Todas as plataformas serão reprocessadas na próxima sincronização
4. Dados locais são preservados e comparados com o servidor (não destrutivo)

**Como usar:**

1. Abra as configurações do plugin (`RomM: Settings`)
2. Marque `Forçar re-sincronização completa?`
3. Salve as configurações
4. Execute `RomM: Sincronizar`
5. Confirme o diálogo quando solicitado

---

### 🔒 Correção da Hierarquia de Parents

Após cada sincronização, o plugin manipula diretamente o `Parents.xml` para garantir a hierarquia correta.

**O que corrige:**
- Categorias RomM (ex.: "RomM | Arcade") recebem parent "RomM"
- Plataformas RomM são vinculadas à categoria pai correta
- Entradas duplicadas no Parents são removidas

Isso usa manipulação direta de XML porque a API do LaunchBox não suporta definir categorias pai programaticamente.

---

## 📦 Instalação

### 1. Baixe o Plugin

Faça download da versão mais recente através da página de Releases do GitHub.

### 2. Extraia Dentro do LaunchBox

Extraia a pasta do plugin em:

```text
LaunchBox/Plugins/RomM LaunchBox Integration
```

### 3. Configure o Plugin

| Configuração | Descrição |
|---|---|
| `RommBaseUrl` | URL do servidor RomM (ex.: http://192.168.1.100:9000) |
| `Username` | Usuário do RomM |
| `Password` | Senha do RomM |
| `ClientApiToken` | Token de API do RomM (`rmm_...`). Se definido, é usado no lugar de usuário/senha |
| `RomsPath` | Pasta local onde os jogos serão instalados |
| `KeepLocalData` | `true` = preserva dados existentes, `false` = sobrescreve |
| `Language` | Código do idioma da UI (`en` ou `pt-BR`). Padrão: `en` |
| `ForceFullResync` | `true` = limpa estado de resume e reprocessa todas as plataformas na próxima sync (não destrutivo) |
| `ProcessPendingOnStartup` | `true` = processa automaticamente eventos pendentes ao iniciar o LaunchBox (padrão: `true`) |
| `PublicScreenshots` | `true` = screenshots enviados ficam visíveis para todos os usuários (padrão: `true`) |
| `UpdateStatsOnGameLaunch` | `true` = sincroniza contagem/tempo de jogo ao abrir/fechar (padrão: `false`) |
| `DetailedSyncLogs` | `true` = log detalhado de tempos de cada operação de sync (padrão: `false`) |
| `LogRetentionDays` | Número de dias para manter arquivos de log antes da exclusão automática (padrão: `7`) |
| `IsAdmin` | `true` = habilita sync bidirecional: pull do RomM + envia metadados locais ao servidor (padrão: `false`) |
| `ForcePushToServer` | `true` = somente admin. Envia todas as metadata, artwork e screenshots locais ao servidor, sobrescrevendo dados remotos (padrão: `false`) |

#### Autenticação: token de API vs usuário/senha

Você pode autenticar com usuário/senha ou com um **Client API token**, que é mais seguro do que
armazenar credenciais. Gere um token no RomM em **Administration → Client API Tokens**
(formato `rmm_` + 64 caracteres hexadecimais) e cole no campo `Client API Token`.

- Se um token for informado, ele **tem prioridade** sobre usuário/senha (enviado como
  `Authorization: Bearer rmm_...`).
- Ao salvar com token e usuário/senha preenchidos, o plugin pergunta se deseja limpar o usuário e a
  senha armazenados.
- Informe **um** token **ou** usuário e senha.

#### Testar Conexão

A tela de configurações inclui um botão **Test Connection** que valida a URL do servidor e as
credenciais contra o servidor RomM antes de salvar, permitindo confirmar que tudo funciona.

### Estado da Sincronização (`sync_information.json`)

O plugin armazena o progresso da sincronização em um arquivo separado `sync_information.json`, mantendo as configurações do usuário limpas.

| Campo | Descrição |
|---|---|
| `SyncInProgress` | `true` quando uma sincronização foi interrompida (gerenciado automaticamente) |
| `CompletedPlatformIds` | IDs de plataformas já processadas completamente (gerenciado automaticamente) |
| `CompletedGameIdsByPlatform` | IDs de jogos por plataforma já processados (gerenciado automaticamente) |
| `UnselectedPlatformIds` | IDs de plataformas desmarcadas pelo usuário no seletor |
| `CurrentPlatforms` | Todas as plataformas RomM conhecidas (gerenciado automaticamente) |

> **Nota:** Este arquivo é gerenciado automaticamente. Não edite manualmente a menos que saiba o que está fazendo.

### 4. Sincronize Sua Biblioteca

Utilize a opção do menu:

```text
RomM: Sync
```

---

## 🧠 Sistema Interno de Sincronização

O plugin utiliza internamente o arquivo `pending.json` para processar eventos pendentes, e `installed-games.json` para rastreamento persistente do estado de instalação.

---

## 🕹️ Fluxo Recomendado de Uso

```text
Servidor RomM
     ↓
Sincronizar Biblioteca
     ↓
LaunchBox Importa Jogos
     ↓
Clique direito → RomM: Instalar
     ↓
Plugin Faz Download + Configuração
     ↓
Executar Pelo LaunchBox / BigBox
```

---

## 🤝 Contribuições

Contribuições são bem-vindas. Abra uma issue ou envie um pull request.

---

## 📄 Licença

GPL-3.0 License
