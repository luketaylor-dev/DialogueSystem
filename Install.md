# DialogueSystem Installation Guide

## Quick Setup

### Step 1: Add to Your Unity Project

**Option A: Git Submodule (Recommended)**

```bash
# In your Unity project root
git submodule add <dialogue-system-repo-url> Assets/DialogueSystem
git submodule init
git submodule update
```

**Option B: Manual Copy**

1. Copy the entire `Assets/DialogueSystem/` folder to your Unity project's Assets folder
2. Unity will automatically import and compile the scripts

### Step 2: Verify Installation

1. Open Unity and check the Console for any compilation errors
2. Look for "Dialogue System" in the Window menu
3. Verify the DialogueSystem prefab exists in `Assets/DialogueSystem/Prefabs/`

### Step 3: Basic Configuration

1. **Create a Dialogue Graph:**

   - Go to Window > Dialogue System > Dialogue Graph
   - Create a new dialogue graph asset

2. **Set up the Dialogue System:**

   - Drag the DialogueSystem prefab into your scene
   - Configure the dialogue graph reference in the inspector

3. **Create Your First Dialogue:**
   - Use the Dialogue Graph window to create dialogue nodes
   - Save your dialogue graph

## Troubleshooting

### Common Issues

**Compilation Errors:**

- Ensure Unity version is 2021.3 or later
- Check that all .meta files are present
- Restart Unity if scripts don't compile automatically

**Missing Menu Items:**

- Verify the Editor folder is properly recognized
- Check that editor scripts compiled successfully
- Restart Unity if menu items don't appear

**Prefab Issues:**

- Ensure all prefab dependencies are properly linked
- Check that UI elements are properly configured

### Getting Help

If you encounter issues:

1. Check the Unity Console for error messages
2. Verify all files are properly imported
3. Restart Unity and try again
4. Check the README.md for additional information

## Next Steps

After installation:

1. Read the main README.md for detailed usage instructions
2. Explore the example dialogues in the Dialogues folder
3. Try creating your own dialogue graph
4. Integrate the system with your game's UI
