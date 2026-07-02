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

### 📚 Library Synchronization

- Sync platforms directly from RomM
- Sync games directly from RomM
- Automatically create missing LaunchBox platforms
- Preserve installed/uninstalled state
- Background synchronization processing
- Keep LaunchBox and RomM synchronized

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
- `true` — only fills empty/null fields, preserves existing data
- `false` — overwrites all fields

---

### 🖼️ Cover Art Download

- Downloads Box - Front cover art automatically from the RomM server
- Only downloads if the game has no existing cover in LaunchBox
- Uses `ForceReload` after sync so images appear immediately

---

### ⬇️ Install & Uninstall Games

Every synchronized game receives additional actions:

- `Install (RomM)`
- `Uninstall (RomM)`

The plugin automatically:

- Downloads games from RomM
- Extracts ZIP files
- Fixes nested folder structures
- Configures executable paths
- Marks games as installed/uninstalled
- Supports DLC packages

---

### 📦 Automatic Archive Handling

Compressed game files are automatically extracted during installation.

The plugin also attempts to fix common nested archive structures automatically to ensure LaunchBox points to the correct executable.

---

### 🧠 Installed State Detection

The plugin automatically tracks installation status for synchronized games.

Installed games will:

- Have executable paths configured
- Be marked as installed inside LaunchBox
- Support uninstall actions directly from LaunchBox

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

Available actions:

| Action | Description |
|---|---|
| `Update RomM Metadata` | Sends LaunchBox metadata + cover art to RomM |
| `Clear RomM Metadata` | Removes synchronized metadata from RomM |

### Recommended Workflow

1. Organize metadata inside LaunchBox
2. Synchronize your RomM library
3. Execute `Update RomM Metadata`
4. RomM receives all LaunchBox metadata automatically

This keeps your LaunchBox and RomM metadata fully synchronized.

---

### 🧠 Internal Processing Features

- Background event queue system
- `romm.sync` event tracking
- Hidden CLI execution (no console popup)
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

### 4. Open LaunchBox

Start LaunchBox normally. The plugin menus will become available automatically.

### 5. Synchronize Your Library

Use the menu option:

```text
RomM: Sync roms list from server
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

The plugin uses a `romm.sync` file internally to process queued events.

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
Install Game From LaunchBox
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

### 📚 Sincronização da Biblioteca

- Sincroniza plataformas diretamente do RomM
- Sincroniza jogos diretamente do RomM
- Cria plataformas automaticamente no LaunchBox
- Mantém o status de instalado/desinstalado
- Processamento de sincronização em background
- Mantém LaunchBox e RomM sincronizados

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
- `true` — só preenche campos vazios, preserva dados existentes
- `false` — sobrescreve todos os campos

---

### 🖼️ Download de Capa

- Baixa a capa (Box - Front) automaticamente do servidor RomM
- Só baixa se o jogo não tiver capa no LaunchBox
- Usa `ForceReload` após a sync para as imagens aparecerem imediatamente

---

### ⬇️ Instalar e Desinstalar Jogos

Cada jogo sincronizado recebe ações adicionais:

- `Install (RomM)`
- `Uninstall (RomM)`

O plugin automaticamente:

- Faz download dos jogos pelo RomM
- Extrai arquivos ZIP
- Corrige estruturas de pastas aninhadas
- Configura caminhos de executáveis automaticamente
- Marca jogos como instalados/desinstalados
- Suporta pacotes de DLC

---

### 📦 Manipulação Automática de Arquivos Compactados

Arquivos compactados são extraídos automaticamente durante a instalação.

O plugin também tenta corrigir automaticamente estruturas comuns de pastas aninhadas para garantir que o LaunchBox aponte para o executável correto.

---

### 🧠 Detecção de Estado de Instalação

O plugin rastreia automaticamente o status de instalação dos jogos sincronizados.

Jogos instalados:

- Possuem caminhos de executáveis configurados
- São marcados como instalados dentro do LaunchBox
- Suportam ações de desinstalação diretamente pelo LaunchBox

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

Ações disponíveis:

| Ação | Descrição |
|---|---|
| `Update RomM Metadata` | Envia metadados + capa do LaunchBox para o RomM |
| `Clear RomM Metadata` | Remove os metadados sincronizados do RomM |

### Fluxo Recomendado

1. Organize os metadados dentro do LaunchBox
2. Sincronize sua biblioteca do RomM
3. Execute `Update RomM Metadata`
4. O RomM receberá automaticamente todos os metadados do LaunchBox

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

### 4. Sincronize Sua Biblioteca

Utilize a opção do menu:

```text
RomM: Sync roms list from server
```

---

## 🧠 Sistema Interno de Sincronização

O plugin utiliza internamente um arquivo `romm.sync` para processar eventos pendentes.

---

## 🕹️ Fluxo Recomendado de Uso

```text
Servidor RomM
     ↓
Sincronizar Biblioteca
     ↓
LaunchBox Importa Jogos
     ↓
Instalar Jogo Pelo LaunchBox
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
