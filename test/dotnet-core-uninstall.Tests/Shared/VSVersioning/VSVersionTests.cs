﻿using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Tools.Uninstall.Shared.BundleInfo;
using Microsoft.DotNet.Tools.Uninstall.Shared.BundleInfo.Versioning;
using Microsoft.DotNet.Tools.Uninstall.Shared.VSVersioning;
using Microsoft.DotNet.Tools.Uninstall.Tests.Attributes;
using NuGet.Versioning;
using Xunit;

namespace Microsoft.DotNet.Tools.Uninstall.Tests.Shared.VSVersioning
{
    public class VSVersionTests
    {
        [WindowsOnlyTheory]
        [InlineData(new string[] { "1.0.0" }, new bool[] { false })]
        [InlineData(new string[] { "1.0.0", "1.0.1" }, new bool[] { true, false })]
        [InlineData(new string[] { "2.1.0", "1.0.1" }, new bool[] { false, false })]
        [InlineData(new string[] { "1.0.0", "1.0.1", "1.1.0" }, new bool[] { true, true, false })]
        [InlineData(new string[] { "1.0.0", "1.0.1", "2.0.0" }, new bool[] { true, false, false })]
        [InlineData(new string[] { "1.0.0", "1.0.1", "1.0.2" }, new bool[] { true, true, false })]
        [InlineData(new string[] { "2.1.500", "2.1.600" }, new bool[] { false, false })]
        [InlineData(new string[] { "2.1.500", "2.1.400", "2.1.600" }, new bool[] { false, true, false })]
        [InlineData(new string[] { "2.2.100", "2.2.200" }, new bool[] { false, false })]
        [InlineData(new string[] { "2.2.100", "2.2.200", "2.2.300" }, new bool[] { false, true, false })]
        [InlineData(new string[] { "3.0.0", "3.0.1", "5.0.0" }, new bool[] { true, false, false })]
        [InlineData(new string[] { "5.0.0", "5.0.1", "10.100.100" }, new bool[] { false, false, false })]
        internal void TestGetUninstallableWindows(string[] versions, bool[] allowed)
        {
            TestGetUninstallable(versions, allowed);
        }

        // For now we are not protecting versions on mac
        [MacOsOnlyTheory]
        [InlineData(new string[] { "1.0.0" }, new bool[] { true })]
        [InlineData(new string[] { "1.0.0", "1.0.1" }, new bool[] { true, true })]
        [InlineData(new string[] { "2.1.0", "1.0.1" }, new bool[] { true, true })]
        [InlineData(new string[] { "5.0.0", "5.0.1", "10.100.100" }, new bool[] { true, true, true })]
        internal void TestGetUninstallableMac(string[] versions, bool[] allowed)
        {
            TestGetUninstallable(versions, allowed);
        }

        internal void TestGetUninstallable(string[] versions, bool[] allowed)
        {
            var bundles = new List<Bundle<SdkVersion>>();
            foreach (string v in versions)
            {
                bundles.Add(new Bundle<SdkVersion>(new SdkVersion(v), new BundleArch(), string.Empty, string.Empty));
            }

            var uninstallable = VisualStudioSafeVersionsExtractor.GetUninstallableBundles(bundles);

            CheckAllowed(bundles, uninstallable, allowed);
        }

        [WindowsOnlyTheory]
        [InlineData(new string[] { "1.0.0", "1.0.1" }, new bool[] { true, false })]
        [InlineData(new string[] { "1.0.0", "1.0.1", "1.1.0" }, new bool[] { true, true, false })]
        [InlineData(new string[] { "1.0.0", "1.0.1", "2.0.0" }, new bool[] { true, false, false })]
        [InlineData(new string[] { "1.0.0", "1.0.1", "1.0.2" }, new bool[] { true, true, false })]
        [InlineData(new string[] { "2.1.500", "2.1.400", "2.1.600" }, new bool[] { false, true, false })]
        [InlineData(new string[] { "2.2.100", "2.2.200", "2.2.300" }, new bool[] { false, true, false })]
        [InlineData(new string[] { "5.0.0", "5.0.1", "10.100.100" }, new bool[] { false, false, false })]
        internal void TestGetUninstallableNonSdkVersionsWindows(string[] versions, bool[] allowed)
        {
            TestGetUninstallableNonSdkVersions(versions, allowed);
        }

        [MacOsOnlyTheory]
        [InlineData(new string[] { "1.0.0", "1.0.1" }, new bool[] { true, true })]
        [InlineData(new string[] { "1.0.0", "1.0.1", "1.1.0" }, new bool[] { true, true, true })]
        [InlineData(new string[] { "5.0.0", "5.0.1", "10.100.100" }, new bool[] { true, true, true })]
        internal void TestGetUninstallableNonSdkVersionsMac(string[] versions, bool[] allowed)
        {
            TestGetUninstallableNonSdkVersions(versions, allowed);
        }

