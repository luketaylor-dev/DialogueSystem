# DialogueSystem

A modular Unity dialogue system that can be easily integrated into any Unity project as a git submodule.

## Structure

```
Assets/DialogueSystem/
├── Scripts/           # Runtime scripts
│   ├── Canvas/        # UI-related scripts
│   ├── Data/          # Data structures and containers
│   ├── ScriptableObjects/ # ScriptableObject definitions
│   ├── DSDialogue.cs  # Main dialogue controller
│   └── DSDialogueActionHandler.cs # Action handling system
├── Editor/            # Editor-only scripts (not included in builds)
│   ├── Data/          # Editor data management
│   ├── Elements/      # Graph view elements
│   ├── Graphs/        # Dialogue graph assets
│   ├── Inspectors/    # Custom inspectors
│   ├── Resources/     # UI stylesheets and editor resources
│   ├── Utilities/     # Editor utilities
│   └── Windows/       # Editor windows
├── Enums/             # Shared enums and constants
├── Utilities/         # Runtime utility classes
├── Prefabs/           # Prefabs for dialogue UI
└── Dialogues/         # Dialogue data and assets
```

## Installation

### As Git Submodule

1. Add this as a submodule to your Unity project:

```bash
git submodule add <repository-url> Assets/DialogueSystem
```

2. Initialize and update the submodule:

```bash
git submodule init
git submodule update
```

### Manual Installation

1. Copy the entire `Assets/DialogueSystem/` folder to your Unity project's Assets folder
2. Unity will automatically compile the scripts and recognize the Editor folder
3. All required resources are included in the module - no additional setup needed

## Usage

### Basic Setup

1. **Import the DialogueSystem prefab** from `Assets/DialogueSystem/Prefabs/DialogueSystem.prefab`
2. **Create dialogue graphs** using the Dialogue Graph window (Window > Dialogue System > Dialogue Graph)
3. **Set up dialogue data** in the Dialogues folder

### Key Components

- **DSDialogue**: Main controller for dialogue flow
- **DSDialogueActionHandler**: Handles custom actions during dialogue
- **Dialogue Graph**: Visual editor for creating dialogue trees

### Editor Tools

- **Dialogue Graph Window**: Visual dialogue tree editor
- **Custom Inspectors**: Enhanced property drawers for dialogue data
- **Graph Utilities**: Tools for managing dialogue graphs

## Features

- Visual dialogue graph editor
- Support for dialogue groups and global dialogues
- Custom action system
- Modular architecture for easy integration
- Editor-only scripts automatically excluded from builds

## Dependencies

This system uses Unity's built-in features and doesn't require additional packages beyond what's included in Unity.

## License

[Add your license information here]

## Contributing

[Add contribution guidelines if applicable]
