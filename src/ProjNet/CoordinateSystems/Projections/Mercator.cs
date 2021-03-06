// Copyright 2005 - 2009 - Morten Nielsen (www.sharpgis.net)
//
// This file is part of ProjNet.
// ProjNet is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// ProjNet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with ProjNet; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

// SOURCECODE IS MODIFIED FROM ANOTHER WORK AND IS ORIGINALLY BASED ON GeoTools.NET:
/*
 *  Copyright (C) 2002 Urban Science Applications, Inc. 
 *
 *  This library is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public
 *  License as published by the Free Software Foundation; either
 *  version 2.1 of the License, or (at your option) any later version.
 *
 *  This library is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 *  Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public
 *  License along with this library; if not, write to the Free Software
 *  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 *
 */

using System;
using System.Collections.Generic;
using ProjNet.CoordinateSystems.Transformations;

namespace ProjNet.CoordinateSystems.Projections
{
    /// <summary>
	/// Implements the Mercator projection.
	/// </summary>
	/// <remarks>
	/// <para>This map projection introduced in 1569 by Gerardus Mercator. It is often described as a cylindrical projection,
	/// but it must be derived mathematically. The meridians are equally spaced, parallel vertical lines, and the
	/// parallels of latitude are parallel, horizontal straight lines, spaced farther and farther apart as their distance
	/// from the Equator increases. This projection is widely used for navigation charts, because any straight line
	/// on a Mercator-projection map is a line of constant true bearing that enables a navigator to plot a straight-line
	/// course. It is less practical for world maps because the scale is distorted; areas farther away from the equator
	/// appear disproportionately large. On a Mercator projection, for example, the landmass of Greenland appears to be
	/// greater than that of the continent of South America; in actual area, Greenland is smaller than the Arabian Peninsula.
	/// </para>
    /// </remarks>
    [Serializable]
    internal class Mercator : MapProjection
    {
        //double lon_center;		//Center longitude (projection center)
        //double lat_origin;		//center latitude
        //double e,e2;			//eccentricity constants
        private readonly double _k0;                //small value m

        /// <summary>
        /// Initializes the MercatorProjection object with the specified parameters to project points. 
        /// </summary>
        /// <param name="parameters">ParameterList with the required parameters.</param>
        /// <remarks>
        /// </remarks>
        public Mercator(IEnumerable<ProjectionParameter> parameters)
            : this(parameters, null)
        {
        }

        /// <summary>
        /// Initializes the MercatorProjection object with the specified parameters.
        /// </summary>
        /// <param name="parameters">List of parameters to initialize the projection.</param>
        /// <param name="isInverse">Indicates whether the projection forward (meters to degrees or degrees to meters).</param>
        /// <remarks>
        /// <para>The parameters this projection expects are listed below.</para>
        /// <list type="table">
        /// <listheader><term>Items</term><description>Descriptions</description></listheader>
        /// <item><term>central_meridian</term><description>The longitude of the point from which the values of both the geographical coordinates on the ellipsoid and the grid coordinates on the projection are deemed to increment or decrement for computational purposes. Alternatively it may be considered as the longitude of the point which in the absence of application of false coordinates has grid coordinates of (0,0).</description></item>
        /// <item><term>latitude_of_origin</term><description>The latitude of the point from which the values of both the geographical coordinates on the ellipsoid and the grid coordinates on the projection are deemed to increment or decrement for computational purposes. Alternatively it may be considered as the latitude of the point which in the absence of application of false coordinates has grid coordinates of (0,0).</description></item>
        /// <item><term>scale_factor</term><description>The factor by which the map grid is reduced or enlarged during the projection process, defined by its value at the natural origin.</description></item>
        /// <item><term>false_easting</term><description>Since the natural origin may be at or near the centre of the projection and under normal coordinate circumstances would thus give rise to negative coordinates over parts of the mapped area, this origin is usually given false coordinates which are large enough to avoid this inconvenience. The False Easting, FE, is the easting value assigned to the abscissa (east).</description></item>
        /// <item><term>false_northing</term><description>Since the natural origin may be at or near the centre of the projection and under normal coordinate circumstances would thus give rise to negative coordinates over parts of the mapped area, this origin is usually given false coordinates which are large enough to avoid this inconvenience. The False Northing, FN, is the northing value assigned to the ordinate.</description></item>
        /// </list>
        /// </remarks>
        protected Mercator(IEnumerable<ProjectionParameter> parameters, Mercator isInverse)
            : base(parameters, isInverse)
        {
            Authority = "EPSG";
            var scaleFactor = GetParameter("scale_factor");

            if (scaleFactor == null) //This is a two standard parallel Mercator projection (2SP)
            {
                _k0 = Math.Cos(lat_origin) / Math.Sqrt(1.0 - _es * Math.Sin(lat_origin) * Math.Sin(lat_origin));
                AuthorityCode = 9805;
                Name = "Mercator_2SP";
            }
            else //This is a one standard parallel Mercator projection (1SP)
            {
                _k0 = scaleFactor.Value;
                Name = "Mercator_1SP";
            }
        }

