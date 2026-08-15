This is a management gateway of my project zomboid server.

minimal api project, discord as background service.

Feature overview:
- discord slash commands via the INTERACTION framework.
- allowlist of ids and channels
- rcon to zomboid rcon
- otel export

use discord .net library. https://github.com/discord-net/Discord.net and https://docs.discordnet.dev/index.html

1 admin (in config) should be able to add/remove to allowlist
As storage, either sqllite or just a couple of .txt files. 

------
commands:
/start - starts the zomboid server, if its offline
/stop - stops the zomboid server, if its online
/status - gets the status of the zomboid process(in systemd)
/players - nr of players online / player list

for admin:
/allow{id, command}- ads a discord user to the allowlist for this command
/disallow{id, command} - removes a discord user from the allowlist for this command
/channel{add/remove, channelid}-adds or removes a channel allowlist of where commands can be run.
