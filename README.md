# GONet - Unity GameObject Networking

**Production-ready multiplayer networking for Unity with auto-magical GameObject state synchronization.**

GONet is a high-performance networking library for Unity that makes multiplayer game development simple and powerful. If you need to network something and a GameObject is involved, GONet is the answer.

## Key Features

- **Auto-Magical Data Sync** - Add `[GONetAutoMagicalSync]` to any field and it syncs automatically
- **Zero-Latency Client Spawning** - GONetId batch system eliminates spawn delay
- **Enhanced RPC System** - Async/await support, server validation, persistent delivery
- **Scene Management** - Server-authoritative with late-joiner synchronization
- **Unity Addressables** - Full support for scenes and runtime prefab spawning
- **Adaptive Congestion** - Auto-scaling packet pools handle burst traffic
- **Velocity-Augmented Sync** - 90%+ bandwidth reduction for slow-moving objects
- **Auto-Detection** - Zero config local testing (first instance = server)

## Quick Start

```csharp
// 1. Add GONetParticipant to GameObjects
// 2. Mark fields to sync
[GONetAutoMagicalSync] public float health;
[GONetAutoMagicalSync] public Vector3 velocity;

// 3. Use RPCs
[ServerRpc]
async Task<ChatResult> SendChatMessage(string message) { }

[ClientRpc]
void NotifyAllClients(string message) { }

// 4. Network spawning works automatically
GameObject.Instantiate(prefab, position, rotation);
```

## Installation

1. Import GONet package from Unity Asset Store
2. Add `GONet_GlobalContext` prefab to your first scene
3. Add `GONetParticipant` component to GameObjects you want to network
4. Build executable
5. Hit Play (first instance becomes server, rest become clients)

## System Requirements

- **Unity:** 2022.3.62f3 LTS or later (tested up to Unity 6)
- **Platforms:** Windows, Mac, Linux, iOS, Android
- **API Level:** .NET Framework or .NET Standard 2.1
- **Architecture:** UDP-only, client-server or LAN

## Documentation & Support

- **Discord:** https://discord.gg/NMeheRHQgd (fastest response)
- **Website:** https://galoreinteractive.com/gonet
- **Email:** contactus@galoreinteractive.com
- **Tutorial Video:** https://www.youtube.com/watch?v=fs1flIi35JM

## What's New in v1.5

- GONetId Batch System (zero spawn latency)
- Enhanced RPC System (async/await, validation)
- Scene Management (server-authoritative)
- Unity Addressables Support (scenes + prefabs)
- Adaptive Congestion Management
- Velocity-Augmented Sync (bandwidth savings)
- Auto-Detection for Development

See [Release Notes](.claude/GONET_V1.5_RELEASE_NOTES.md) for full details.

## License

Copyright © 2025 Galore Interactive LLC. All rights reserved.

Full source code included with Unity Asset Store purchase.

---

**Making Unity Multiplayer Simple, Powerful, and Production-Ready**
