// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Http
{
    /// <summary>
    /// The http request
    /// </summary>
    public interface IRequest
    {
        /// <summary>
        /// The user agent for this request.
        /// </summary>
        string UserAgent { get; set; }

        /// <summary>
        /// The accept header for this request.
        /// </summary>
        string Accept { get; set; }

        /// <summary>
        /// Aborts the request.
        /// </summary>
        void Abort();

        void AddHeader(string name, string value);
        string GetHeader(string name);
        List<string> GetHeaders(string name);
        void SetHeader(string name, string value);
        Task<IResponse> Send();
    }
}
