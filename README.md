# Zombie Gateway

Management gateway for a Project Zomboid server with:

- ASP.NET Core minimal API host
- Discord.Net slash command worker
- Per-command user allowlist and channel allowlist
- local or remote systemd control hooks (`start`, `stop`, `status`)
- RCON players command hook
- OpenTelemetry tracing export

## Bootstrap

```bash
dotnet restore
dotnet run --project ZombieGateway/ZombieGateway.csproj
```

### Remote management API (run on server host)

```bash
dotnet run --project ZombieManagementApi/ZombieManagementApi.csproj
```

## Configure

Edit `ZombieGateway/appsettings.json`:

- `Discord.BotToken`: Discord bot token
- `Discord.AdminUserId`: single admin user id
- `Discord.GuildId`: guild id for fast slash command registration
- `Authorization.StoragePath`: allowlist file path
- `ManagementApi.BaseUrl`: optional base URL for remote management API (for example `http://192.168.1.50:5005`)
- `ManagementApi.ApiKey`: shared API key for remote management API
- `Zomboid.SystemdServiceName`: systemd unit name
- `Zomboid.RconHost` / `RconPort` / `RconPassword`: RCON settings
- `OpenTelemetry.Otlp.Endpoint`: optional OTLP collector endpoint

If `ManagementApi.BaseUrl` is set, the gateway routes `/start`, `/stop`, `/status`, and `/players` through that API instead of local systemd/RCON.

Edit `ZombieManagementApi/appsettings.json` on the server host:

- `ManagementAuth.ApiKey`: shared API key expected in `X-Api-Key`
- `Zomboid.*`: local systemd/RCON settings on the server machine

Exposed authenticated endpoints:

- `GET /server/status`
- `POST /server/start`
- `POST /server/stop`
- `GET /server/players`

## Slash commands

User commands:

- `/start`
- `/stop`
- `/status`
- `/players`

Admin commands:

- `/allow id command`
- `/disallow id command`
- `/channel action channelid`
