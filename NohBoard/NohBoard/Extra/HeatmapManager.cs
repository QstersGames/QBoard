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

namespace ThoNohT.NohBoard.Extra
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;

    /// <summary>
    /// Manages heatmap data for keyboard visualization, tracking key press counts and generating heat colors.
    /// </summary>
    public static class HeatmapManager
    {
        #region Configuration Constants
        
        /// <summary>
        /// Blend factor multiplier for color blending. Higher values make heat colors more visible at lower heat levels.
        /// Range: 0.5 to 3.0. Default: 1.5
        /// </summary>
        private const double BlendAmplification = 1.5;
        
        /// <summary>
        /// Whether to use logarithmic scaling for heat levels. 
        /// True = smoother curve, prevents overwhelming from heavy-use keys
        /// False = linear scaling, more dramatic differences
        /// </summary>
        private const bool UseLogarithmicScaling = true;
        
        #endregion
        
        #region Fields

        /// <summary>
        /// Dictionary storing press counts for each keycode.
        /// This allows heatmap data to persist across different keyboard layouts.
        /// </summary>
        private static readonly Dictionary<int, long> keyPressCounts = new Dictionary<int, long>();

        /// <summary>
        /// The maximum number of presses recorded for any single key (used for normalization).
        /// </summary>
        private static long maxPressCount = 1;

        /// <summary>
        /// Total number of key presses recorded across all keys.
        /// </summary>
        private static long totalPresses = 0;

        /// <summary>
        /// When the heatmap data was last reset.
        /// </summary>
        private static DateTime lastReset = DateTime.Now;

        /// <summary>
        /// Tracks which keys were pressed in the last frame to avoid double-counting held keys.
        /// </summary>
        private static readonly HashSet<int> previouslyPressedKeys = new HashSet<int>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the total number of key presses recorded.
        /// </summary>
        public static long TotalPresses => totalPresses;

        /// <summary>
        /// Gets the maximum press count for any single key.
        /// </summary>
        public static long MaxPressCount => maxPressCount;

        /// <summary>
        /// Gets when the heatmap was last reset.
        /// </summary>
        public static DateTime LastReset => lastReset;

        #endregion

        #region Public Methods

        /// <summary>
        /// Records a key press for the specified keycode.
        /// Only counts new presses, not held keys.
        /// </summary>
        /// <param name="keyCode">The keycode of the key that was pressed.</param>
        public static void RecordKeyPress(int keyCode)
        {
            // Only count if this key wasn't pressed in the previous frame
            if (!previouslyPressedKeys.Contains(keyCode))
            {
                // Increment the press count for this key
                if (keyPressCounts.ContainsKey(keyCode))
                {
                    keyPressCounts[keyCode]++;
                }
                else
                {
                    keyPressCounts[keyCode] = 1;
                }

                // Update statistics
                totalPresses++;
                maxPressCount = Math.Max(maxPressCount, keyPressCounts[keyCode]);
                
                // Debug output to show current state
                System.Diagnostics.Debug.WriteLine($"KeyCode {keyCode} press count: {keyPressCounts[keyCode]} (Total: {totalPresses})");
                
                // Trigger a style refresh if heatmap is enabled for current style
                // This forces the background brush cache to be invalidated so colors update immediately
                TriggerStyleRefresh();
            }

            // Add to currently pressed keys
            previouslyPressedKeys.Add(keyCode);
        }

        /// <summary>
        /// Triggers a style refresh if heatmap is enabled, forcing immediate color updates.
        /// </summary>
        private static void TriggerStyleRefresh()
        {
            // Check if heatmap is enabled for the current style
            var heatmapEnabled = GlobalSettings.CurrentStyle?.HeatmapEnabled ?? false;
            if (heatmapEnabled)
            {
                // Increment the style dependency counter to invalidate brush caches
                GlobalSettings.StyleDependencyCounter++;
                
                System.Diagnostics.Debug.WriteLine($"Style refresh triggered - StyleDependencyCounter: {GlobalSettings.StyleDependencyCounter}");
            }
        }

        /// <summary>
        /// Marks a key as no longer pressed. Should be called when keys are released.
        /// </summary>
        /// <param name="keyCode">The keycode of the key that was released.</param>
        public static void RecordKeyRelease(int keyCode)
        {
            previouslyPressedKeys.Remove(keyCode);
        }

        /// <summary>
        /// Clears all currently pressed keys. Called each frame before processing new presses.
        /// </summary>
        public static void ClearPressedKeys()
        {
            previouslyPressedKeys.Clear();
        }

        /// <summary>
        /// Gets the press count for a specific key.
        /// </summary>
        /// <param name="keyCode">The keycode to get the count for.</param>
        /// <returns>The number of times this key has been pressed.</returns>
        public static long GetKeyPressCount(int keyCode)
        {
            return keyPressCounts.TryGetValue(keyCode, out var count) ? count : 0;
        }

        /// <summary>
        /// Calculates the heat level (0.0 to 1.0) for a specific key based on its press count.
        /// Uses logarithmic scaling to prevent heavily-used keys from overwhelming the heatmap.
        /// </summary>
        /// <param name="keyCode">The keycode to calculate heat for.</param>
        /// <returns>Heat level from 0.0 (cold) to 1.0 (hottest).</returns>
        public static double GetKeyHeatLevel(int keyCode)
        {
            if (maxPressCount == 0) return 0.0;

            var pressCount = GetKeyPressCount(keyCode);
            if (pressCount == 0) return 0.0;

            if (UseLogarithmicScaling)
            {
                // Logarithmic scaling to smooth out the curve
                // This prevents heavily-used keys from making others invisible
                var logPresses = Math.Log(pressCount + 1);
                var logMaxPresses = Math.Log(maxPressCount + 1);
                return logPresses / logMaxPresses;
            }
            else
            {
                // Linear scaling - more dramatic differences
                return (double)pressCount / maxPressCount;
            }
        }

        /// <summary>
        /// Gets the heatmap color multiplier for a specific key based on its press frequency.
        /// Returns a color that should be multiplied with the base style color.
        /// </summary>
        /// <param name="keyCode">The keycode to get the multiplier for.</param>
        /// <param name="baseColor">The base color from the style.</param>
        /// <returns>The color after applying heatmap multiplier.</returns>
        public static Color ApplyHeatmapToColor(int keyCode, Color baseColor)
        {
            var heatLevel = GetKeyHeatLevel(keyCode);
            if (heatLevel <= 0.0) return baseColor; // No heat, return original color
            
            // Create heat color based on the scheme: White -> Blue -> Orange -> Red
            var heatColor = CalculateHeatColor(heatLevel);
            
            // Blend the base color with the heat color
            // The more heat, the more the heat color dominates
            var blendFactor = (float)Math.Min(heatLevel * BlendAmplification, 1.0);
            
            var r = (int)(baseColor.R * (1 - blendFactor) + heatColor.R * blendFactor);
            var g = (int)(baseColor.G * (1 - blendFactor) + heatColor.G * blendFactor);
            var b = (int)(baseColor.B * (1 - blendFactor) + heatColor.B * blendFactor);
            
            r = Math.Max(0, Math.Min(255, r));
            g = Math.Max(0, Math.Min(255, g));
            b = Math.Max(0, Math.Min(255, b));
            
            var resultColor = Color.FromArgb(r, g, b);
            
            // Debug output (only for significant heat levels to reduce spam)
            if (heatLevel > 0.1)
            {
                System.Diagnostics.Debug.WriteLine($"KeyCode {keyCode}: Heat={heatLevel:F2}, Result=({resultColor.R},{resultColor.G},{resultColor.B})");
            }
            
            return resultColor;
        }

        /// <summary>
        /// Calculates a color based on heat level using the custom color scheme.
        /// </summary>
        /// <param name="heatLevel">Heat level from 0.0 to 1.0.</param>
        /// <returns>Color representing the heat level.</returns>
        private static Color CalculateHeatColor(double heatLevel)
        {
            // Clamp heat level to valid range
            heatLevel = Math.Max(0.0, Math.Min(1.0, heatLevel));

            // Define color stops for the heatmap: White -> Blue -> Orange -> Red
            var colorStops = new[]
            {
                new { Level = 0.0,  Color = Color.FromArgb(255, 255, 255) }, // White (0%)
                new { Level = 0.33, Color = Color.FromArgb(0, 120, 255) },   // Blue (33%)
                new { Level = 0.67, Color = Color.FromArgb(255, 165, 0) },   // Orange (67%)
                new { Level = 1.0,  Color = Color.FromArgb(255, 0, 0) }      // Red (100%)
            };

            // Find the two color stops to interpolate between
            for (int i = 0; i < colorStops.Length - 1; i++)
            {
                var lower = colorStops[i];
                var upper = colorStops[i + 1];

                if (heatLevel >= lower.Level && heatLevel <= upper.Level)
                {
                    // Calculate interpolation factor
                    var range = upper.Level - lower.Level;
                    var factor = range > 0 ? (heatLevel - lower.Level) / range : 0;

                    // Interpolate between the two colors
                    return InterpolateColor(lower.Color, upper.Color, factor);
                }
            }

            // Fallback to red for maximum heat
            return Color.FromArgb(255, 0, 0);
        }

        /// <summary>
        /// Interpolates between two colors.
        /// </summary>
        /// <param name="color1">The first color.</param>
        /// <param name="color2">The second color.</param>
        /// <param name="factor">Interpolation factor (0.0 to 1.0).</param>
        /// <returns>The interpolated color.</returns>
        private static Color InterpolateColor(Color color1, Color color2, double factor)
        {
            var r = (int)(color1.R + (color2.R - color1.R) * factor);
            var g = (int)(color1.G + (color2.G - color1.G) * factor);
            var b = (int)(color1.B + (color2.B - color1.B) * factor);

            return Color.FromArgb(
                Math.Max(0, Math.Min(255, r)),
                Math.Max(0, Math.Min(255, g)),
                Math.Max(0, Math.Min(255, b))
            );
        }

        /// <summary>
        /// Resets all heatmap data.
        /// </summary>
        public static void ResetHeatmapData()
        {
            keyPressCounts.Clear();
            maxPressCount = 1;
            totalPresses = 0;
            lastReset = DateTime.Now;
        }

        #endregion
    }
}