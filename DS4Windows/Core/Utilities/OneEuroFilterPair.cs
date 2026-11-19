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

using Sensorit.Base;
namespace DS4Windows.Core.Utilities
{

    public class OneEuroFilterPair
    {
        public const double DEFAULT_WHEEL_CUTOFF = 0.1;
        public const double DEFAULT_WHEEL_BETA = 0.1;

        public OneEuroFilter axis1Filter;
        public OneEuroFilter axis2Filter;

        public OneEuroFilterPair()
        {
            axis1Filter = new OneEuroFilter(minCutoff: DEFAULT_WHEEL_CUTOFF, beta: DEFAULT_WHEEL_BETA);
            axis2Filter = new OneEuroFilter(minCutoff: DEFAULT_WHEEL_CUTOFF, beta: DEFAULT_WHEEL_BETA);
        }

        public OneEuroFilterPair(double minCutoff, double beta)
        {
            axis1Filter = new OneEuroFilter(minCutoff: minCutoff, beta: beta);
            axis2Filter = new OneEuroFilter(minCutoff: minCutoff, beta: beta);
        }
    }
}

