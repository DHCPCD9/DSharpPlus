﻿// This file is part of the DSharpPlus project.
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

using Newtonsoft.Json;

namespace DSharpPlus.Lavalink.Entities.Filters
{
    /// <summary>
    /// Uses equalization to eliminate part of a band, usually targeting vocals.
    /// </summary>
    public class LavalinkKaraokeFilter : ILavalinkFilter
    {
        /// <summary>
        /// The level (0 to 1.0 where 0.0 is no effect and 1.0 is full effect)
        /// </summary>
        [JsonProperty("level")]
        public float? Level { get; set; }

        /// <summary>
        /// The mono level (0 to 1.0 where 0.0 is no effect and 1.0 is full effect)
        /// </summary>
        [JsonProperty("monoLevel")]
        public float? MonoLevel { get; set; }

        /// <summary>
        /// The filter band
        /// </summary>
        [JsonProperty("filterBand")]
        public float? FilterBand { get; set; }

        /// <summary>
        /// The filter width
        /// </summary>
        [JsonProperty("filterWidth")]
        public float? FilterWidth { get; set; }

        public LavalinkKaraokeFilter(float level = 0, float monoLevel = 0, float filterBand = 0, float filterWidth = 0)
        {
            this.Level = level;
            this.MonoLevel = monoLevel;
            this.FilterBand = filterBand;
            this.FilterWidth = filterWidth;
        }

        public void Reset() => (this.Level, this.MonoLevel, this.FilterBand, this.FilterWidth) = (0, 0, 0, 0);
    }
}
