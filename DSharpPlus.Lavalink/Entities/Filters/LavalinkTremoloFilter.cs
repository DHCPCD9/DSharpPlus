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
    /// Uses amplification to create a shuddering effect, where the volume quickly oscillates. https://en.wikipedia.org/wiki/File:Fuse_Electronics_Tremolo_MK-III_Quick_Demo.ogv
    /// </summary>
    public class LavalinkTremoloFilter : ILavalinkFilter
    {
        /// <summary>
        /// The tremolo depth (0 to 1.0 where 0.0 is no effect and 1.0 is full effect)
        /// </summary>
        [JsonProperty("depth")]
        public float? Depth { get; set; }

        /// <summary>
        /// The tremolo frequency (0 to 20.0 where 0.0 is no effect and 20.0 is full effect)
        /// </summary>
        [JsonProperty("frequency")]
        public float? Frequency { get; set; }

        public LavalinkTremoloFilter(float? depth = 0, float? frequency = 0)
        {
            this.Depth = depth;
            this.Frequency = frequency;
        }

        public void Reset() => (this.Depth, this.Frequency) = (0, 0);
    }
}
