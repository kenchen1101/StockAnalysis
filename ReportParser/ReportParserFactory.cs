﻿using System;
using System.IO;

using StockAnalysis.Share;

namespace ReportParser
{
    public static class ReportParserFactory
    {
        public static IReportParser Create(ReportFileType reportFileType, DataDictionary dataDictionary, TextWriter errorWriter)
        {
            switch (reportFileType)
            {
                case ReportFileType.EastMoneyPlainHtml:
                    return new EastMoneyPlainHtmlReportParser(dataDictionary, errorWriter);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
