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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink.Entities;
using DSharpPlus.Lavalink.Entities.Filters;
using DSharpPlus.Lavalink.Enums;
using DSharpPlus.Lavalink.EventArgs;
using Emzi0767.Utilities;
using Newtonsoft.Json;

namespace DSharpPlus.Lavalink
{
    internal delegate void ChannelDisconnectedEventHandler(LavalinkGuildConnection node);

    /// <summary>
    /// Represents a Lavalink connection to a channel.
    /// </summary>
    public sealed class LavalinkGuildConnection
    {
        /// <summary>
        /// Triggered whenever Lavalink updates player status.
        /// </summary>
        public event AsyncEventHandler<LavalinkGuildConnection, PlayerUpdateEventArgs> PlayerUpdated
        {
            add { this._playerUpdated.Register(value); }
            remove { this._playerUpdated.Unregister(value); }
        }
        private readonly AsyncEvent<LavalinkGuildConnection, PlayerUpdateEventArgs> _playerUpdated;

        /// <summary>
        /// Triggered whenever playback of a track starts.
        /// <para>This is only available for version 3.3.1 and greater.</para>
        /// </summary>
        public event AsyncEventHandler<LavalinkGuildConnection, TrackStartEventArgs> PlaybackStarted
        {
            add { this._playbackStarted.Register(value); }
            remove { this._playbackStarted.Unregister(value); }
        }
        private readonly AsyncEvent<LavalinkGuildConnection, TrackStartEventArgs> _playbackStarted;

        /// <summary>
        /// Triggered whenever playback of a track finishes.
        /// </summary>
        public event AsyncEventHandler<LavalinkGuildConnection, TrackFinishEventArgs> PlaybackFinished
        {
            add { this._playbackFinished.Register(value); }
            remove { this._playbackFinished.Unregister(value); }
        }
        private readonly AsyncEvent<LavalinkGuildConnection, TrackFinishEventArgs> _playbackFinished;

        /// <summary>
        /// Triggered whenever playback of a track gets stuck.
        /// </summary>
        public event AsyncEventHandler<LavalinkGuildConnection, TrackStuckEventArgs> TrackStuck
        {
            add { this._trackStuck.Register(value); }
            remove { this._trackStuck.Unregister(value); }
        }
        private readonly AsyncEvent<LavalinkGuildConnection, TrackStuckEventArgs> _trackStuck;

        /// <summary>
        /// Triggered whenever playback of a track encounters an error.
        /// </summary>
        public event AsyncEventHandler<LavalinkGuildConnection, TrackExceptionEventArgs> TrackException
        {
            add { this._trackException.Register(value); }
            remove { this._trackException.Unregister(value); }
        }
        private readonly AsyncEvent<LavalinkGuildConnection, TrackExceptionEventArgs> _trackException;

        /// <summary>
        /// Triggered whenever Discord Voice WebSocket connection is terminated.
        /// </summary>
        public event AsyncEventHandler<LavalinkGuildConnection, WebSocketCloseEventArgs> DiscordWebSocketClosed
        {
            add { this._webSocketClosed.Register(value); }
            remove { this._webSocketClosed.Unregister(value); }
        }
        private readonly AsyncEvent<LavalinkGuildConnection, WebSocketCloseEventArgs> _webSocketClosed;

        internal event ChannelDisconnectedEventHandler ChannelDisconnected;

        /// <summary>
        /// Gets whether this channel is still connected.
        /// </summary>
        public bool IsConnected => !Volatile.Read(ref this._isDisposed) && this.Channel != null;
        private bool _isDisposed = false;

        /// <summary>
        /// Gets the current player state.
        /// </summary>
        public LavalinkPlayerState CurrentState { get; }

        /// <summary>
        /// Gets the voice channel associated with this connection.
        /// </summary>
        public DiscordChannel Channel { get; }

        /// <summary>
        /// Gets the guild associated with this connection.
        /// </summary>
        public DiscordGuild Guild => this.Channel.Guild;

        /// <summary>
        /// Gets the Lavalink node associated with this connection.
        /// </summary>
        public LavalinkNodeConnection Node { get; }
        public LavalinkVoiceState VoiceState { get; }
        public VoiceStateUpdateEventArgs VoiceStateUpdate { get; set; }
        public List<ILavalinkFilter> Filters { get; set; } = new();

        internal string GuildIdString => this.GuildId.ToString(CultureInfo.InvariantCulture);
        internal ulong GuildId => this.Channel.Guild.Id;
        internal TaskCompletionSource<bool> VoiceWsDisconnectTcs { get; set; }

