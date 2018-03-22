﻿import React, { Component } from "react";
import { NavLink } from "react-router-dom";
import { SessionService } from './services';
import { withRouter } from 'react-router-dom';
import { translate } from 'react-i18next';
import AppDispatcher from './appDispatcher';
import Constants from './constants';

class Layout extends Component {
    constructor(props) {
        super(props);
        this._appDispatcher = null;
        this.disconnect = this.disconnect.bind(this);
        this.state = {
            isLoggedIn: false
        };
    }
    /**
     * Disconnect the user.
     * @param {any} e
     */
    disconnect(e) {
        e.preventDefault();
        SessionService.remove();
        this.setState({
            isLoggedIn: false
        });
        AppDispatcher.dispatch({
            actionName: Constants.events.USER_LOGGED_OUT
        });
        this.props.history.push('/');
    }
    render() {
        const { t } = this.props;
        return (<div className="default">
            <nav className="navbar-static-side navbar left">
                <div className="sidebar-collapse">
                    <ul className="nav flex-column">
                        <li className="nav-item"><NavLink to="/about" className="nav-link">{t('aboutMenuItem')}</NavLink></li>
                        {(this.state.isLoggedIn && (
                            <li className="nav-item"><NavLink to="/resources" className="nav-link">{t('resourcesMenuItem')}</NavLink></li>
                        ))}
                        {(this.state.isLoggedIn && (
                            <li className="nav-item"><NavLink to="/connections" className="nav-link">{t('connectionsMenuItem')}</NavLink></li>
                        ))}
                        <li className="nav-item"><NavLink to="/tools" className="nav-link">{t('toolsMenuItem')}</NavLink></li>
                        {(this.state.isLoggedIn && (
                            <li className="nav-item"><NavLink to="/logs" className="nav-link">{t('logsMenuItem')}</NavLink></li>
                        ))}
                        {(this.state.isLoggedIn && (
                            <li className="nav-item"><NavLink to="/settings" className="nav-link">{t('settingsMenuItem')}</NavLink></li>
                        ))}
                        {(this.state.isLoggedIn && (
                            <li className="nav-item"><NavLink to="/cache" className="nav-link">{t('cacheMenuItem')}</NavLink></li>
                        ))}
                        {(this.state.isLoggedIn && (
                            <li className="nav-item"><NavLink to="/manage" className="nav-link">{t('manageMenuItem')}</NavLink></li>
                        ))}
                        {(this.state.isLoggedIn ? (
                            <li className="nav-item"><a href="#" className="nav-link" onClick={this.disconnect}><span className="glyphicon glyphicon-user"></span> {t('disconnect')}</a></li>
                        ) : (
                                <li className="nav-item"><NavLink to="/login" className="nav-link"><span className="glyphicon glyphicon-user"></span> {t('connect')}</NavLink></li>
                        ))}
                    </ul>
                </div>
            </nav>
            <section id="wrapper">
                { /* Navigation */ }
                <nav className="navbar navbar-toggleable-md navbar-light">
                    <a className="navbar-brand" href="#" id="uma-title">Uma</a>
                    <button type="button" className="navbar-toggler" data-toggle="collapse" data-target="#collapseNavBar">
                        <span className="navbar-toggler-icon"></span>
                    </button>
                    <div className="collapse navbar-collapse" id="collapseNavBar">
                        <ul className="navbar-nav mr-auto navbar-right">
                            <li className="nav-item"><NavLink to="/about" className="nav-link">About</NavLink></li>
                            {(this.state.isLoggedIn && (
                                <li className="nav-item"><NavLink to="/resources" className="nav-link">Resources</NavLink></li>
                            ))}
                            {(this.state.isLoggedIn && (
                                <li className="nav-item"><NavLink to="/connections" className="nav-link">Connections</NavLink></li>
                            ))}
                            <li className="nav-item"><NavLink to="/tools" className="nav-link">Tools</NavLink></li>
                            {(this.state.isLoggedIn && (
                                <li className="nav-item"><NavLink to="/logs" className="nav-link">Logs</NavLink></li>
                            ))}
                            {(this.state.isLoggedIn && (
                                <li className="nav-item"><NavLink to="/settings" className="nav-link">Settings</NavLink></li>
                            ))}
                            {(this.state.isLoggedIn && (
                                <li className="nav-item"><NavLink to="/cache" className="nav-link">Cache</NavLink></li>
                            ))}
                            {(this.state.isLoggedIn && (
                                <li className="nav-item"><NavLink to="/manage" className="nav-link">Manage</NavLink></li>
                            ))}
                            {(this.state.isLoggedIn ? (
                                <li className="nav-item"><a href="#" className="nav-link" onClick={this.disconnect}><span className="glyphicon glyphicon-user"></span> Disconnect</a></li>
                            ) : (
                                    <li className="nav-item"><NavLink to="/login" className="nav-link"><span className="glyphicon glyphicon-user"></span> Connect</NavLink></li>
                            ))}
                        </ul>
                    </div>
                </nav>
                { /* Display component */}
                <section id="body">
                    <div className="col-md-10 offset-md-1 cell">
                        {this.props.children}
                    </div>
                </section>
            </section>
        </div>);
    }
    componentDidMount() {
        var self = this;
        self.setState({
            isLoggedIn: !SessionService.isExpired()
        });
        self._appDispatcher = AppDispatcher.register(function (payload) {
            switch (payload.actionName) {
                case Constants.events.USER_LOGGED_IN:
                    self.setState({
                        isLoggedIn: true
                    });
                    break;
            }
        });
    }
    componentWillUnmount() {
        AppDispatcher.unregister(this._appDispatcher);
    }
}

export default translate('common', { wait: process && !process.release })(withRouter(Layout));