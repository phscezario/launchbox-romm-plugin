# 🚀 LaunchBox RomM Plugin

> Seamless integration between **RomM Server** and **LaunchBox / BigBox**  
> Integração completa entre **RomM Server** e **LaunchBox / BigBox**

---

# 🇺🇸 English

## 📌 Overview

LaunchBox RomM Plugin allows you to sync, install and uninstall games directly from your RomM server inside LaunchBox.

It was designed to provide a clean and automated workflow for managing ROM collections hosted in RomM while keeping LaunchBox as the frontend.

---

## ✨ Features

- ✔ Sync platforms from RomM
- ✔ Sync games from RomM
- ✔ Automatically create LaunchBox platforms
- ✔ Add custom metadata fields per game
- ✔ Add **Install (RomM)** and **Uninstall (RomM)** actions
- ✔ Background event processing system
- ✔ ZIP extraction for folder-based games
- ✔ `_launchbox.json` support for advanced configuration
- ✔ CLI without visible console window
- ✔ Automatic ApplicationPath management
- ✔ DLC support (when configured)
- ✔ Update RomM server metadata using LaunchBox metadata
- ✔ Delete all RomM server metadata

---

## ⚙️ Installation

1. Download the latest release.
2. Extract the plugin folder into:

```
LaunchBox/Plugins/RomM LaunchBox Integration
```

3. Edit the `settings.json` file inside the plugin folder or you can edit the settings from the LaunchBox UI:

```json
{
  "RommBaseUrl": "http://your-romm-server:9000",
  "Username": "your_username",
  "Password": "your_password",
  "RomsPath": "D:\\LaunchBox\\Games"
}
```

4. Open LaunchBox.
5. Use the menu option:
   ```
   RomM: Sync roms list from server
   ```

---

## 📦 How Install / Uninstall Works

Each synced game automatically receives two additional applications:

| Action | Executable | Arguments |
|--------|------------|-----------|
| Install | RommPlugin.CLI.exe | install <gameId> |
| Uninstall | RommPlugin.CLI.exe | uninstall <gameId> |

When installing:

- The game is downloaded from RomM
- ZIP files are extracted if needed
- Folder hierarchy is corrected automatically
- `_launchbox.json` (if present) is parsed
- ApplicationPath is set automatically

When uninstalling:

- Files are removed
- LaunchBox application path is cleared
- Game marked as not installed

---

## 📄 _launchbox.json Support

If a game contains a `_launchbox.json`, it may define:

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
      "Name": "Loader",
      "Path": "Loader.exe",
      "CommandLine": "\"%romsFolder%\""
      "WaitToExit": false
    }
  ],
  "PosLoaders": [
    {
      "Name": "Loader",
      "Path": "Loader.exe",
      "CommandLine": "\"%romsFolder%\""
    }
  ]
}
```

This allows advanced configuration for complex games.

---

## 🔁 Event Processing

The plugin uses a `romm.sync` file to track pending install/uninstall events.

On processing:

- The plugin updates installed status
- Cleans up finished events
- Removes the sync file when empty

---

## 🤝 Contributing

Pull requests are welcome.

If you find a bug or want to suggest improvements:

- Open an Issue
- Submit a PR
- Provide detailed reproduction steps

---

## 📜 License

GPL-3.0 License

---

---

# 🇧🇷 Português

## 📌 Visão Geral

O LaunchBox RomM Plugin permite sincronizar, instalar e desinstalar jogos diretamente do servidor RomM dentro do LaunchBox.

Ele foi projetado para oferecer um fluxo automatizado e limpo para gerenciar coleções hospedadas no RomM utilizando o LaunchBox como frontend.

---

## ✨ Funcionalidades

- ✔ Sincronização de plataformas
- ✔ Sincronização de jogos
- ✔ Criação automática de plataformas
- ✔ Campos personalizados por jogo
- ✔ Ações de **Install (RomM)** e **Uninstall (RomM)**
- ✔ Sistema de processamento de eventos
- ✔ Extração automática de ZIP
- ✔ Suporte a `_launchbox.json`
- ✔ CLI sem janela de console
- ✔ Atualização automática do ApplicationPath
- ✔ Suporte a DLC (quando configurado)
- ✔ Atualiza metadados do servidor RomM usando metadados do LaunchBox
- ✔ Deleta todos os metadados do servidor RomM

---

## ⚙️ Instalação

1. Baixe a última release.
2. Extraia para:

```
LaunchBox/Plugins/RomM LaunchBox Integration
```

3. Edite o `settings.json``ou você pode usar a opção dentra da UI do LaunchBox:

```json
{
  "RommBaseUrl": "http://seu-romm:9000",
  "Username": "usuario",
  "Password": "senha",
  "RomsPath": "D:\\Jogos"
}
```

4. Abra o LaunchBox.
5. Execute:
   ```
   RomM: Sync roms list from server
   ```

---

## 📦 Como Funciona a Instalação

Ao sincronizar, cada jogo recebe:

- Install (RomM)
- Uninstall (RomM)

Instalar:

- Baixa do servidor
- Extrai ZIP
- Ajusta hierarquia
- Configura via `_launchbox.json`
- Define ApplicationPath

Desinstalar:

- Remove arquivos
- Limpa ApplicationPath
- Marca como não instalado

---

## 🔁 Sistema de Eventos

O arquivo `romm.sync` controla eventos pendentes.

Após processamento:

- Atualiza status
- Remove eventos concluídos
- Deleta o arquivo se vazio

---

## 🤝 Contribuições

Contribuições são bem-vindas!

Abra uma issue ou envie um pull request.

---

## 📜 Licença

GPL-3.0