        internal LavalinkGuildConnection(LavalinkNodeConnection node, DiscordChannel channel, LavalinkVoiceState state, VoiceStateUpdateEventArgs vstu)
        {
            this.Node = node;
            this.VoiceState = state;
            this.Channel = channel;
            this.VoiceStateUpdate = vstu;
            this.CurrentState = new LavalinkPlayerState();
            this.VoiceState = new LavalinkVoiceState();
            this.VoiceWsDisconnectTcs = new TaskCompletionSource<bool>();

            Volatile.Write(ref this._isDisposed, false);

            this._playerUpdated = new AsyncEvent<LavalinkGuildConnection, PlayerUpdateEventArgs>("LAVALINK_PLAYER_UPDATE", TimeSpan.Zero, this.Node.Discord.EventErrorHandler);
            this._playbackStarted = new AsyncEvent<LavalinkGuildConnection, TrackStartEventArgs>("LAVALINK_PLAYBACK_STARTED", TimeSpan.Zero, this.Node.Discord.EventErrorHandler);
            this._playbackFinished = new AsyncEvent<LavalinkGuildConnection, TrackFinishEventArgs>("LAVALINK_PLAYBACK_FINISHED", TimeSpan.Zero, this.Node.Discord.EventErrorHandler);
            this._trackStuck = new AsyncEvent<LavalinkGuildConnection, TrackStuckEventArgs>("LAVALINK_TRACK_STUCK", TimeSpan.Zero, this.Node.Discord.EventErrorHandler);
            this._trackException = new AsyncEvent<LavalinkGuildConnection, TrackExceptionEventArgs>("LAVALINK_TRACK_EXCEPTION", TimeSpan.Zero, this.Node.Discord.EventErrorHandler);
            this._webSocketClosed = new AsyncEvent<LavalinkGuildConnection, WebSocketCloseEventArgs>("LAVALINK_DISCORD_WEBSOCKET_CLOSED", TimeSpan.Zero, this.Node.Discord.EventErrorHandler);
        }

        /// <summary>
        /// Disconnects the connection from the voice channel.
        /// </summary>
        /// <param name="shouldDestroy">Whether the connection should be destroyed on the Lavalink server when leaving.</param>

        public Task DisconnectAsync(bool shouldDestroy = true)
            => this.DisconnectInternalAsync(shouldDestroy);

        internal async Task DisconnectInternalAsync(bool shouldDestroy, bool isManualDisconnection = false)
        {
            if (!this.IsConnected && !isManualDisconnection)
                throw new InvalidOperationException("This connection is not valid.");

            Volatile.Write(ref this._isDisposed, true);

            if (shouldDestroy)
                await this.Node.Rest.InternalDestroyAsync(this.GuildId, this.Node.SessionId).ConfigureAwait(false);

            if (!isManualDisconnection)
            {
                await this.SendVoiceUpdateAsync().ConfigureAwait(false);
                this.ChannelDisconnected?.Invoke(this);
            }
        }

        internal async Task SendVoiceUpdateAsync()
        {
            var vsd = new VoiceDispatch
            {
                OpCode = 4,
                Payload = new VoiceStateUpdatePayload
                {
                    GuildId = this.GuildId,
                    ChannelId = null,
                    Deafened = false,
                    Muted = false
                }
            };
            var vsj = JsonConvert.SerializeObject(vsd, Formatting.None);
            await (this.Channel.Discord as DiscordClient).WsSendAsync(vsj).ConfigureAwait(false);
        }

        /// <summary>
        /// Searches for specified terms.
        /// </summary>
        /// <param name="searchQuery">What to search for.</param>
        /// <param name="type">What platform will search for.</param>
        /// <returns>A collection of tracks matching the criteria.</returns>
        public Task<LavalinkLoadResult> GetTracksAsync(string searchQuery, LavalinkSearchType type = LavalinkSearchType.Youtube)
            => this.Node.Rest.GetTracksAsync(searchQuery, type);

        /// <summary>
        /// Loads tracks from specified URL.
        /// </summary>
        /// <param name="uri">URL to load tracks from.</param>
        /// <returns>A collection of tracks from the URL.</returns>
        public Task<LavalinkLoadResult> GetTracksAsync(Uri uri)
            => this.Node.Rest.GetTracksAsync(uri);

        /// <summary>
        /// Loads tracks from a local file.
        /// </summary>
        /// <param name="file">File to load tracks from.</param>
        /// <returns>A collection of tracks from the file.</returns>
        public Task<LavalinkLoadResult> GetTracksAsync(FileInfo file)
            => this.Node.Rest.GetTracksAsync(file);

        public Task<LavalinkPlayer> GetPlayerAsync()
            => this.Node.Rest.InternalGetPlayerAsync(this.Node.SessionId, this.GuildId);

