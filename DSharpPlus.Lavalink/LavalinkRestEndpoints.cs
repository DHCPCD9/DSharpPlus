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

namespace DSharpPlus.Lavalink
{
    internal static class Endpoints
    {
        internal const string BASE_URL = "/v3";
        internal const string VERSION = "/version";
        internal const string SESSIONS = "{0}/sessions/{1}";

        //Webosocket
        internal const string WEBSOCKET = "/v3/websocket";

        //Track loading
        internal const string LOAD_TRACKS = "/loadtracks";
        internal const string DECODE_TRACK = "/decodetrack";
        internal const string DECODE_TRACKS = "/decodetracks";

        //Route Planner
        internal const string ROUTE_PLANNER = "/routeplanner";
        internal const string STATUS = "/status";
        internal const string FREE_ADDRESS = "/free/address";
        internal const string FREE_ALL = "/free/all";

        //Player
        internal const string PLAYER = "{0}/sessions/{1}/players/{2}";
    }
}
