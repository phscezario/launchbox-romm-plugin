# LaunchBox RomM Plugin

![CI](https://github.com/phscezario/launchbox-romm-plugin/actions/workflows/ci.yml/badge.svg)
![License](https://img.shields.io/badge/license-GPL--3.0-blue)
![Platform](https://img.shields.io/badge/platform-LaunchBox-orange)
![Integration](https://img.shields.io/badge/integration-RomM-green)
![Version](https://img.shields.io/badge/version-1.0.3-green)

> Sync, install and manage your RomM library directly from LaunchBox and BigBox.

Integração completa entre **RomM Server** e **LaunchBox / BigBox**.

---

# Documentation

For developers and contributors, see the **[docs/](docs/)** folder:

| Document | Description |
|----------|-------------|
| [Architecture](docs/architecture.md) | System architecture, project structure, design patterns |
| [Development Setup](docs/development-setup.md) | Prerequisites, cloning, building, IDE configuration |
| [Contributing](docs/contributing.md) | Contribution guidelines, commit conventions, PR process |
| [Testing](docs/testing.md) | How to run and write unit tests |
| [CI/CD](docs/ci-cd.md) | GitHub Actions workflows and release pipeline |
| [Configuration](docs/configuration.md) | Complete settings reference |
| [Deployment](docs/deployment.md) | Building for release, package structure, installation |
| [Release Process](docs/release-process.md) | Versioning, tagging, and GitHub Releases |
| [Localization](docs/localization.md) | i18n system, adding new languages |
| [API Integration](docs/api-integration.md) | RomM REST API endpoints and data models |

---

# English

## Overview

The **LaunchBox RomM Plugin** connects your local LaunchBox installation directly to a RomM server, allowing you to synchronize, install, manage and launch games seamlessly.

The plugin was designed to create an automated workflow between a self-hosted RomM server and a local LaunchBox setup.

Supports both traditional ROM setups and PC/native games depending on your LaunchBox configuration.

---

## Features

### Library Synchronization

- Single unified sync button for all operations
- Sync platforms and games directly from RomM
- Automatically create missing LaunchBox platforms
- Preserve installed/uninstalled state
- Platform selection persistence (remember which platforms to sync)
- Admin mode for bidirectional sync (pull + push)
- Resume interrupted syncs from where they stopped
- Auto sync on startup with configurable interval
- Hash-based optimization: 8000 games with 50 changes = ~51 API calls

### Metadata Synchronization

Auto-fill LaunchBox metadata from the RomM server with configurable priority: **LaunchBoxMetadata > ScreenScraper > IGDB > RomM Metadata**.

Fields synced: release date, max players, play mode, video URL, Wikipedia URL, community rating, ESRB rating, synopsis, genre, companies, game modes, LaunchBox ID mapping.

- **KeepLocalData** = `true`: only fills empty/null fields, preserves existing data
- **KeepLocalData** = `false`: overwrites all fields with server data
- Screenshots are always bidirectional regardless of this setting

### Cover Art Download

- Downloads Box - Front cover art automatically from the RomM server
- Only downloads if the game has no existing cover in LaunchBox

### Install, Uninstall & Update Games

Right-click any RomM game in LaunchBox:

| Action | Description |
|---|---|
| `RomM: Install` | Downloads and installs the game from RomM server |
| `RomM: Uninstall` | Removes local files and marks the game as uninstalled |
| `RomM: Update Metadata` | Refreshes game metadata from the RomM server |

### Game Manager

A unified game management form with download queue support:

- Download queue with up to 5 concurrent downloads
- Resume interrupted downloads (HTTP Range headers)
- Automatic retry (5 attempts with exponential backoff)
- Real-time speed and estimated time remaining
- Uninstall selected or all installed games
- Persistent state (downloads resume after restart)

### `_launchbox.json` Support

Advanced LaunchBox integration via per-game `_launchbox.json`:

- Custom executable selection
- Pre-loaders and post-loaders
- Additional applications
- Custom command line arguments (`%romsFolder%` variable)
- DLC handling

### Admin vs Non-Admin

| Operation | Admin | Non-Admin |
|---|---|---|
| Pull metadata from server | ✅ | ✅ |
| Pull cover art from server | ✅ | ✅ |
| Pull screenshots from server | ✅ | ✅ |
| Push local metadata to server | ✅ | ❌ |
| Push local artwork to server | ✅ | ❌ |
| Push local screenshots to server | ✅ | ✅ |
| Send play sessions | ✅ | ✅ |
| Auto sync on startup | ✅ | ✅ |

### Resume Interrupted Sync

If a sync is interrupted, the plugin saves the state and offers to resume on the next sync. Per-game resume tracking allows resuming mid-platform.

### Other Features

- Internationalization: English and Portuguese (Brazil)
- Automatic archive extraction during installation
- Installed state detection with persistent tracking
- Parents hierarchy fix (direct `Parents.xml` manipulation)
- Automatic log cleanup with configurable retention
- Force full resync option
- Bidirectional play stats sync (play count, play time, last played)
- Bidirectional screenshot sync
- Auto-update from GitHub Releases

---

## Requirements

- LaunchBox / BigBox
- Active RomM server
- Windows environment
- Network access to RomM server

---

## Installation

### 1. Download the Plugin

Download the latest release from the [GitHub Releases](https://github.com/phscezario/launchbox-romm-plugin/releases) page.

### 2. Extract Into LaunchBox

```text
LaunchBox/Plugins/RomM LaunchBox Integration
```

### 3. Configure the Plugin

Open `settings.json` in the plugin folder and configure:

| Setting | Description |
|---|---|
| `RommBaseUrl` | RomM server URL (e.g. `http://192.168.1.100:9000`) |
| `Username` | RomM username |
| `Password` | RomM password |
| `ClientApiToken` | RomM Client API token (`rmm_...`). Takes priority over username/password |
| `RomsPath` | Local folder where games will be installed |
| `KeepLocalData` | `true` = preserve existing LaunchBox data, `false` = overwrite |
| `IsAdmin` | `true` = enable bidirectional sync (default: `false`) |

Or use the Settings UI in LaunchBox: `RomM: Settings`

See [Configuration Reference](docs/configuration.md) for all available settings.

### 4. Synchronize Your Library

Click `RomM: Sync` in the LaunchBox menu. The plugin will connect to RomM, retrieve platforms and games, create missing platforms, apply metadata, and download cover art.

---

## Recommended Usage Flow

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

## Known Limitations

- Requires an active RomM server connection
- Very large artwork images (>10MB) may fail to upload (RomM server limitation)
- Some emulators may still require manual LaunchBox configuration
- Large libraries (1000+ games) may take 15-30 minutes for full metadata sync

---

## Contributing

Contributions are welcome! See [Contributing Guidelines](docs/contributing.md) for:

- Commit conventions (conventional commits)
- Code style and naming conventions
- PR process and review workflow
- How to report bugs

---

## License

GPL-3.0 License

---

# Português

## Visão Geral

O **LaunchBox RomM Plugin** conecta sua instalação local do LaunchBox diretamente a um servidor RomM, permitindo sincronizar, instalar, gerenciar e executar jogos de forma integrada.

O plugin foi desenvolvido para criar um fluxo automatizado entre um servidor RomM self-hosted e uma instalação local do LaunchBox.

Suporta tanto bibliotecas tradicionais de ROMs quanto jogos nativos de PC, dependendo da configuração do seu LaunchBox.

---

## Funcionalidades

### Sincronização da Biblioteca

- Botão único unificado para todas as operações
- Sincroniza plataformas e jogos diretamente do RomM
- Cria plataformas automaticamente no LaunchBox
- Mantém o status de instalado/desinstalado
- Persistência da seleção de plataformas
- Modo Admin para sync bidirecional (pull + push)
- Retomada de sincronização interrompida de onde parou
- Auto sync na inicialização com intervalo configurável
- Otimização por hash: 8000 jogos com 50 alterações = ~51 chamadas API

### Sincronização de Metadados

Preenche automaticamente os metadados do LaunchBox com dados do servidor, com prioridade configurável: **LaunchBoxMetadata > ScreenScraper > IGDB > RomM Metadata**.

Campos sincronizados: data de lançamento, máximo de jogadores, modo de jogo, vídeo (YouTube), Wikipedia, rating comunitário, classificação ESRB, sinopse, gênero, empresas, modos de jogo, LaunchBox ID.

- **KeepLocalData** = `true`: só preenche campos vazios, preserva dados existentes
- **KeepLocalData** = `false`: sobrescreve todos os campos com dados do servidor
- Screenshots são sempre bidirecionais independente desta configuração

### Download de Capa

- Baixa a capa (Box - Front) automaticamente do servidor RomM
- Só baixa se o jogo não tiver capa no LaunchBox

### Instalar, Desinstalar e Atualizar Jogos

Clique com o botão direito em qualquer jogo RomM no LaunchBox:

| Ação | Descrição |
|---|---|
| `RomM: Instalar` | Baixa e instala o jogo do servidor RomM |
| `RomM: Desinstalar` | Remove arquivos locais e marca o jogo como desinstalado |
| `RomM: Atualizar Metadados` | Atualiza os metadados do jogo a partir do servidor RomM |

### Gerenciador de Jogos

Um formulário unificado com suporte a fila de downloads:

- Fila de downloads com até 5 downloads simultâneos
- Retomada de downloads interrompidos (HTTP Range headers)
- Retry automático (5 tentativas com backoff exponencial)
- Velocidade em tempo real e tempo estimado restante
- Desinstalar jogos selecionados ou todos
- Estado persistente (downloads retomam após reinício)

### Suporte ao `_launchbox.json`

Integração avançada via `_launchbox.json` por jogo:

- Seleção personalizada de executável
- Pre-loaders e pós-loaders
- Aplicações adicionais
- Argumentos personalizados de linha de comando (variável `%romsFolder%`)
- Suporte a DLC

### Admin vs Non-Admin

| Operação | Admin | Non-Admin |
|---|---|---|
| Puxar metadados do servidor | ✅ | ✅ |
| Puxar capa do servidor | ✅ | ✅ |
| Puxar screenshots do servidor | ✅ | ✅ |
| Enviar metadados locais ao servidor | ✅ | ❌ |
| Enviar artwork local ao servidor | ✅ | ❌ |
| Enviar screenshots locais ao servidor | ✅ | ✅ |
| Enviar sessões de jogo | ✅ | ✅ |
| Auto sync na inicialização | ✅ | ✅ |

### Retomada de Sincronização Interrompida

Se uma sincronização for interrompida, o plugin salva o estado e oferece retomar na próxima vez. Rastreamento por jogo permite retomar no meio de uma plataforma.

### Outras Funcionalidades

- Internacionalização: Inglês e Português (Brasil)
- Extração automática de arquivos compactados
- Detecção de estado de instalação com persistência
- Correção da hierarquia de Parents (manipulação direta do `Parents.xml`)
- Limpeza automática de logs com retenção configurável
- Opção de re-sincronização completa
- Sincronização bidirecional de estatísticas (contagem, tempo, última data)
- Sincronização bidirecional de screenshots
- Auto-update via GitHub Releases

---

## Requisitos

- LaunchBox / BigBox
- Servidor RomM ativo
- Ambiente Windows
- Acesso de rede ao servidor RomM

---

## Instalação

### 1. Baixe o Plugin

Faça download da versão mais recente na página de [Releases do GitHub](https://github.com/phscezario/launchbox-romm-plugin/releases).

### 2. Extraia Dentro do LaunchBox

```text
LaunchBox/Plugins/RomM LaunchBox Integration
```

### 3. Configure o Plugin

Abra o `settings.json` na pasta do plugin e configure:

| Configuração | Descrição |
|---|---|
| `RommBaseUrl` | URL do servidor RomM (ex.: `http://192.168.1.100:9000`) |
| `Username` | Usuário do RomM |
| `Password` | Senha do RomM |
| `ClientApiToken` | Token de API do RomM (`rmm_...`). Tem prioridade sobre usuário/senha |
| `RomsPath` | Pasta local onde os jogos serão instalados |
| `KeepLocalData` | `true` = preserva dados existentes, `false` = sobrescreve |
| `IsAdmin` | `true` = habilita sync bidirecional (padrão: `false`) |

Ou use a tela de Configurações no LaunchBox: `RomM: Settings`

Veja a [Referência de Configuração](docs/configuration.md) para todas as opções disponíveis.

### 4. Sincronize Sua Biblioteca

Clique em `RomM: Sync` no menu do LaunchBox. O plugin conectará ao RomM, recuperará plataformas e jogos, criará plataformas faltantes, aplicará metadados e baixará capas.

---

## Fluxo Recomendado de Uso

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

## Limitações Conhecidas

- Requer conexão ativa com o servidor RomM
- Imagens de artwork muito grandes (>10MB) podem falhar no upload (limitação do servidor RomM)
- Alguns emuladores podem ainda exigir configuração manual no LaunchBox
- Bibliotecas grandes (1000+ jogos) podem levar 15-30 minutos para sincronização completa de metadados

---

## Contribuições

Contribuições são bem-vindas! Veja as [Diretrizes de Contribuição](docs/contributing.md) para:

- Convenções de commits (conventional commits)
- Estilo de código e convenções de nomenclatura
- Processo de PR e review
- Como reportar bugs

---

## Licença

GPL-3.0 License