        /// <summary>
        /// Queues the specified track for playback.
        /// </summary>
        /// <param name="track">Track to play.</param>
        /// <exception cref="InvalidOperationException">Thrown when the connection is not valid.</exception>
        public async Task PlayAsync(LavalinkTrack track, bool noReplace = true)
        {
            if (!this.IsConnected)
                throw new InvalidOperationException("This connection is not valid.");

            await this.Node.Rest.InternalUpdatePlayerAsync(this.GuildId, this.Node.SessionId, new LavalinkPlayerUpdatePayload
            {
                EncodedTrack = track.Encoded
            }, noReplace);
        }

        /// <summary>
        /// Queues the specified track for playback. The track will be played from specified start timestamp to specified end timestamp.
        /// </summary>
        /// <param name="track">Track to play.</param>
        /// <param name="start">Timestamp to start playback at.</param>
        /// <param name="end">Timestamp to stop playback at.</param>
        /// <exception cref="ArgumentException">Thrown when both start and end timestamps are less than zero, or when end timestamp is less than start timestamp.</exception>
        public async Task PlayPartialAsync(LavalinkTrack track, TimeSpan start, TimeSpan end, bool noReplace = true)
        {
            if (!this.IsConnected)
                throw new InvalidOperationException("This connection is not valid.");

            if (start.TotalMilliseconds < 0 || end <= start)
                throw new ArgumentException("Both start and end timestamps need to be greater or equal to zero, and the end timestamp needs to be greater than start timestamp.");

            await this.Node.Rest.InternalUpdatePlayerAsync(this.GuildId, this.Node.SessionId, new LavalinkPlayerUpdatePayload
            {
                EncodedTrack = track.Encoded,
                Position = start.Milliseconds,
                EndTime = end.Milliseconds
            }, noReplace);
        }

        /// <summary>
        /// Stops the player completely.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when connection is not valid.</exception>
        public Task StopAsync()
        {
            if (!this.IsConnected)
                throw new InvalidOperationException("This connection is not valid.");

            return this.Node.Rest.InternalUpdatePlayerAsync(this.GuildId, this.Node.SessionId, new LavalinkPlayerUpdatePayload
            {
                EncodedTrack = null
            });
        }

        /// <summary>
        /// Pauses the player.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when connection is not valid.</exception>
        public Task PauseAsync()
        {
            if (!this.IsConnected)
                throw new InvalidOperationException("This connection is not valid.");

            return this.Node.Rest.InternalUpdatePlayerAsync(this.GuildId, this.Node.SessionId, new LavalinkPlayerUpdatePayload
            {
                Paused = true
            });

        }

        /// <summary>
        /// Resumes playback.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when connection is not valid.</exception>
        public Task ResumeAsync()
        {
            if (!this.IsConnected)
                throw new InvalidOperationException("This connection is not valid.");

            return this.Node.Rest.InternalUpdatePlayerAsync(this.GuildId, this.Node.SessionId, new LavalinkPlayerUpdatePayload
            {
                Paused = false
            });
        }

        /// <summary>
        /// Seeks the current track to specified position.
        /// </summary>
        /// <param name="position">Position to seek to.</param>
        /// <exception cref="InvalidOperationException">Thrown when connection is not valid.</exception>
        public Task SeekAsync(TimeSpan position)
        {
            if (!this.IsConnected)
                throw new InvalidOperationException("This connection is not valid.");

            if (position.TotalMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(position), "Position needs to be greater or equal to zero.");

            return this.Node.Rest.InternalUpdatePlayerAsync(this.GuildId, this.Node.SessionId, new LavalinkPlayerUpdatePayload
            {
                Position = position.Milliseconds
            });
        }

        /// <summary>
        /// Sets the playback volume. This might incur a lot of CPU usage.
        /// </summary>
        /// <param name="volume">Volume to set. Needs to be greater or equal to 0, and less than or equal to 100. 100 means 100% and is the default value.</param>
        /// <exception cref="InvalidOperationException">Thrown when connection is not valid.</exception>
        public Task SetVolumeAsync(int volume)
        {
            if (!this.IsConnected)
                throw new InvalidOperationException("This connection is not valid.");

            if (volume < 0 || volume > 1000)
                throw new ArgumentOutOfRangeException(nameof(volume), "Volume needs to range from 0 to 1000.");

            return this.Node.Rest.InternalUpdatePlayerAsync(this.GuildId, this.Node.SessionId, new LavalinkPlayerUpdatePayload
            {
                Volume = volume
            });
        }

        /// <summary>
        /// Adjusts the specified bands in the audio equalizer. This will alter the sound output, and might incur a lot of CPU usage.
        /// </summary>
        /// <param name="bands">Bands adjustments to make. You must specify one adjustment per band at most.</param>
        /// <exception cref="InvalidOperationException">Thrown when connection is not valid, or when multiple adjustments are specified for the same band.</exception>
        public Task AdjustEqualizerAsync(params LavalinkBandAdjustment[] bands)
        {
            if (!this.IsConnected)
                throw new InvalidOperationException("This connection is not valid.");

            if (bands?.Any() != true)
                return this.ResetEqualizerAsync();

            if (bands.Distinct(new LavalinkBandAdjustmentComparer()).Count() != bands.Count())
                throw new InvalidOperationException("You cannot specify multiple modifiers for the same band.");
            return this.Node.Rest.InternalUpdatePlayerAsync(this.GuildId, this.Node.SessionId, new LavalinkPlayerUpdatePayload
            {
                Filters = LavalinkFilters.FromFilters(this.Filters, bands)
            });
        }

