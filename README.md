# Zombie Gateway

A Discord bot + management API for controlling a Project Zomboid dedicated server.

## Architecture

The gateway **always** routes commands through `ZombieManagementApi`. There is no local systemd/RCON fallback — `ManagementApi:BaseUrl` is required. If it is not set, the gateway will fail to start with a clear error message.

`ZombieManagementApi` must run on the game server host (the machine where the Zomboid process and systemd unit live).

```
Discord ──► ZombieGateway ──(HTTP+ApiKey)──► ZombieManagementApi ──► systemd / RCON
                                                   (game server host)
```

## Projects

| Project | Purpose |
|---------|---------|
| `ZombieGateway` | Discord bot + HTTP host. Runs anywhere. |
| `ZombieManagementApi` | Minimal API with API-key auth. Runs on the game server host. |
| `ZombieGateway.AppHost` | .NET Aspire AppHost for local development. |
| `ZombieGateway.ServiceDefaults` | Shared OpenTelemetry + health check configuration. |

## Running with Aspire (recommended for local dev)

```bash
# Set the shared API key secret (once)
dotnet user-secrets set "Parameters:management-api-key" "your-secret-key" \
  --project ZombieGateway.AppHost

dotnet run --project ZombieGateway.AppHost
```

Aspire starts both services, wires `ManagementApi:BaseUrl` automatically, and opens the dashboard at `https://localhost:15888` with live logs, traces, and metrics.

## Running standalone

### Gateway (your machine or any host)

```bash
dotnet run --project ZombieGateway
```

### Management API (game server host)

```bash
dotnet run --project ZombieManagementApi
```

## Configuration

### ZombieGateway — `appsettings.json`

| Key | Description |
|-----|-------------|
| `Discord:BotToken` | Discord bot token |
| `Discord:AdminUserId` | Discord user ID with admin privileges |
| `Discord:GuildId` | Guild ID for slash command registration |
| `Discord:RegisterCommandsGlobally` | `true` to register commands globally (default `false`) |
| `Authorization:StoragePath` | Path to allowlist JSON file (default `Data/allowlist.json`) |
| `ManagementApi:BaseUrl` | Base URL of `ZombieManagementApi` (e.g. `http://192.168.1.50:5005`). Leave empty to use local systemd/RCON. |
| `ManagementApi:ApiKey` | API key sent to the management API in `X-Api-Key` |
| `Zomboid:SystemdServiceName` | systemd unit name (default `zomboid-server`) |
| `Zomboid:RconHost` / `RconPort` / `RconPassword` | RCON connection details |
| `Zomboid:CommandTimeoutSeconds` | Timeout for systemd/RCON calls (default `10`) |

### ZombieManagementApi — `appsettings.json` (on the game server)

| Key | Description |
|-----|-------------|
| `ManagementAuth:ApiKey` | Expected value of `X-Api-Key` header |
| `Zomboid:SystemdServiceName` | systemd unit name |
| `Zomboid:RconHost` / `RconPort` / `RconPassword` | RCON connection details |
| `Zomboid:CommandTimeoutSeconds` | Timeout for systemd/RCON calls |

### Management API endpoints

All endpoints require `X-Api-Key` header.

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/server/status` | Returns `{ isOnline, rawStatus }` |
| `POST` | `/server/start` | Starts the systemd service |
| `POST` | `/server/stop` | Stops the systemd service |
| `GET` | `/server/players` | Returns RCON `players` output |

## Discord slash commands

All commands are in the `/pz` group.

### User commands

| Command | Description | Control path |
|---------|-------------|--------------|
| `/pz start` | Starts the server | systemd `start` |
| `/pz stop` | Stops the server | systemd `stop` |
| `/pz status` | Shows 🟢/🔴 and systemd status | systemd `is-active` |
| `/pz players` | Lists connected players | RCON `players` |

### Admin commands (AdminUserId only)

| Command | Description |
|---------|-------------|
| `/pz allow <id> <command>` | Allow a Discord user ID to run a command |
| `/pz disallow <id> <command>` | Remove a user from a command's allowlist |
| `/pz channel add <channelid>` | Add a channel where commands are accepted |
| `/pz channel remove <channelid>` | Remove a channel from the allowlist |

Commands are rejected with an ephemeral message if the user or channel is not on the allowlist.

## Allowlist

The allowlist is stored as JSON at `Authorization:StoragePath`. The admin can manage it live via `/pz allow`, `/pz disallow`, and `/pz channel` — no restart required.

Each of `start`, `stop`, `status`, and `players` has an independent user list. Channel allowlist applies to all commands.

## Observability

Both services export OpenTelemetry logs, traces, and metrics via OTLP.

When running under Aspire the endpoint is injected automatically. Standalone, set:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

### Discord command metrics

| Metric | Description | Tags |
|--------|-------------|------|
| `discord.command.invocations` | Total command calls | `command`, `user.id`, `user.name` |
| `discord.command.denials` | Allowlist-denied calls | `command`, `user.id`, `user.name`, `reason` |
| `discord.command.errors` | Failed/unreachable calls | `command`, `user.id`, `user.name` |

Each command also emits a trace span (`ZombieGateway.Discord`) tagged with `command`, `user.id`, `user.name`, and result details.

## GitHub Releases

Tagged releases (`v*`) automatically build self-contained single-file binaries via GitHub Actions:

- `zombie-gateway-linux-x64`
- `zombie-gateway-linux-arm64`
- `zombie-management-api-linux-x64`
- `zombie-management-api-linux-arm64`

```bash
git tag v1.0.0
git push origin v1.0.0
```
