﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using NuGet.Licenses.Models;
using NuGet.Licenses.Services;
using NuGet.Packaging.Licenses;
using Xunit;

namespace NuGet.Licenses.Tests
{
    public class TheGetLicenseExpressionRunsMethod : LicenseExpressionSplitterFactsBase
    {
        [Fact]
        public void ThrowsWhenLicenseExpressionRootIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => _target.GetLicenseExpressionRuns(null));
        }

        public static IEnumerable<object[]> LicenseExpressionsAndRuns => new object[][]
        {
            new object[] { "(MIT OR ISC)", new[] { License("MIT"), Or(), License("ISC") } },
            new object[] { "(((MIT  OR ISC)))", new[] { License("MIT"), Or(), License("ISC") } },
            new object[] { "(((MIT)) OR  ((ISC)))", new[] { License("MIT"), Or(), License("ISC") } },
            new object[] { "(MIT OR ISC  WITH Classpath-exception-2.0)", new[] { License("MIT"), Or(), License("ISC"), With(), Exception("Classpath-exception-2.0") } },
            new object[] { "(MIT+ OR  ((ISC)))", new[] { License("MIT"), Operator("+"), Or(), License("ISC") } },
        };

        [Theory]
        [MemberData(nameof(LicenseExpressionsAndRuns))]
        public void ProducesProperSequenceOfRuns(string licenseExpression, CompositeLicenseExpressionRun[] expectedSequence)
        {
            var expressionTreeRoot = NuGetLicenseExpression.Parse(licenseExpression);

            var runs = _target.GetLicenseExpressionRuns((LicenseOperator)expressionTreeRoot);

            Assert.NotNull(runs);
            Assert.Equal(expectedSequence, runs, new ComplexLicenseExpressionRunEqualityComparer());
        }
    }

    public class TheSplitFullExpressionMethod : LicenseExpressionSplitterFactsBase
    {
        [Fact]
        public void ThrowsWhenLicenseExpressionIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _target.SplitFullExpression(null, new CompositeLicenseExpressionRun[0]));
            Assert.Equal("licenseExpression", ex.ParamName);
        }

        [Fact]
        public void ThrowsWhenRunsIsNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => _target.SplitFullExpression("", null));
            Assert.Equal("runs", ex.ParamName);
        }

        public static IEnumerable<object[]> LicenseExpressionsAndRuns => new object[][]
        {
            new object[] {
                "(MIT OR ISC)",
                new[] { License("MIT"), Or(), License("ISC") },
                new[] { Other("("), License("MIT"), Other(" "), Or(), Other(" "), License("ISC"), Other(")") }
            },
            new object[] {
                "(((MIT  OR ISC)))",
                new[] { License("MIT"), Or(), License("ISC") },
                new[] { Other("((("), License("MIT"), Other("  "), Or(), Other(" "), License("ISC"), Other(")))") }
            },
            new object[] {
                "(((MIT)) OR  ((ISC)))",
                new[] { License("MIT"), Or(), License("ISC") },
                new[] { Other("((("), License("MIT"), Other(")) "), Or(), Other("  (("), License("ISC"), Other(")))") }
            },
            new object[] {
                "(MIT OR ISC  WITH Classpath-exception-2.0)",
                new[] { License("MIT"), Or(), License("ISC"), With(), Exception("Classpath-exception-2.0") },
                new[] { Other("("), License("MIT"), Other(" "), Or(), Other(" "), License("ISC"), Other("  "), With(), Other(" "), Exception("Classpath-exception-2.0"), Other(")") }
            },
        };

        [Theory]
        [MemberData(nameof(LicenseExpressionsAndRuns))]
        public void AddsParenthesesAndWhitespace(string licenseExpression, CompositeLicenseExpressionRun[] runs, CompositeLicenseExpressionRun[] expectedRuns)
        {
            var result = _target.SplitFullExpression(licenseExpression, runs);

            Assert.Equal(expectedRuns, result, new ComplexLicenseExpressionRunEqualityComparer());
        }
    }


    public class LicenseExpressionSplitterFactsBase
    {
        protected LicenseExpressionSplitter _target;

        public LicenseExpressionSplitterFactsBase()
        {
            _target = new LicenseExpressionSplitter();
        }

        protected static CompositeLicenseExpressionRun License(string licenseId)
            => new CompositeLicenseExpressionRun(licenseId, CompositeLicenseExpressionRunType.LicenseIdentifier);

        protected static CompositeLicenseExpressionRun Operator(string operatorName)
            => new CompositeLicenseExpressionRun(operatorName, CompositeLicenseExpressionRunType.Operator);

        protected static CompositeLicenseExpressionRun Exception(string exceptionId)
            => new CompositeLicenseExpressionRun(exceptionId, CompositeLicenseExpressionRunType.ExceptionIdentifier);

        protected static CompositeLicenseExpressionRun Or() => Operator("OR");
        protected static CompositeLicenseExpressionRun And() => Operator("AND");
        protected static CompositeLicenseExpressionRun With() => Operator("WITH");

        protected static CompositeLicenseExpressionRun Other(string value)
            => new CompositeLicenseExpressionRun(value, CompositeLicenseExpressionRunType.Other);
    }

    internal class ComplexLicenseExpressionRunEqualityComparer : IEqualityComparer<CompositeLicenseExpressionRun>
    {
        public bool Equals(CompositeLicenseExpressionRun x, CompositeLicenseExpressionRun y)
        {
            if (x == null || y == null)
            {
                return x == y;
            }

            return x.Type == y.Type && x.Value == y.Value;
        }

        public int GetHashCode(CompositeLicenseExpressionRun obj)
        {
            return obj.Type.GetHashCode() ^ obj.Value.GetHashCode();
        }
    }
}