        /// <summary>
        /// Converts coordinates in decimal degrees to projected meters.
        /// </summary>
        /// <param name="lon">The longitude of the point in decimal degrees.</param>
        /// <param name="lat">The latitude of the point in decimal degrees.</param>
        /// <returns>Point in projected meters</returns>
        protected override void RadiansToMeters(ref double lon, ref double lat)
        {
            if (double.IsNaN(lon) || double.IsNaN(lat))
            {
                lon = double.NaN;
                lat = double.NaN;
                return;
            }

            double dLongitude = lon;
            double dLatitude = lat;

            /* Forward equations */
            if (Math.Abs(Math.Abs(dLatitude) - HALF_PI) <= EPSLN)
                throw new ArgumentException("Transformation cannot be computed at the poles.");

            double esinphi = _e * Math.Sin(dLatitude);
            lon = _semiMajor * _k0 * (dLongitude - central_meridian);
            lat = _semiMajor * _k0 * Math.Log(Math.Tan(PI * 0.25 + dLatitude * 0.5) *
                                              Math.Pow((1 - esinphi) / (1 + esinphi), _e * 0.5));
        }

        /// <summary>
        /// Converts coordinates in projected meters to decimal degrees.
        /// </summary>
        /// <param name="x">The x-ordinate in projected meters</param>
        /// <param name="y">The y-ordinate in projected meters</param>
        /// <returns>Transformed point in decimal degrees</returns>
        protected override void MetersToRadians(ref double x, ref double y)
        {
            /* Inverse equations
              -----------------*/
            double dX = x; // * _metersPerUnit - this._falseEasting;
            double dY = y; // * _metersPerUnit - this._falseNorthing;
            double ts = Math.Exp(-dY / (_semiMajor * _k0)); //t

            double chi = HALF_PI - 2 * Math.Atan(ts);
            double e4 = Math.Pow(_e, 4);
            double e6 = Math.Pow(_e, 6);
            double e8 = Math.Pow(_e, 8);

            y = chi + (_es * 0.5 + 5 * e4 / 24 + e6 / 12 + 13 * e8 / 360) * Math.Sin(2 * chi)
                    + (7 * e4 / 48 + 29 * e6 / 240 + 811 * e8 / 11520) * Math.Sin(4 * chi) +
                    +(7 * e6 / 120 + 81 * e8 / 1120) * Math.Sin(6 * chi) +
                    +(4279 * e8 / 161280) * Math.Sin(8 * chi);

            x = dX / (_semiMajor * _k0) + central_meridian;

            //return (x, y, z);
        }

        /// <summary>
        /// Returns the inverse of this projection.
        /// </summary>
        /// <returns>IMathTransform that is the reverse of the current projection.</returns>
        public override MathTransform Inverse()
        {
            if (_inverse == null)
                _inverse = new Mercator(_Parameters.ToProjectionParameter(), this);
            return _inverse;
        }
    }
}
