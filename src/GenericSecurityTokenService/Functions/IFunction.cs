﻿using System.Threading.Tasks;
using GenericSecurityTokenService.Functions.FunctionOptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace GenericSecurityTokenService.Functions
{
    /// <summary>
    /// This provides interfaces to functions.
    /// </summary>
    public interface IFunction
    {
        /// <summary>
        /// Gets or sets the <see cref="TraceWriter"/> instance.
        /// </summary>
        ILogger Log { get; set; }

        /// <summary>
        /// Invokes the function.
        /// </summary>
        Task<ActionResult> InvokeAsync();
    }
}