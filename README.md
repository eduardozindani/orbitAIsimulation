# Orbit AI Simulation

An immersive VR/AR educational platform that teaches space missions through AI-powered conversational interactions. Built with Unity 6 and Meta Quest integration.

![Unity Version](https://img.shields.io/badge/Unity-6000.0.47f1-black)
![Meta XR SDK](https://img.shields.io/badge/Meta%20XR%20SDK-78.0.0-blue)
![Platform](https://img.shields.io/badge/Platform-Meta%20Quest%203%2FPro-green)

## Overview

Orbit AI Simulation allows users to explore real space missions (ISS, Voyager, Hubble) in virtual reality, converse with AI mission specialists, and manipulate orbital mechanics in real-time through natural language conversation.

## Demo
Here is a 8 minute demo of the functionality. https://youtu.be/Y9PWMZA4WV8

### Key Features

- **AI-Powered Conversations**: Interact with mission specialists powered by OpenAI GPT-4
- **Real-time Orbital Physics**: Physics-based calculations using Kepler's laws
- **Text-to-Speech**: Natural voice responses via ElevenLabs API
- **Tool-Based AI Commands**: Adjust orbits, control time, and navigate missions through conversation
- **VR/AR Support**: Fully integrated with Meta Quest 3/Pro, including pass-through AR mode
- **Multi-Mission Experience**: Explore ISS, Voyager, and Hubble missions with unique specialists

## Table of Contents

- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [API Configuration](#api-configuration)
- [Building the Project](#building-the-project)
- [Project Structure](#project-structure)
- [Usage](#usage)
- [Development](#development)
- [Contributing](#contributing)
- [License](#license)
- [Acknowledgments](#acknowledgments)

## Prerequisites

### Required Software

- **Unity 6000.0.47f1** (or compatible version)
- **Meta Quest 3 or Quest Pro** (for VR testing)
- **Visual Studio 2022** or **Rider** (for C# development)
- **Git** (for version control)

### Required API Accounts

- **OpenAI Account** - [Sign up here](https://platform.openai.com/signup)
- **ElevenLabs Account** - [Sign up here](https://elevenlabs.io/sign-up)

Both services offer free tiers suitable for development and testing.

## Installation

### 1. Clone the Repository

```bash
git clone https://github.com/ezindani/orbitAIsimulation.git
cd orbitAIsimulation
```

### 2. Open in Unity

1. Open **Unity Hub**
2. Click **Add** → **Add project from disk**
3. Navigate to the cloned repository folder
4. Select the folder and click **Open**
5. Unity will import all packages (this may take 5-10 minutes on first open)

### 3. Install Dependencies

Unity will automatically install required packages via Package Manager:
- Meta XR SDK (v78.0.0)
- Universal Render Pipeline (URP)
- XR Interaction Toolkit
- Newtonsoft JSON

If any packages fail to install, go to **Window → Package Manager** and install them manually.

## API Configuration

**IMPORTANT**: You must configure API keys before running the project.

### Quick Setup (Easiest)

Run the setup script to automatically create configuration files from templates:

**macOS/Linux:**
```bash
./setup-config.sh
```

**Windows:**
```bash
setup-config.bat
```

Then add your API keys to the created `.env` file or Unity asset files.

### Method 1: Environment Variables (Recommended)

1. Copy the example environment file:
   ```bash
   cp .env.example .env
   ```

2. Edit `.env` and add your API keys:
   ```bash
   OPENAI_API_KEY=sk-proj-your_actual_openai_key_here
   ELEVENLABS_API_KEY=sk_your_actual_elevenlabs_key_here
   ```

3. **On macOS/Linux**, load environment variables before launching Unity:
   ```bash
   export OPENAI_API_KEY="your_key_here"
   export ELEVENLABS_API_KEY="your_key_here"
   open -a Unity\ Hub
   ```

4. **On Windows**, set environment variables in System Properties or PowerShell:
   ```powershell
   $env:OPENAI_API_KEY="your_key_here"
   $env:ELEVENLABS_API_KEY="your_key_here"
   ```

### Method 2: Unity ScriptableObject Assets (Alternative)

1. Copy template files to create your local configuration:
   ```bash
   cp Assets/Resources/OpenAISettings.asset.template Assets/Resources/OpenAISettings.asset
   cp Assets/Resources/Agents/ElevenLabsSettings.asset.template Assets/Resources/Agents/ElevenLabsSettings.asset
   cp Assets/AnastasiaVoiceSettings.asset.template Assets/AnastasiaVoiceSettings.asset
   cp Assets/HubbleVoiceSettings.asset.template Assets/HubbleVoiceSettings.asset
   cp Assets/KarlVoiceSettings.asset.template Assets/KarlVoiceSettings.asset
   ```

2. In Unity Editor, navigate to each asset and replace `YOUR_API_KEY_HERE` with your actual keys:
   - `Assets/Resources/OpenAISettings.asset` - Add your OpenAI key
   - `Assets/Resources/Agents/ElevenLabsSettings.asset` - Add your ElevenLabs key
   - `Assets/AnastasiaVoiceSettings.asset` - Add your ElevenLabs key
   - `Assets/HubbleVoiceSettings.asset` - Add your ElevenLabs key
   - `Assets/KarlVoiceSettings.asset` - Add your ElevenLabs key

**⚠️ WARNING**: Never commit these `.asset` files with real API keys! They are gitignored by default.

### Getting API Keys

#### OpenAI API Key
1. Go to [OpenAI Platform](https://platform.openai.com/api-keys)
2. Sign in or create an account
3. Click **Create new secret key**
4. Copy the key (starts with `sk-proj-...`)
5. Add billing information if required

#### ElevenLabs API Key
1. Go to [ElevenLabs Settings](https://elevenlabs.io/app/settings/api-keys)
2. Sign in or create an account
3. Navigate to **API Keys** tab
4. Click **Generate API Key**
5. Copy the key (starts with `sk_...`)

## Building the Project

### For Meta Quest (Android)

1. Connect your Quest headset via USB
2. Enable **Developer Mode** on your Quest
3. In Unity, go to **File → Build Settings**
4. Select **Android** as platform
5. Click **Switch Platform** (if not already Android)
6. Click **Build and Run**

### For PC/Mac (Development Testing)

1. In Unity, go to **File → Build Settings**
2. Select **Windows, Mac, Linux** as platform
3. Click **Build and Run**

**Note**: PC builds won't have VR features unless you have a compatible VR headset connected.

## Project Structure

```
orbitAIsimulation/
├── Assets/
│   ├── Scripts/                  # C# source code
│   │   ├── AI/                   # AI conversation system
│   │   ├── Core/                 # Core managers & config
│   │   ├── Orbital/              # Orbital mechanics
│   │   ├── Simulation/           # Time & physics
│   │   ├── Camera/               # Camera systems
│   │   ├── UI/                   # User interface
│   │   └── Scenes/               # Scene-specific scripts
│   │
│   ├── Scenes/                   # Unity scenes
│   │   ├── Hub.unity             # Mission selection hub
│   │   ├── ISS.unity             # ISS mission
│   │   ├── Voyager.unity         # Voyager mission
│   │   └── Hubble.unity          # Hubble mission
│   │
│   ├── Prefabs/                  # Reusable game objects
│   ├── Models/                   # 3D models (ISS, Voyager, Hubble)
│   ├── Audio/                    # Sound effects and music
│   ├── Resources/                # Runtime-loaded assets
│   ├── config/                   # Mission configurations
│   └── Settings/                 # URP settings
│
├── ProjectSettings/              # Unity project settings
├── Packages/                     # Package dependencies
├── Relatorio/                    # Academic thesis (Portuguese)
├── .gitignore                    # Git ignore rules
├── .env.example                  # Environment variable template
├── README.md                     # This file
└── LICENSE                       # ITA License

```

### Key Components

- **PromptConsole.cs**: Main AI orchestrator handling user input and OpenAI integration
- **OpenAIClient.cs**: API client for OpenAI Responses API
- **ElevenLabsClient.cs**: Text-to-speech and speech-to-text client
- **OrbitController.cs**: Bridge between AI commands and orbital physics
- **Orbit.cs**: Kepler's laws implementation
- **MissionContext.cs**: Persistent state across scenes
- **SceneTransitionManager.cs**: Scene navigation with smooth transitions

## Usage

### In-Editor Testing (Unity Editor)

1. Open the **Hub** scene: `Assets/Scenes/Hub.unity`
2. Click the **Play** button
3. Use keyboard input to interact with the AI:
   - Type questions or commands
   - Press **Enter** to submit
   - Press **Shift+Enter** for new line

### On Meta Quest

1. Build and deploy to your Quest headset
2. Launch the app from **Unknown Sources** in your library
3. Use the **A button** on your right controller to toggle voice input
4. Speak naturally to the AI mission specialists
5. Use hand tracking or controllers to interact with UI

### Example Interactions

```
You: "What is the current altitude of the ISS?"
AI: "The ISS is currently orbiting at approximately 420 kilometers altitude..."

You: "Increase the altitude to 500 km"
AI: *Adjusts orbit* "I've increased the orbital altitude to 500 km. The new orbital period is..."

You: "Take me to the Voyager mission"
AI: *Transitions to Voyager scene* "Welcome to the Voyager mission..."
```

## Development

### Adding New Missions

1. Create a new scene in `Assets/Scenes/`
2. Create a `MissionConfig` ScriptableObject in `Assets/config/`
3. Configure mission parameters (orbit, specialist personality, etc.)
4. Add mission to `MissionRegistry.cs`
5. Create specialist voice settings asset

### Modifying AI Prompts

Edit prompt templates in:
- `Assets/config/PromptSettings.asset` (centralized prompts)
- Or directly in script files: `SystemPrompt.cs`, `SpecialistPrompt.cs`, etc.

### Testing Without API Keys

The project will run without valid API keys, but AI features will be disabled. You'll see warning messages in the console.

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Quick Start for Contributors

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature-name`
3. Make your changes
4. Test thoroughly (especially in VR!)
5. Commit with clear messages: `git commit -m 'Add feature description'`
6. Push to your fork: `git push origin feature/your-feature-name`
7. Open a Pull Request

### Code Style

- Follow Unity C# conventions
- Use meaningful variable names
- Comment complex logic
- Document public APIs with XML doc comments

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

Developed as part of academic research at **Instituto Tecnológico de Aeronáutica (ITA)**, Brazil.

## Acknowledgments

### APIs & Services
- **OpenAI** for GPT-4 conversational AI
- **ElevenLabs** for text-to-speech and speech recognition
- **Meta** for Quest VR platform and XR SDK

### 3D Models
- **NASA** for ISS, Voyager, and Hubble 3D models (public domain)

### Academic Institution
- **Instituto Tecnológico de Aeronáutica (ITA)** - Brazil's premier aerospace engineering institute

### Contributors
- Eduardo Zindani - Lead Developer & Researcher

## Support

For issues, questions, or suggestions:
- Open an issue on [GitHub Issues](https://github.com/ezindani/orbitAIsimulation/issues)
- Contact: emzindani@gmail.com

## Roadmap

- [ ] Add more space missions (Mars rovers, Moon missions)
- [ ] Implement multiplayer collaboration
- [ ] Add educational quiz mode
- [ ] Support additional VR platforms (PSVR2, Index)
- [ ] Offline mode with pre-recorded responses
- [ ] Mission progress tracking and achievements

## Citation

If you use this project in academic work, please cite:

```bibtex
@mastersthesis{zindani2025orbit,
  title={Orbit AI Simulation: An Immersive Educational Platform for Space Mission Learning},
  author={Zindani, Eduardo},
  year={2025},
  school={Instituto Tecnológico de Aeronáutica (ITA)},
  address={São José dos Campos, Brazil}
}
```

---

**Instituto Tecnológico de Aeronáutica (ITA) - São José dos Campos, Brazil**
