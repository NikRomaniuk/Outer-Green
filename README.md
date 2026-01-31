# 🌱 Outer Green

> Life in the silence of the void. A 2.5D procedural botany simulator about cultivating a resilient ecosystem on a lonely space station

## 🧭 Overview

A Unity-based botanical simulation game set on an isolated space station. Players cultivate and breed procedurally generated fictional plants with the ultimate goal of creating a self-sustaining ecosystem capable of thriving in the harsh vacuum of space. The station operates far from other human outposts, with limited material exchange possible only at significant cost

The game centers on developing resilient plant species that can support each other in extreme conditions, combining creative breeding mechanics with strategic resource management

## 🎮 Core Gameplay

### 🧬 Procedural Plant Generation
Plants are built from randomly generated segments with attachment points. Each segment can connect to others based on available attachment points, creating unique plant structures. Growth boundaries are enforced by invisible limits that deactivate attachment points outside the allowed area

### 🌬️ Growth Manipulation
- In-Growth Modifiers:
    - Apply directional wind to influence growth patterns
    - Remove or reduce lighting to slow/pause growth, buying time to gather necessary components
- Before-Growth Modifiers:
    - Apply solutions and extracts (harvested from other plants) to mutate existing and future segments, affecting lifecycle duration, yield, and cosmetic appearance

### 📦 Trading
Limited material exchange between stations. Shipping costs scale with cargo weight, making seed trading more economical than shipping mature plants or heavy materials. Primary income comes from quests rather than trade

### 🗺️ Quest System
Main income source. Neighbors request resources or plants, some time-limited. Mix of procedurally generated repeatable quests and unique quest chains that unlock special rewards and content. Available quest pool depends on reputation and development stage

### 🚀 Expeditions
Dungeon-like planetary exploration using the player's robot. Robot equip grown plants as tools (plant-lamps, plant-bombs, etc.) to navigate puzzle-based environments, overcome obstacles, and harvest rare resources for advanced cultivation and the end-game goal

## 🛠️ Technical Details

**Engine**: Unity  
**Platform**: PC only  
**Rendering**: 2.5D hybrid approach
- Pixel art for interactive elements: plants, character, liquids, resources, UI
- 3D low-poly for environment: station, dungeons, containers, decorations

**Core Systems**:
- Procedural plant generation with segment-based architecture
- Dynamic attachment point system with boundary constraints
- Real-time growth simulation with environmental factors
- Mutation genetics system with traits (laws)

## 📁 Project Structure

```
Assets/
├── _Project/              # Core game content
│   ├── Art/               # Sprites, textures, materials, shaders and atlases
│   ├── Audio/             # Music, SFX, Ambient audio, Mixers
│   ├── Data/              # ScriptableObjects, JSON definitions, data tables
│   ├── Prefabs/           # Reusable prefabs
│   ├── Scenes/            # Unity Scenes
│   ├── Scripts/           # All game code
│   └── Settings/          # Project settings
├── Plugins/               # Third-party integrations
├── Samples/               # Example objects and prototypes
└── TutorialInfo/          # Documentation and guides
```

## ✨ Features

### ✅ Implemented
- Basic Unity project setup
- 2.5D View
- "Billboarding" Sprites
- **Procedural plant growth system**
  - Segment-based architecture with attachment points
  - Two-phase growth cycle: Logic → Visuals
  - Intelligent collision detection
  - A lots of controllable random variation
  - Global direction influence
  
- **Time management system**
  - Cycle-based simulation with persistence
  - Absence simulation (growth while offline) (probably won't last for long, meh)
  - Configurable growth rates

### 🟨 Coming Soon
- Growth boundaries
- Plants special traits (laws)
- Plants mutation
- Market trading
- Space station environment
- Quest system
- Expeditions
- Materials management
- Save/load functionality

## 🎨 Design

**Atmosphere**: Solitude and tranquility define the experience. No living creatures inhabit the explored planets. The protagonist exists among robots and distant communications from neighboring stations. This isolation emphasizes plants as the true protagonists—living companions in an otherwise empty void

**Visual Style**: Combines nostalgic pixel art with clean low-poly geometry, creating a distinct aesthetic that highlights interactive elements while maintaining environmental cohesion

## 📌 Status

🚧 **Early Development** - Core mechanics design phase

---

👥 **Development Team**: Another Living Worlds (ALW)
📅 **Started**: January 2026