        internal void TestGetUninstallableNonSdkVersions(string[] versions, bool[] allowed)
        {
            var bundles = new List<Bundle>
            {
                new Bundle<RuntimeVersion>(new RuntimeVersion(), new BundleArch(), string.Empty, "RuntimeVersion"),
                new Bundle<AspNetRuntimeVersion>(new AspNetRuntimeVersion(), new BundleArch(), string.Empty, "AspNetVersion"),
                new Bundle<HostingBundleVersion>(new HostingBundleVersion(), new BundleArch(), string.Empty, "HostingBundleVersion")
            };
            foreach (string v in versions)
            {
                bundles.Add(new Bundle<SdkVersion>(new SdkVersion(v), new BundleArch(), string.Empty, v));
            }

            var uninstallable = VisualStudioSafeVersionsExtractor.GetUninstallableBundles(bundles);

            // Check that we still have all of the non-sdk bundles
            bundles.Should().Contain(b => b.Version is RuntimeVersion);
            bundles.Should().Contain(b => b.Version is AspNetRuntimeVersion);
            bundles.Should().Contain(b => b.Version is HostingBundleVersion);

            CheckAllowed(bundles, uninstallable, allowed);
        }

        private void CheckAllowed(IEnumerable<Bundle> bundles, IEnumerable<Bundle> uninstallable, bool[] allowed)
        {
            var fullList = bundles.Where(b => b.Version is SdkVersion).ToArray();
            for (int i = 0; i < fullList.Count(); i++)
            {
                if (allowed[i])
                {
                    uninstallable.Should().Contain(fullList[i]);
                }
                else
                {
                    uninstallable.Should().NotContain(fullList[i]);
                }
            }
        }

        [WindowsOnlyTheory]
        [InlineData(new string[] { "1.0.1", "1.0.0" }, new string[] { "", "None" })]
        [InlineData(new string[] { "2.3.0", "2.1.800", "2.1.300" }, new string[] { "", " 2019", " 2017" })]
        [InlineData(new string[] { "2.1.500", "2.1.400", "2.1.600" }, new string[] { " 2017", "None", " 2019" })]
        [InlineData(new string[] { "2.1.500", "5.0.1", "5.0.0" }, new string[] { " 2017", "5.0.0", "5.0.0" })]
        internal void TestGetListCommandUninstallableStringsWindows(string[] versions, string[] expectedStrings)
        {
            TestGetListCommandUninstallableStrings(versions, ConvertStringInput(expectedStrings));
        }

        [MacOsOnlyTheory]
        [InlineData(new string[] { "1.0.0", "1.0.1" }, new string[] { "None", "None" })]
        [InlineData(new string[] { "2.3.0", "2.2.0" }, new string[] { "None", "None" })]
        [InlineData(new string[] { "3.1.500", "5.0.1", "5.0.0" }, new string[] { "None", "None", "None" })]
        internal void TestGetListCommandUninstallableStringsMac(string[] versions, string[] expectedStrings)
        {
            TestGetListCommandUninstallableStrings(versions, ConvertStringInput(expectedStrings));
        }

        internal void TestGetListCommandUninstallableStrings(string[] versions, string[] expectedStrings)
        {
            var bundles = new List<Bundle>
            {
                new Bundle<RuntimeVersion>(new RuntimeVersion(), new BundleArch(), string.Empty, "RuntimeVersion"),
                new Bundle<AspNetRuntimeVersion>(new AspNetRuntimeVersion(), new BundleArch(), string.Empty, "AspNetVersion"),
                new Bundle<HostingBundleVersion>(new HostingBundleVersion(), new BundleArch(), string.Empty, "HostingBundleVersion")
            };
            foreach (string v in versions)
            {
                bundles.Add(new Bundle<SdkVersion>(new SdkVersion(v), new BundleArch(), string.Empty, v));
            }

            var strings = VisualStudioSafeVersionsExtractor.GetReasonRequiredStrings(bundles);

            strings.Count().Should().Be(bundles.Count());
            strings.Where(pair => !(pair.Key.Version is SdkVersion)).Select(pair => pair.Value).ToList().ForEach(str => str.Should().Be(string.Empty));
            for (int i = 0; i < versions.Length; i++)
            {
                strings.First(pair => pair.Key.DisplayName.Equals(versions[i])).Value.Should().Be(expectedStrings[i]);
            }
        }

        private string[] ConvertStringInput(string[] input)
        {
            var output = new string[input.Length];
            
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = input[i].Equals("None") ? string.Empty :
                    SemanticVersion.TryParse(input[i], out var parsed) ? string.Format(LocalizableStrings.UpperLimitRequirement, VisualStudioSafeVersionsExtractor.UpperLimit) :
                    string.Format(LocalizableStrings.RequirementExplainationString, input[i]);
            }

            return output;
        }
    }
}