﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using System;

namespace SimpleIdentityServer.Shell
{
    public static class RoutingBuilderExtensions
    {
        public static IRouteBuilder UseShell(this IRouteBuilder routeBuilder)
        {
            if (routeBuilder == null)
            {
                throw new ArgumentNullException(nameof(routeBuilder));
            }

            routeBuilder.MapRoute("Error401Route",
                Core.CoreConstants.EndPoints.Get401,
                new
                {
                    controller = "Error",
                    action = "Get401",
                    area = "Shell"
                }, constraints: new { area = "Shell" });
            routeBuilder.MapRoute("Error404Route",
                Core.CoreConstants.EndPoints.Get404,
                new
                {
                    controller = "Error",
                    action = "Get404",
                    area = "Shell"
                }, constraints: new { area = "Shell" });
            routeBuilder.MapRoute("Error500Route",
                Core.CoreConstants.EndPoints.Get500,
                new
                {
                    controller = "Error",
                    action = "Get500",
                    area = "Shell"
                }, constraints: new { area = "Shell" });
            routeBuilder.MapRoute("BasicShellAuthentication",
                "{controller}/{action}/{id?}",
                new { controller = "Home", action = "Index", area = "Shell" }, constraints: new { area = "Shell" });
            return routeBuilder;
        }
    }
}