        /// <summary>
        /// Resets the equalizer to default values.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when connection is not valid.</exception>
        public Task ResetEqualizerAsync()
        {
            if (!this.IsConnected)
                throw new InvalidOperationException("This connection is not valid.");

            return this.Node.Rest.InternalUpdatePlayerAsync(this.GuildId, this.Node.SessionId, new LavalinkPlayerUpdatePayload
            {
                Filters = new LavalinkFilters
                {
                    Equalizers = null
                }
            });
        }

        /// <summary>
        /// Adds a filter to the player.
        /// </summary>
        /// <param name="filter">Filter to apply.</param>
        /// <exception cref="InvalidOperationException">Thrown when connection is not valid.</exception>
        /// <exception cref="ArgumentNullException">Thrown when filter is null.</exception>
        public Task AddFilterAsync(ILavalinkFilter filter)
        {
            if (!this.IsConnected)
                throw new InvalidOperationException("This connection is not valid.");

            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            if (this.Filters.Any(x => x.GetType() == filter.GetType()))
            {
                this.Filters.Remove(this.Filters.First(x => x.GetType() == filter.GetType()));
            }

            this.Filters.Add(filter);

            return this.Node.Rest.InternalUpdatePlayerAsync(this.GuildId, this.Node.SessionId, new LavalinkPlayerUpdatePayload
            {
                Filters = LavalinkFilters.FromFilters(this.Filters)
            });
        }

        /// <summary>
        /// Clears all filters from the player.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when connection is not valid.</exception>
        public Task ClearFiltersAsync()
        {
            if (!this.IsConnected)
                throw new InvalidOperationException("This connection is not valid.");

            this.Filters.Clear();

            return this.Node.Rest.InternalUpdatePlayerAsync(this.GuildId, this.Node.SessionId, new LavalinkPlayerUpdatePayload
            {
                Filters = LavalinkFilters.DefaultValues(),
            });
        }

        /// <summary>
        /// Sets the volume filter. This will alter the sound output, and might incur a lot of CPU usage.
        /// </summary>
        /// <param name="volume">Volume to set. Needs to be greater or equal to 0, and less than or equal to 5. 1 means 100% and is the default value.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when volume is less than 0 or greater than 5.</exception>
        public Task SetVolumeFilterAsync(float volume = 1f)
        {
            if (volume < 0 || volume > 5)
                throw new ArgumentOutOfRangeException(nameof(volume), "Volume needs to range from 0 to 5.");

            return this.Node.Rest.InternalUpdatePlayerAsync(this.GuildId, this.Node.SessionId, new LavalinkPlayerUpdatePayload
            {
                Filters = new LavalinkFilters
                {
                    Volume = volume
                }
            });
        }



        internal Task InternalUpdatePlayerStateAsync(LavalinkPlayerState newState) => this._playerUpdated.InvokeAsync(this, new PlayerUpdateEventArgs(this, newState.Time, newState.Position, newState.Connected, newState.Ping));

        internal Task InternalPlaybackStartedAsync(string track)
        {
            var ea = new TrackStartEventArgs(this, LavalinkUtilities.DecodeTrack(track));
            this.CurrentState.Track = ea.Track;
            return this._playbackStarted.InvokeAsync(this, ea);
        }

        internal Task InternalPlaybackFinishedAsync(TrackFinishData e)
        {
            if (e.Reason != TrackEndReason.Replaced)
                this.CurrentState.Track = default;

            var ea = new TrackFinishEventArgs(this, LavalinkUtilities.DecodeTrack(e.Track), e.Reason);
            return this._playbackFinished.InvokeAsync(this, ea);
        }

        internal Task InternalTrackStuckAsync(TrackStuckData e)
        {
            var ea = new TrackStuckEventArgs(this, e.Threshold, LavalinkUtilities.DecodeTrack(e.Track));
            return this._trackStuck.InvokeAsync(this, ea);
        }

        internal Task InternalTrackExceptionAsync(TrackExceptionData e)
        {
            var ea = new TrackExceptionEventArgs(this, e.Error, LavalinkUtilities.DecodeTrack(e.Track));
            return this._trackException.InvokeAsync(this, ea);
        }

        internal Task InternalWebSocketClosedAsync(WebSocketCloseEventArgs e)
            => this._webSocketClosed.InvokeAsync(this, e);
    }
}
