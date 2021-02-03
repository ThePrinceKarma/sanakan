﻿#pragma warning disable 1591

using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Sanakan.Config;
using Sanakan.Extensions;
using Sanakan.Services.Executor;
using Shinden.Logger;
using Z.EntityFramework.Plus;

namespace Sanakan.Services
{
    public class Greeting
    {
        private DiscordSocketClient _client { get; set; }
        private IExecutor _executor { get; set; }
        private ILogger _logger { get; set; }
        private IConfig _config { get; set; }

        public Greeting(DiscordSocketClient client, ILogger logger, IConfig config, IExecutor exe)
        {
            _client = client;
            _logger = logger;
            _config = config;
            _executor = exe;

#if !DEBUG
            _client.LeftGuild += BotLeftGuildAsync;
            _client.UserJoined += UserJoinedAsync;
            _client.UserLeft += UserLeftAsync;
#endif
        }

        private async Task BotLeftGuildAsync(SocketGuild guild)
        {
            using (var db = new Database.GuildConfigContext(_config))
            {
                var gConfig = await db.GetGuildConfigOrCreateAsync(guild.Id);
                db.Guilds.Remove(gConfig);

                var stats = db.TimeStatuses.AsQueryable().AsSplitQuery().Where(x => x.Guild == guild.Id).ToList();
                db.TimeStatuses.RemoveRange(stats);

                await db.SaveChangesAsync();
            }

            using (var db = new Database.ManagmentContext(_config))
            {
                var mute = db.Penalties.AsQueryable().AsSplitQuery().Where(x => x.Guild == guild.Id).ToList();
                db.Penalties.RemoveRange(mute);

                await db.SaveChangesAsync();
            }
        }

        private async Task UserJoinedAsync(SocketGuildUser user)
        {
            if (user.IsBot || user.IsWebhook) return;

            if (_config.Get().BlacklistedGuilds.Any(x => x == user.Guild.Id))
                return;

            using (var db = new Database.GuildConfigContext(_config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(user.Guild.Id);
                if (config?.WelcomeMessage == null) return;
                if (config.WelcomeMessage == "off") return;

                await SendMessageAsync(ReplaceTags(user, config.WelcomeMessage), user.Guild.GetTextChannel(config.GreetingChannel));

                if (config?.WelcomeMessagePW == null) return;
                if (config.WelcomeMessagePW == "off") return;

                try
                {
                    var pw = await user.GetOrCreateDMChannelAsync();
                    await pw.SendMessageAsync(ReplaceTags(user, config.WelcomeMessagePW));
                    await pw.CloseAsync();
                }
                catch (Exception ex)
                {
                    _logger.Log($"Greeting: {ex}");
                }
            }
        }

        private async Task UserLeftAsync(SocketGuildUser user)
        {
            if (user.IsBot || user.IsWebhook) return;

            if (!_config.Get().BlacklistedGuilds.Any(x => x == user.Guild.Id))
            {
                using (var db = new Database.GuildConfigContext(_config))
                {
                    var config = await db.GetCachedGuildFullConfigAsync(user.Guild.Id);
                    if (config?.GoodbyeMessage == null) return;
                    if (config.GoodbyeMessage == "off") return;

                    await SendMessageAsync(ReplaceTags(user, config.GoodbyeMessage), user.Guild.GetTextChannel(config.GreetingChannel));
                }
            }

            var thisUser = _client.Guilds.FirstOrDefault(x => x.Id == user.Id);
            if (thisUser != null) return;

            var moveTask = new Task(() =>
            {
                using (var db = new Database.UserContext(_config))
                {
                    var duser = db.GetUserOrCreateAsync(user.Id).Result;
                    var fakeu = db.GetUserOrCreateAsync(1).Result;

                    foreach (var card in duser.GameDeck.Cards)
                    {
                        card.InCage = false;
                        card.TagList.Clear();
                        card.LastIdOwner = user.Id;
                        fakeu.GameDeck.Cards.Add(card);
                    }

                    duser.GameDeck.Cards.Clear();
                    db.Users.Remove(duser);

                    db.SaveChanges();

                    QueryCacheManager.ExpireTag(new string[] { "users" });
                }
            });

            await _executor.TryAdd(new Executable("delete user", moveTask), TimeSpan.FromSeconds(1));
        }

        private async Task SendMessageAsync(string message, ITextChannel channel)
        {
            if (channel != null) await channel.SendMessageAsync(message);
        }

        private string ReplaceTags(SocketGuildUser user, string message)
            => message.Replace("^nick", user.Nickname ?? user.Username).Replace("^mention", user.Mention);
    }
}