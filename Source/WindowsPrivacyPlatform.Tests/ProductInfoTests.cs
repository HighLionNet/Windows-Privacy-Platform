using System.Reflection;
using WindowsPrivacyPlatform.Models;
using Xunit;

namespace WindowsPrivacyPlatform.Tests;

public class ProductInfoTests
{
    [Fact]
    public void About_metadata_comes_from_the_built_app_assembly()
    {
        var assembly = typeof(ManagedObject).Assembly;
        var info = ProductInfoReader.Read(assembly);
        Assert.Equal(assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product, info.Name);
        Assert.Equal(assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company, info.Company);
        Assert.Equal(assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright, info.Copyright);
        Assert.Equal(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0], info.Version);
        Assert.False(string.IsNullOrWhiteSpace(info.BuildIdentifier));
        Assert.StartsWith("https://", info.RepositoryUrl, StringComparison.OrdinalIgnoreCase);
    }
}
