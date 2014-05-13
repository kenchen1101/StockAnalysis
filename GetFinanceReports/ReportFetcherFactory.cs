﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetFinanceReports
{
    public static class ReportFetcherFactory
    {
        public static IReportFetcher Create(string serverAddress)
        {
            if (string.IsNullOrWhiteSpace(serverAddress))
            {
                throw new ArgumentNullException("serverAddress");
            }

            serverAddress = serverAddress.Trim().ToLowerInvariant();

            if (serverAddress.StartsWith("http://")
                || serverAddress.StartsWith("https://"))
            {
                return new WebReportFetcher(serverAddress);
            }

            throw new NotImplementedException();
        }
    }
}
