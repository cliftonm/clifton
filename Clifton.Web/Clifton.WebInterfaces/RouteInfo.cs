/* The MIT License (MIT)
* 
* Copyright (c) 2015 Marc Clifton
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

using System;

namespace Clifton.WebInterfaces
{
	public class RouteInfo
	{
		public Type ReceptorSemanticType { get; protected set; }
		public RouteType RouteType { get; protected set; }
		public uint RoleMask { get; protected set; }

		/// <summary>
		/// By default, the role mask is 0: no role.
		/// The application determines how the uint bits determine role permissions.
		/// Any bits that are set with a binary "and" of the route's role mask and the current role passes the authorization test.
		/// </summary>
		public RouteInfo(Type receptorSemanticType, RouteType routeType = RouteType.PublicRoute, uint roleMask = 0)
		{
			ReceptorSemanticType = receptorSemanticType;
			RouteType = routeType;
			RoleMask = roleMask;
		}
	}
}
