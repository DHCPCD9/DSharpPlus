// This file is part of the DSharpPlus project.
//
// Copyright (c) 2015 Mike Santiago
// Copyright (c) 2016-2022 DSharpPlus Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.Entities.Filters;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.Net;

namespace DSharpPlus.Test
{
    [Group("lavalink"), Description("Provides audio playback via lavalink."), Aliases("lava", "ll"), ModuleLifespan(ModuleLifespan.Singleton)]
    public class TestBotLavaCommands : BaseCommandModule
    {
        private LavalinkNodeConnection Lavalink { get; set; }
        private LavalinkGuildConnection LavalinkVoice { get; set; }
        private DiscordChannel ContextChannel { get; set; }

        [Command("connect"), Description("Connects to Lavalink")]
        public async Task ConnectAsync(CommandContext ctx, string hostname, int port, string password)
        {
            if (this.Lavalink != null)
                return;

            var lava = ctx.Client.GetLavalink();
            if (lava == null)
            {
                await ctx.RespondAsync("Lavalink is not enabled.").ConfigureAwait(false);
                return;
            }

            this.Lavalink = await lava.ConnectAsync(new LavalinkConfiguration
            {
                RestEndpoint = new ConnectionEndpoint(hostname, port, false, "/v3"),
                SocketEndpoint = new ConnectionEndpoint(hostname, port, false, "/v3/websocket"),
                Password = password
            }).ConfigureAwait(false);
            this.Lavalink.Disconnected += this.Lavalink_Disconnected;
            await ctx.RespondAsync("Connected to lavalink node.").ConfigureAwait(false);
        }

        private Task Lavalink_Disconnected(LavalinkNodeConnection ll, NodeDisconnectedEventArgs e)
        {
            if (e.IsCleanClose)
            {
                this.Lavalink = null;
                this.LavalinkVoice = null;
            }
            return Task.CompletedTask;
        }

        [Command("disconnect"), Description("Disconnects from Lavalink")]
        public async Task DisconnectAsync(CommandContext ctx)
        {
            if (this.Lavalink == null)
                return;

            var lava = ctx.Client.GetLavalink();
            if (lava == null)
            {
                await ctx.RespondAsync("Lavalink is not enabled.").ConfigureAwait(false);
                return;
            }

            await this.Lavalink.StopAsync().ConfigureAwait(false);
            this.Lavalink = null;
            await ctx.RespondAsync("Disconnected from Lavalink node.").ConfigureAwait(false);
        }

        [Command("join"), Description("Joins a voice channel.")]
        public async Task JoinAsync(CommandContext ctx, DiscordChannel chn = null)
        {
            if (this.Lavalink == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.").ConfigureAwait(false);
                return;
            }

            var vc = chn ?? ctx.Member.VoiceState.Channel;
            if (vc == null)
            {
                await ctx.RespondAsync("You are not in a voice channel or you did not specify a voice channel.").ConfigureAwait(false);
                return;
            }

            this.LavalinkVoice = await this.Lavalink.ConnectAsync(vc).ConfigureAwait(false);
            this.LavalinkVoice.PlaybackFinished += this.LavalinkVoice_PlaybackFinished;
            this.LavalinkVoice.DiscordWebSocketClosed += (s, e) => ctx.RespondAsync("discord websocket close event");
            await ctx.RespondAsync("Connected.").ConfigureAwait(false);
        }

        private async Task LavalinkVoice_PlaybackFinished(LavalinkGuildConnection ll, TrackFinishEventArgs e)
        {
            if (this.ContextChannel == null)
                return;

            await this.ContextChannel.SendMessageAsync($"Playback of {Formatter.Bold(Formatter.Sanitize(e.Track.Info.Title))} by {Formatter.Bold(Formatter.Sanitize(e.Track.Info.Author))} finished.").ConfigureAwait(false);
            this.ContextChannel = null;
        }

        [Command("leave"), Description("Leaves a voice channel.")]
        public async Task LeaveAsync(CommandContext ctx)
        {
            if (this.LavalinkVoice == null)
                return;

            await this.LavalinkVoice.DisconnectAsync().ConfigureAwait(false);
            this.LavalinkVoice = null;
            await ctx.RespondAsync("Disconnected.").ConfigureAwait(false);
        }

        [Command("play"), Description("Queues tracks for playback.")]
        public async Task PlayAsync(CommandContext ctx, [RemainingText] Uri uri)
        {
            if (this.LavalinkVoice == null)
                return;

            this.ContextChannel = ctx.Channel;

            var trackLoad = await this.Lavalink.Rest.GetTracksAsync(uri);
            var track = trackLoad.Tracks.First();
            await this.LavalinkVoice.PlayAsync(track);

            await ctx.RespondAsync($"Now playing: {Formatter.Bold(Formatter.Sanitize(track.Info.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Info.Author))}.").ConfigureAwait(false);
        }

