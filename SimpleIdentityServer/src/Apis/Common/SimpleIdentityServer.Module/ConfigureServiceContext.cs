﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace SimpleIdentityServer.Module
{
    public class ConfigureServiceContext
    {
        private readonly Dictionary<string, AuthenticationBuilder> _authenticationBuilders;
        private IServiceCollection _services;
        private IMvcBuilder _mvcBuilder;
        private AuthorizationOptions _authorizationOptions;

        internal ConfigureServiceContext()
        {
            _authenticationBuilders = new Dictionary<string, AuthenticationBuilder>();
        }

        public event EventHandler Initialized;
        public event EventHandler AuthenticationCookieAdded;
        public event EventHandler AuthorizationAdded;
        public event EventHandler MvcAdded;

        public Dictionary<string, AuthenticationBuilder> AuthenticationBuilders
        {
            get
            {
                return _authenticationBuilders;
            }
        }

        public IServiceCollection Services
        {
            get
            {
                return _services;
            }
        }

        public AuthorizationOptions AuthorizationOptions
        {
            get
            {
                return _authorizationOptions;
            }
        }

        public IMvcBuilder MvcBuilder
        {
            get
            {
                return _mvcBuilder;
            }
        }

        public void Init(IServiceCollection services)
        {
            _services = services;
            if (Initialized != null)
            {
                Initialized(this, EventArgs.Empty);
            }
        }

        public void AddCookieAuthentication(string cookieName, AuthenticationBuilder authenticationBuilder)
        {
            _authenticationBuilders.Add(cookieName, authenticationBuilder);
            if (AuthenticationCookieAdded != null)
            {
                AuthenticationCookieAdded(this, EventArgs.Empty);
            }
        }

        public void AddAuthorization(AuthorizationOptions authorizationOptions)
        {
            if (authorizationOptions == null)
            {
                throw new ArgumentNullException(nameof(authorizationOptions));
            }

            _authorizationOptions = authorizationOptions;
            if (AuthorizationAdded != null)
            {
                AuthorizationAdded(this, EventArgs.Empty);
            }
        }

        public void AddMvc(IMvcBuilder mvcBuilder)
        {
            if (mvcBuilder == null)
            {
                throw new ArgumentNullException(nameof(mvcBuilder));
            }

            _mvcBuilder = mvcBuilder;
            if(MvcAdded != null)
            {
                MvcAdded(this, EventArgs.Empty);
            }
        }
    }
}