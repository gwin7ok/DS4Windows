/*
DS4Windows
Copyright (C) 2023  Travis Nickles

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

#nullable enable

using System;
using Sensorit.Base;

namespace DS4Windows.Core.Settings
{

    public class GyroMouseInfo : ProfileSubSettingBase
    {
        public const double DEFAULT_MINCUTOFF = 1.0;
        public const double DEFAULT_BETA = 0.7;
        public const string DEFAULT_SMOOTH_TECHNIQUE = "one-euro";
        public const double DEFAULT_MIN_THRESHOLD = 1.0;
        public const bool JITTER_COMPENSATION_DEFAULT = true;

        public bool enableSmoothing = false;
        public double smoothingWeight = 0.5;
        public GyroSmoothingMethod smoothingMethod;

        public double minCutoff = DEFAULT_MINCUTOFF;
        public double beta = DEFAULT_BETA;
        public double minThreshold = DEFAULT_MIN_THRESHOLD;
        public bool jitterCompensation = JITTER_COMPENSATION_DEFAULT;

        public delegate void GyroMouseInfoEventHandler(GyroMouseInfo sender, EventArgs args);

        public double MinCutoff
        {
            get => minCutoff;
            set
            {
                if (minCutoff == value) return;
                minCutoff = value;
                MinCutoffChanged?.Invoke(this, EventArgs.Empty);
                RaisePropertyChanged();
            }
        }
        public event GyroMouseInfoEventHandler? MinCutoffChanged;

        public double Beta
        {
            get => beta;
            set
            {
                if (beta == value) return;
                beta = value;
                BetaChanged?.Invoke(this, EventArgs.Empty);
                RaisePropertyChanged();
            }
        }
        public event GyroMouseInfoEventHandler? BetaChanged;

        public bool JitterCompensation
        {
            get => jitterCompensation;
            set
            {
                if (jitterCompensation == value) return;
                jitterCompensation = value;
                RaisePropertyChanged();
            }
        }

        public void Reset()
        {
            minCutoff = DEFAULT_MINCUTOFF;
            beta = DEFAULT_BETA;
            enableSmoothing = false;
            smoothingMethod = GyroSmoothingMethod.None;
            smoothingWeight = 0.5;
            minThreshold = DEFAULT_MIN_THRESHOLD;
            jitterCompensation = JITTER_COMPENSATION_DEFAULT;
            RaiseAllPropertiesChanged();
        }

        public void ResetSmoothing()
        {
            enableSmoothing = false;
            ResetSmoothingMethods();
            RaisePropertyChanged(nameof(enableSmoothing));
        }

        public void ResetSmoothingMethods()
        {
            smoothingMethod = GyroSmoothingMethod.None;
            RaisePropertyChanged(nameof(smoothingMethod));
        }

        public void DetermineSmoothMethod(string identier)
        {
            ResetSmoothingMethods();
            smoothingMethod = SmoothingMethodParse(identier);
            RaisePropertyChanged(nameof(smoothingMethod));
        }

        public static GyroSmoothingMethod SmoothingMethodParse(string identifier) => GyroSmoothingMethods.Parse(identifier);

        public string SmoothMethodIdentifier() => GyroSmoothingMethods.Identifier(smoothingMethod);

        public void SetRefreshEvents(OneEuroFilter euroFilter)
        {
            BetaChanged += (sender, args) => { euroFilter.Beta = beta; };
            MinCutoffChanged += (sender, args) => { euroFilter.MinCutoff = minCutoff; };
        }

        public void RemoveRefreshEvents()
        {
            BetaChanged = null;
            MinCutoffChanged = null;
        }
    }
}