        [Command("play_file"), Description("Queues tracks for playback.")]
        public async Task PlayFileAsync(CommandContext ctx, [RemainingText] string path)
        {
            if (this.LavalinkVoice == null)
                return;

            var trackLoad = await this.Lavalink.Rest.GetTracksAsync(new FileInfo(path)).ConfigureAwait(false);
            var track = trackLoad.Tracks.First();
            await this.LavalinkVoice.PlayAsync(track).ConfigureAwait(false);

            await ctx.RespondAsync($"Now playing: {Formatter.Bold(Formatter.Sanitize(track.Info.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Info.Author))}.").ConfigureAwait(false);
        }

        [Command("play_sc"), Description("Queues track for playback.")]
        public async Task PlaySoundCloudAsync(CommandContext ctx, string search)
        {
            if (this.Lavalink == null)
                return;

            var result = await this.Lavalink.Rest.GetTracksAsync(search, LavalinkSearchType.SoundCloud).ConfigureAwait(false);
            var track = result.Tracks.First();
            await this.LavalinkVoice.PlayAsync(track).ConfigureAwait(false);

            await ctx.RespondAsync($"Now playing: {Formatter.Bold(Formatter.Sanitize(track.Info.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Info.Author))}.").ConfigureAwait(false);
        }

        [Command("play_partial"), Description("Queues tracks for playback.")]
        public async Task PlayPartialAsync(CommandContext ctx, TimeSpan start, TimeSpan stop, [RemainingText] Uri uri)
        {
            if (this.LavalinkVoice == null)
                return;

            var trackLoad = await this.Lavalink.Rest.GetTracksAsync(uri).ConfigureAwait(false);
            var track = trackLoad.Tracks.First();
            await this.LavalinkVoice.PlayPartialAsync(track, start, stop).ConfigureAwait(false);

            await ctx.RespondAsync($"Now playing: {Formatter.Bold(Formatter.Sanitize(track.Info.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Info.Author))}.").ConfigureAwait(false);
        }

        [Command("pause"), Description("Pauses playback.")]
        public async Task PauseAsync(CommandContext ctx)
        {
            if (this.LavalinkVoice == null)
                return;

            await this.LavalinkVoice.PauseAsync().ConfigureAwait(false);
            await ctx.RespondAsync("Paused.").ConfigureAwait(false);
        }

        [Command("resume"), Description("Resumes playback.")]
        public async Task ResumeAsync(CommandContext ctx)
        {
            if (this.LavalinkVoice == null)
                return;

            await this.LavalinkVoice.ResumeAsync().ConfigureAwait(false);
            await ctx.RespondAsync("Resumed.").ConfigureAwait(false);
        }

        [Command("stop"), Description("Stops playback.")]
        public async Task StopAsync(CommandContext ctx)
        {
            if (this.LavalinkVoice == null)
                return;

            await this.LavalinkVoice.StopAsync().ConfigureAwait(false);
            await ctx.RespondAsync("Stopped.").ConfigureAwait(false);
        }

        [Command("seek"), Description("Seeks in the current track.")]
        public async Task SeekAsync(CommandContext ctx, TimeSpan position)
        {
            if (this.LavalinkVoice == null)
                return;

            await this.LavalinkVoice.SeekAsync(position).ConfigureAwait(false);
            await ctx.RespondAsync($"Seeking to {position}.").ConfigureAwait(false);
        }

        [Command("volume"), Description("Changes playback volume.")]
        public async Task VolumeAsync(CommandContext ctx, int volume)
        {
            if (this.LavalinkVoice == null)
                return;

            await this.LavalinkVoice.SetVolumeAsync(volume).ConfigureAwait(false);
            await ctx.RespondAsync($"Volume set to {volume}%.").ConfigureAwait(false);
        }

        [Command("now_playing"), Description("Shows what's being currently played."), Aliases("np")]
        public async Task NowPlayingAsync(CommandContext ctx)
        {
            if (this.LavalinkVoice == null)
                return;

            var state = this.LavalinkVoice.CurrentState;
            var track = state.Track;
            await ctx.RespondAsync($"Now playing: {Formatter.Bold(Formatter.Sanitize(track.Info.Title))} by {Formatter.Bold(Formatter.Sanitize(track.Info.Author))} [{state.Track.Info.Position}/{track.Info.Length}].").ConfigureAwait(false);
        }

