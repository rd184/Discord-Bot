﻿using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NadekoBot.Services.Administration
{
    public class Ratelimiter
    {
        private readonly RatelimitService _svc;

        public class RatelimitedUser
        {
            public ulong UserId { get; set; }
            public int MessageCount { get; set; } = 0;
        }

        public ulong ChannelId { get; set; }
        public int MaxMessages { get; set; }
        public int PerSeconds { get; set; }

        public Ratelimiter(RatelimitService svc)
        {
            _svc = svc;
        }

        public CancellationTokenSource CancelSource { get; set; } = new CancellationTokenSource();

        public ConcurrentDictionary<ulong, RatelimitedUser> Users { get; set; } = new ConcurrentDictionary<ulong, RatelimitedUser>();

        public bool CheckUserRatelimit(ulong id, ulong guildId, SocketGuildUser optUser)
        {
            if ((_svc.IgnoredUsers.TryGetValue(guildId, out HashSet<ulong> ignoreUsers) && ignoreUsers.Contains(id)) ||
                (optUser != null && _svc.IgnoredRoles.TryGetValue(guildId, out HashSet<ulong> ignoreRoles) && optUser.Roles.Any(x => ignoreRoles.Contains(x.Id))))
                return false;

            var usr = Users.GetOrAdd(id, (key) => new RatelimitedUser() { UserId = id });
            if (usr.MessageCount >= MaxMessages)
            {
                return true;
            }
            usr.MessageCount++;
            var _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(PerSeconds * 1000, CancelSource.Token);
                }
                catch (OperationCanceledException) { }
                usr.MessageCount--;
            });
            return false;
        }
    }
}