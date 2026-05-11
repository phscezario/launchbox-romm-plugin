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
      "Path": "Launcher.exe",
      "CommandLine": "\"%romsFolder%\"",
      "WaitToExit": false
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

Available actions:

| Action | Description |
|---|---|
| `Update RomM Metadata` | Sends LaunchBox metadata to RomM |
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

Download the latest release from:

- GitHub Repository
- LaunchBox Community Page

---

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

---

### 3. Configure the Plugin

You can configure the plugin using:

- `settings.json`
- LaunchBox plugin interface

Example:

```json
{
  "RommBaseUrl": "http://your-romm-server:9000",
  "Username": "your_username",
  "Password": "your_password",
  "RomsPath": "D:\\LaunchBox\\Games"
}
```

| Setting | Description |
|---|---|
| `RommBaseUrl` | RomM server URL |
| `Username` | RomM username |
| `Password` | RomM password |
| `RomsPath` | Local installation folder |

---

### 4. Open LaunchBox

Start LaunchBox normally.

The plugin menus will become available automatically.

---

### 5. Synchronize Your Library

Use the menu option:

```text
RomM: Sync roms list from server
```

The plugin will:

- Connect to RomM
- Retrieve platforms
- Retrieve games
- Automatically create missing platforms
- Import games into LaunchBox

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

## 📸 Screenshots

Recommended screenshots to include:

- Plugin settings
- Synchronization menu
- Install / uninstall actions
- Installed game example
- Metadata synchronization actions

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

---

# 🇧🇷 Português

## Visão Geral

O **LaunchBox RomM Plugin** conecta sua instalação local do LaunchBox diretamente a um servidor RomM, permitindo sincronizar, instalar, gerenciar e executar jogos de forma integrada.

O plugin foi desenvolvido para criar um fluxo automatizado entre um servidor RomM self-hosted e uma instalação local do LaunchBox.

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

### ⚙️ Suporte Avançado ao `_launchbox.json`

Os jogos podem conter um arquivo `_launchbox.json` para integração avançada com o LaunchBox.

Os recursos suportados incluem:

- Seleção personalizada de executável
- Aplicações adicionais
- Pre-loaders
- Pós-loaders
- Argumentos personalizados de linha de comando
- Suporte a DLC

Exemplo:

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
      "Path": "Launcher.exe",
      "CommandLine": "\"%romsFolder%\"",
      "WaitToExit": false
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

### 🔹 PreLoaders

Os `PreLoaders` são executados antes do jogo principal iniciar.

Eles podem ser utilizados para:

- Launchers customizados
- Aplicações de setup
- Montagem de discos virtuais
- Inicialização de dependências
- Ferramentas de compatibilidade

Campos suportados:

| Campo | Descrição |
|---|---|
| `Name` | Nome exibido no LaunchBox |
| `Path` | Caminho do executável |
| `CommandLine` | Argumentos personalizados |
| `WaitToExit` | Aguarda finalizar antes de iniciar o jogo |

---

### 🔹 PosLoaders

Os `PosLoaders` são executados após o encerramento do jogo principal.

Eles podem ser utilizados para:

- Limpeza de arquivos temporários
- Desmontagem de discos virtuais
- Encerramento de processos auxiliares
- Pós-processamento
- Scripts automáticos

Campos suportados:

| Campo | Descrição |
|---|---|
| `Name` | Nome exibido no LaunchBox |
| `Path` | Caminho do executável |
| `CommandLine` | Argumentos personalizados |

---

### 🔹 AdditionalApplications

As `AdditionalApplications` permitem adicionar ferramentas extras diretamente ao jogo dentro do LaunchBox.

Exemplos:

- Configuradores
- Launchers alternativos
- Editors
- Setup tools
- Trainers
- Ferramentas auxiliares

Campos suportados:

| Campo | Descrição |
|---|---|
| `Name` | Nome da aplicação |
| `Path` | Caminho do executável |

---

### 🔹 Variáveis Suportadas

O plugin suporta variáveis dinâmicas dentro de `CommandLine`.

| Variável | Descrição |
|---|---|
| `%romsFolder%` | Pasta onde o jogo foi instalado |

Isso permite criar configurações portáveis e automatizadas.

---

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

Ações disponíveis:

| Ação | Descrição |
|---|---|
| `Update RomM Metadata` | Envia os metadados do LaunchBox para o RomM |
| `Clear RomM Metadata` | Remove os metadados sincronizados do RomM |

### Fluxo Recomendado

1. Organize os metadados dentro do LaunchBox
2. Sincronize sua biblioteca do RomM
3. Execute `Update RomM Metadata`
4. O RomM receberá automaticamente todos os metadados do LaunchBox

Isso mantém os metadados do LaunchBox e do RomM totalmente sincronizados.

---

### 🧠 Recursos Internos de Processamento

- Sistema de fila de eventos em background
- Controle de eventos através do `romm.sync`
- Execução oculta de CLI (sem popup de console)
- Limpeza automática de operações concluídas

---

## 📦 Requisitos

- LaunchBox / BigBox
- Servidor RomM ativo
- Ambiente Windows
- Acesso de rede ao servidor RomM

---

## 📦 Instalação

### 1. Baixe o Plugin

Faça download da versão mais recente através do:

- GitHub Repository
- Página da Comunidade LaunchBox

---

### 2. Extraia Dentro do LaunchBox

Extraia a pasta do plugin em:

```text
LaunchBox/Plugins/RomM LaunchBox Integration
```

Estrutura esperada:

```text
LaunchBox
 └── Plugins
      └── RomM LaunchBox Integration
```

---

### 3. Configure o Plugin

Você pode configurar o plugin utilizando:

- `settings.json`
- Interface do plugin dentro do LaunchBox

Exemplo:

```json
{
  "RommBaseUrl": "http://seu-romm:9000",
  "Username": "usuario",
  "Password": "senha",
  "RomsPath": "D:\\Jogos"
}
```

| Configuração | Descrição |
|---|---|
| `RommBaseUrl` | URL do servidor RomM |
| `Username` | Usuário do RomM |
| `Password` | Senha do RomM |
| `RomsPath` | Pasta local de instalação |

---

### 4. Abra o LaunchBox

Abra o LaunchBox normalmente.

Os menus do plugin ficarão disponíveis automaticamente.

---

### 5. Sincronize Sua Biblioteca

Utilize a opção do menu:

```text
RomM: Sync roms list from server
```

O plugin irá:

- Conectar ao RomM
- Buscar plataformas
- Buscar jogos
- Criar plataformas automaticamente
- Importar jogos para o LaunchBox

---

## 🧠 Sistema Interno de Sincronização

O plugin utiliza internamente um arquivo `romm.sync` para processar eventos pendentes.

Esse sistema gerencia:

- Eventos de instalação
- Eventos de desinstalação
- Sincronização de metadados
- Limpeza automática
- Execução em background

Isso garante que o LaunchBox e o RomM permaneçam sincronizados com segurança.

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

## 📸 Screenshots

Screenshots recomendadas:

- Tela de configurações do plugin
- Menu de sincronização
- Ações de instalação/desinstalação
- Exemplo de jogo instalado
- Tela de sincronização de metadados

---

## 🤝 Contribuições

Contribuições são bem-vindas.

Caso encontre bugs ou queira melhorias:

- Abra uma issue
- Envie um pull request
- Inclua passos para reprodução sempre que possível

---

## 📄 Licença

GPL-3.0 License