        [Command("reset_equalizer"), Description("resets equalizer settings."),]
        public async Task EqualizerAsync(CommandContext ctx)
        {
            if (this.LavalinkVoice == null)
                return;

            await this.LavalinkVoice.ResetEqualizerAsync().ConfigureAwait(false);
            await ctx.RespondAsync("All equalizer bands were reset.").ConfigureAwait(false);
        }

        [Command("equalizer"), Description("adjusts equalizer settings."), Aliases("eq")] 
        public async Task EqualizerAsync(CommandContext ctx, int band, float gain)
        {
            if (this.LavalinkVoice == null)
                return;

            await this.LavalinkVoice.AdjustEqualizerAsync(new LavalinkBandAdjustment(band, gain)).ConfigureAwait(false);
            await ctx.RespondAsync($"Band {band} adjusted by {gain}").ConfigureAwait(false);
        }
        
        [Command("karaoke"), Description("adjusts karaoke settings."), Aliases("k")]
        public async Task KaraokeAsync(CommandContext ctx, float level, float monoLevel, float filterBand, float filterWidth)
        {
            if (this.LavalinkVoice == null)
                return;

            await this.LavalinkVoice.AddFilterAsync(new LavalinkKaraokeFilter(level, monoLevel, filterBand, filterWidth));
            await ctx.RespondAsync($"Karaoke adjusted.");
        }
        
        [Command("timescale"), Description("adjusts timescale settings."), Aliases("ts")]
        public async Task TimescaleAsync(CommandContext ctx, float speed, float pitch, float rate)
        {
            if (this.LavalinkVoice == null)
                return;

            await this.LavalinkVoice.AddFilterAsync(new LavalinkTimescaleFilter(speed, pitch, rate));
            await ctx.RespondAsync($"Timescale adjusted.");
        }
        
        [Command("tremolo"), Description("adjusts tremolo settings."), Aliases("tr")]
        public async Task TremoloAsync(CommandContext ctx, float frequency, float depth)
        {
            if (this.LavalinkVoice == null)
                return;

            await this.LavalinkVoice.AddFilterAsync(new LavalinkTremoloFilter(frequency, depth));
            await ctx.RespondAsync($"Tremolo adjusted.");
        }
        
        [Command("filters_clear"), Description("clears all filters."), Aliases("fc")]
        public async Task FiltersClearAsync(CommandContext ctx)
        {
            if (this.LavalinkVoice == null)
                return;

            await this.LavalinkVoice.ClearFiltersAsync();
            await ctx.RespondAsync($"Filters cleared.");
        }
        

        [Command("stats"), Description("Displays Lavalink statistics.")]
        public async Task StatsAsync(CommandContext ctx)
        {
            if (this.LavalinkVoice == null)
                return;

            var stats = this.Lavalink.Statistics;
            var sb = new StringBuilder();
            sb.Append("Lavalink resources usage statistics: ```")
                .Append("Uptime:                    ").Append(stats.Uptime).AppendLine()
                .Append("Players:                   ").AppendFormat("{0} active / {1} total", stats.ActivePlayers, stats.TotalPlayers).AppendLine()
                .Append("CPU Cores:                 ").Append(stats.CpuCoreCount).AppendLine()
                .Append("CPU Usage:                 ").AppendFormat("{0:#,##0.0%} lavalink / {1:#,##0.0%} system", stats.CpuLavalinkLoad, stats.CpuSystemLoad).AppendLine()
                .Append("RAM Usage:                 ").AppendFormat("{0} allocated / {1} used / {2} free / {3} reservable", SizeToString(stats.RamAllocated), SizeToString(stats.RamUsed), SizeToString(stats.RamFree), SizeToString(stats.RamReservable)).AppendLine()
                .Append("Audio frames (per minute): ").AppendFormat("{0:#,##0} sent / {1:#,##0} nulled / {2:#,##0} deficit", stats.AverageSentFramesPerMinute, stats.AverageNulledFramesPerMinute, stats.AverageDeficitFramesPerMinute).AppendLine()
                .Append("```");
            await ctx.RespondAsync(sb.ToString()).ConfigureAwait(false);
        }

        private static readonly string[] _units = new[] { "", "ki", "Mi", "Gi" };
        private static string SizeToString(long l)
        {
            double d = l;
            var u = 0;
            while (d >= 900 && u < _units.Length - 2)
            {
                u++;
                d /= 1024;
            }

            return $"{d:#,##0.00} {_units[u]}B";
        }
    }
}
