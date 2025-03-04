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
using Emzi0767.Utilities;

namespace DSharpPlus.Lavalink.EventArgs
{
    /// <summary>
    /// Represents arguments for player update event.
    /// </summary>
    public sealed class PlayerUpdateEventArgs : AsyncEventArgs
    {
        /// <summary>
        /// Gets the timestamp at which this event was emitted.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Gets the position in the playback stream.
        /// </summary>
        public TimeSpan Position { get; }

        /// <summary>
        /// Gets the player that emitted this event.
        /// </summary>
        public LavalinkGuildConnection Player { get; }
        /// <summary>
        /// If Lavalink is connected to the voice gateway
        /// </summary>
        public bool Connected { get; }
        /// <summary>
        /// The ping of the node to the Discord voice server in milliseconds (-1 if not connected)
        /// </summary>
        public int Ping { get; }

        internal PlayerUpdateEventArgs(LavalinkGuildConnection lvl, DateTimeOffset timestamp, TimeSpan position, bool connected, int ping)
        {
            this.Player = lvl;
            this.Timestamp = timestamp;
            this.Position = position;
            this.Connected = connected;
            this.Ping = ping;
        }
    }
}
