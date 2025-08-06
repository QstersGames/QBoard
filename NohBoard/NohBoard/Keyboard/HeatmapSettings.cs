/*
Copyright (C) 2025 by QBoard Project

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

namespace ThoNohT.NohBoard.Keyboard
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using ThoNohT.NohBoard.Extra;

    /// <summary>
    /// Represents a single color stop in the heatmap gradient.
    /// </summary>
    [DataContract]
    public class HeatmapColorStop
    {
        /// <summary>
        /// The heat level (0.0 to 1.0) where this color appears.
        /// </summary>
        [DataMember]
        public double Level { get; set; }

        /// <summary>
        /// The color at this heat level.
        /// </summary>
        [DataMember]
        public SerializableColor Color { get; set; }

        /// <summary>
        /// Default constructor for serialization.
        /// </summary>
        public HeatmapColorStop()
        {
        }

        /// <summary>
        /// Creates a new color stop.
        /// </summary>
        /// <param name="level">The heat level (0.0 to 1.0).</param>
        /// <param name="color">The color at this level.</param>
        public HeatmapColorStop(double level, SerializableColor color)
        {
            Level = level;
            Color = color;
        }
    }

    /// <summary>
    /// Contains all heatmap configuration settings for a keyboard style.
    /// </summary>
    [DataContract]
    public class HeatmapSettings
    {
        /// <summary>
        /// Blend factor multiplier for color blending. Higher values make heat colors more visible at lower heat levels.
        /// Range: 0.0 to 3.0. Default: 1.0
        /// </summary>
        [DataMember]
        public double BlendAmplification { get; set; } = 1.0;

        /// <summary>
        /// Whether to use logarithmic scaling for heat levels. 
        /// True = smoother curve, prevents overwhelming from heavy-use keys
        /// False = linear scaling, more dramatic differences
        /// </summary>
        [DataMember]
        public bool UseLogarithmicScaling { get; set; } = true;

        /// <summary>
        /// The color gradient stops for the heatmap. Must be ordered by Level from 0.0 to 1.0.
        /// </summary>
        [DataMember]
        public List<HeatmapColorStop> ColorStops { get; set; }

        /// <summary>
        /// Creates default heatmap settings with the current gradient: White -> Yellow -> Red.
        /// </summary>
        public HeatmapSettings()
        {
            ColorStops = new List<HeatmapColorStop>
            {
                new HeatmapColorStop(0.0, System.Drawing.Color.FromArgb(255, 255, 255)),  // White (0%)
                new HeatmapColorStop(0.333, System.Drawing.Color.FromArgb(255, 255, 0)),  // Yellow (33%)
                new HeatmapColorStop(1.0, System.Drawing.Color.FromArgb(255, 0, 0))       // Red (100%)
            };
        }

        /// <summary>
        /// Returns a clone of these heatmap settings.
        /// </summary>
        /// <returns>The cloned settings.</returns>
        public HeatmapSettings Clone()
        {
            var clone = new HeatmapSettings
            {
                BlendAmplification = this.BlendAmplification,
                UseLogarithmicScaling = this.UseLogarithmicScaling,
                ColorStops = new List<HeatmapColorStop>()
            };

            foreach (var stop in this.ColorStops)
            {
                clone.ColorStops.Add(new HeatmapColorStop(stop.Level, stop.Color.Clone()));
            }

            return clone;
        }
    }
}