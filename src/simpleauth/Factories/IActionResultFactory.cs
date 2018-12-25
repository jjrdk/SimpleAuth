﻿namespace SimpleAuth.Factories
{
    using Results;

    public interface IActionResultFactory
    {
        EndpointResult CreateAnEmptyActionResultWithRedirectionToCallBackUrl();
        EndpointResult CreateAnEmptyActionResultWithRedirection();
        EndpointResult CreateAnEmptyActionResultWithOutput();
        EndpointResult CreateAnEmptyActionResultWithNoEffect();
    }
}